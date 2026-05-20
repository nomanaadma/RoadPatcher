using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadPatcher.Application.Data;
using RoadPatcher.Helpers;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class StatisticsController(ApplicationDbContext context) : Controller
{
	[HttpGet]
	public IActionResult Index()
	{
		return View();
	}

	[HttpPost]
	public async Task<IActionResult> MapPostsByBoundary(
		[FromBody] BoundaryRequest request,
		[FromServices] GeometryFactory geometryFactory)
	{
		if (request.Coordinates.Count < 3)
		{
			return BadRequest(new { message = "Draw a polygon with at least 3 points." });
		}

		var coordinates = request.Coordinates
			.Select(point => new Coordinate(point.Lng, point.Lat))
			.ToList();

		if (!coordinates.First().Equals2D(coordinates.Last()))
		{
			coordinates.Add(new Coordinate(coordinates[0].X, coordinates[0].Y));
		}

		var polygon = geometryFactory.CreatePolygon(coordinates.ToArray());

		var rawPosts = await context.Posts
			.Include(post => post.User)
			.ThenInclude(user => user!.UserData)
			.Where(post => polygon.Intersects(post.Location))
			.OrderByDescending(post => post.CreatedAt)
			.ThenByDescending(post => post.Id)
			.Select(post => new
			{
				post.Id,
				post.Description,
				post.Latitude,
				post.Longitude,
				Status = post.Status.ToString(),
				post.Cost,
				post.Raised,
				post.PeopleDonated,
				post.CreatedAt,
				post.User!.UserData!.FirstName,
				post.User.UserData!.LastName
			})
			.ToListAsync();

		var posts = rawPosts.Select(post => new
		{
			id = post.Id,
			post.Description,
			lat = post.Latitude,
			lng = post.Longitude,
			status = post.Status,
			raisedPercentage = PostHelper.GetRaisedPercentage(post.Cost, post.Raised),
			remainingAmount = PostHelper.GetRemaining(post.Cost, post.Raised).ToPkr(),
			cost = post.Cost.ToPkr(),
			peopleDonated = post.PeopleDonated,
			totalRaised = post.Raised.ToPkr(),
			date = post.CreatedAt.ToPrettyFormat(),
			since = post.CreatedAt.ToAgo(),
			user = $"{post.FirstName} {post.LastName}"
		}).ToList();

		var pendingCount = rawPosts.Count(p => p.Status == PostStatus.Pending.ToString());
		var inProcessCount = rawPosts.Count(p => p.Status == PostStatus.InProcess.ToString());
		var completedCount = rawPosts.Count(p => p.Status == PostStatus.Completed.ToString());

		var totalReceived = rawPosts.Sum(post => post.Raised);
		var totalGoal = rawPosts.Sum(post => post.Cost);
		var filteredPostIds = rawPosts.Select(post => post.Id).ToList();

		var donorRows = await context.Donations
			.Where(donation => donation.Status == DonationStatus.Completed && filteredPostIds.Contains(donation.PostId))
			.Join(context.UserData, donation => donation.UserId, userData => userData.UserId, (donation, userData) => new
			{
				donation.UserId,
				userData.FirstName,
				userData.LastName,
				userData.Badge,
				donation.Amount
			})
			.GroupBy(row => new { row.UserId, row.FirstName, row.LastName, row.Badge })
			.Select(group => new
			{
				group.Key.FirstName,
				group.Key.LastName,
				group.Key.Badge,
				Amount = group.Sum(row => row.Amount)
			})
			.OrderByDescending(row => row.Amount)
			.ThenBy(row => row.FirstName)
			.ThenBy(row => row.LastName)
			.ToListAsync();

		var donationRows = await context.Donations
			.Where(donation => donation.Status == DonationStatus.Completed && filteredPostIds.Contains(donation.PostId))
			.OrderByDescending(donation => donation.CreatedAt)
			.ThenByDescending(donation => donation.Id)
			.Select(donation => new
			{
				donation.Id,
				donation.Amount,
				donation.CreatedAt,
				donation.PostId,
				donation.User!.UserData!.FirstName,
				donation.User.UserData!.LastName
			})
			.ToListAsync();

		return Ok(new
		{
			posts,
			summary = new
			{
				count = posts.Count,
				totalRaised = totalReceived.ToPkr(),
				pending = pendingCount,
				inProcess = inProcessCount,
				completed = completedCount,
				totalReceived = totalReceived.ToPkr(),
				totalGoal = totalGoal.ToPkr()
			},
			donors = donorRows.Select((row, index) => new
			{
				rank = index + 1,
				userName = $"{row.FirstName} {row.LastName} {ContributionHelper.GetBadgeSvg(row.Badge)}",
				amount = row.Amount.ToPkr()
			}),
			donations = donationRows.Select((row, index) => new
			{
				rank = index + 1,
				userName = $"{row.FirstName} {row.LastName}",
				amount = row.Amount.ToPkr(),
				date = row.CreatedAt.ToPrettyFormat(),
				postId = row.PostId
			})
		});
	}
}

public sealed class BoundaryRequest
{
	public List<BoundaryCoordinateDto> Coordinates { get; set; } = [];
}

public sealed class BoundaryCoordinateDto
{
	public double Lat { get; set; }
	public double Lng { get; set; }
}