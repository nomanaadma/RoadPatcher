using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadPatcher.Application.Data;
using RoadPatcher.Helpers;

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
			.AsNoTracking()
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
				FirstName = post.User!.UserData!.FirstName,
				LastName = post.User.UserData!.LastName
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

		return Ok(new
		{
			posts,
			summary = new
			{
				count = posts.Count,
				totalRaised = rawPosts.Sum(post => post.Raised).ToPkr()
			}
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