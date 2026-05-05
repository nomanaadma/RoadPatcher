using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Helpers;
using RoadPatcher.Models;
using RoadPatcher.Hubs;
using Microsoft.AspNetCore.SignalR;
using X.PagedList;
using X.PagedList.EF;
using X.PagedList.Extensions;
using Sysfile = System.IO.File;

namespace RoadPatcher.Controllers;

[Authorize]
public partial class PostsController(
	ApplicationDbContext context,
	IWebHostEnvironment webHostEnvironment,
	IConfiguration configuration,
	UserManager<ApplicationUser> userManager,
	IHubContext<NotificationHub> notificationHub
	) : Controller
{
	
	public IActionResult Index()
	{
		return RedirectToAction("Pending");
	}
	
	public async Task<IActionResult> Pending(int page = 1)
	{
		var posts = await GetPostsByStatus(PostStatus.Pending, page);
		return View("PostList", posts);
	}

	public async Task<IActionResult> InProcess(int page = 1)
	{
		var posts = await GetPostsByStatus(PostStatus.InProcess, page);
		return View("PostList", posts);
	}

	public async Task<IActionResult> Completed(int page = 1)
	{
		var posts = await GetPostsByStatus(PostStatus.Completed, page);
		return View("PostList", posts);
	}
	
	public async Task<IActionResult> Favourited(int page = 1)
	{
		var pageSize = int.Parse(configuration["Paging:PageSize"]!);
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		// Fetch favourited post ids for the current user first (single query)
		var favPostIds = await context.Favourites
			.Where(f => f.UserId == userId)
			.Select(f => f.PostId)
			.ToListAsync();
		
		var query = await BasePostQuery();

		var posts = await query
			.Where(p => favPostIds.Contains(p.Id))
			.ToPagedListAsync(page, pageSize);

		ViewData["Title"] = "Favourited";

		return View("PostList", posts);
    }
	
	public async Task<IActionResult> Donated(int page = 1)
	{
        var pageSize = int.Parse(configuration["Paging:PageSize"]!);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		// Fetch donated post ids for the current user first (single query)
		var donatedPostIds = await context.Donations
			.Where(d => d.UserId == userId)
			.Where(d => d.Status == DonationStatus.Completed)
			.Select(d => d.PostId)
			.ToListAsync();
		
		var query = await BasePostQuery();

		var posts = await query
			.Where(p => donatedPostIds.Contains(p.Id))
			.ToPagedListAsync(page, pageSize);

		ViewData["Title"] = "Donated";

		return View("PostList", posts);
    }
	
	public async Task<IActionResult> Search(string s, int page = 1)
	{
		ViewData["Title"] = $"Search Results for: \"{s}\"";
		ViewData["s"] = s;

		var pageSize = int.Parse(configuration["Paging:PageSize"]!);
		
		var query = await BasePostQuery();
		var pattern = $"%{s}%";

		var posts = await query
			.Where(p => EF.Functions.Like(p.Address, pattern)
				|| EF.Functions.Like(p.GoogleAddress, pattern)
				|| EF.Functions.Like(p.Description, pattern))
			.ToPagedListAsync(page, pageSize);

		return View("PostList", posts);
	}

	private async Task<IPagedList<Post>> GetPostsByStatus(
		PostStatus status,
		int page)
	{
		
		ViewData["Title"] = status.ToString();
		
		var pageSize = int.Parse(configuration["Paging:PageSize"]!);

		var query = await BasePostQuery();
		
		return await query
			.Where(p => p.Status == status)
			.ToPagedListAsync(page, pageSize);
	}
	
	[HttpGet("Posts/{id:int}")]
	public async Task<IActionResult> SinglePost(int id)
	{
		var query = await BasePostQuery();
		var post = await query
			.FirstOrDefaultAsync(p => p.Id == id);
	
		if (post == null)
		{
			return NotFound();
		}
	
		ViewData["Title"] = $"Post #{post.Id} Details";
	
		return View(post);
	}
	
	private async Task<IQueryable<Post>> BasePostQuery()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		var userData = await context.UserData.FirstOrDefaultAsync(x => x.UserId == userId);
		
		var userLocation = new Point(userData!.Longitude, userData.Latitude) { SRID = 4326 };

		return context.Posts
			.Include(p => p.User)
			.ThenInclude(u => u!.UserData)
			.Include(p => p.MediaFiles)
			.OrderBy(p => p.Location.Distance(userLocation));
	}

	[HttpGet]
	public IActionResult Create()
	{
		return View();
	}
	
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(PostViewModel model)
	{
		if (!ModelState.IsValid)
		{
			TempData["FormErrors"] = string.Join("||", 
				ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage));

			return View(model);
		}
		
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		
		var post = new Post
		{
			UserId = userId,
			Address = model.Address,
			GoogleAddress = model.GoogleAddress,
			Latitude = model.Latitude,
			Longitude = model.Longitude,
			Description = model.Description,
			Cost = model.Cost,
			Raised = model.Raised,
			ImageId = model.ImageId,
			VideoId = model.VideoId
		};
		
		context.Posts.Add(post);
		
		await context.SaveChangesAsync();
		
		await AttachMedia(model, post.Id);
		await NotifyAdminsOnNewPostAsync(post);

		TempData["Success"] = "Post submitted for review successfully. You will be notified once it has been reviewed.";
		return RedirectToAction("Edit", new { id = post.Id } );
		
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		var post = await context.Posts
			.Include(p => p.MediaFiles)
			.FirstOrDefaultAsync(p => p.Id == id);

		if (post == null)
			return NotFound();

		var model = new PostViewModel
		{
			Address = post.Address,
			GoogleAddress = post.GoogleAddress,
			Description = post.Description,
			Latitude = post.Latitude,
			Longitude = post.Longitude,
			Cost = post.Cost,
			Raised = post.Raised,
			ImageId = post.ImageId,
			VideoId = post.VideoId,
			MediaFiles = post.MediaFiles
		};
		
		return View(model);
	}
	
	
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, PostViewModel model)
	{
		
		if (!ModelState.IsValid)
		{
			TempData["FormErrors"] = string.Join("||", 
				ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage));

			return View(model);
		}
		
		var post = await context.Posts
			.AsTracking()
			.FirstOrDefaultAsync(p => p.Id == id);

		if (post == null)
			return NotFound();
		
		// Update MediaFiles
		var existingMedia = await context.Media
			.AsTracking()
			.Where(m => m.PostId == id)
			.ToListAsync();

		foreach (var media in existingMedia)
		{
			media.PostId = null;
		}
		
		post.Address = model.Address;
		post.GoogleAddress = model.GoogleAddress;
		post.Description = model.Description;
		post.Latitude = model.Latitude;
		post.Longitude = model.Longitude;
		post.Cost = model.Cost;
		post.ImageId = model.ImageId;
		post.VideoId = model.VideoId;
		post.UpdatedAt = DateTime.UtcNow;
		
		await context.SaveChangesAsync();

		await AttachMedia(model, post.Id);
		
		TempData["Success"] = "Post updated successfully!";
		return RedirectToAction("Edit", new { id = post.Id });
	}
	
	

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Upload(IFormFile file, [FromForm] bool uploaded)
	{
	
		// if the image is preload then don't upload it again
		if (uploaded)
		{
			return Ok();
		}
		
		// Basic Validation
		if (file.Length == 0)
			return BadRequest(new { error = "No file received." });
		
		// Allowed extensions
		string[] allowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".mp4", ".webm", ".ogg", ".mpeg", ".avi"];
		var ext = Path.GetExtension(file.FileName).ToLower();
		
		if (!allowedExtensions.Contains(ext))
			return BadRequest(new { error = "Wrong File Format" });
		
		
		// ORIGINAL FILENAME CLEANED
		var originalName = Path.GetFileNameWithoutExtension(file.FileName);
	
		// Replace special chars + spaces with "-"
		originalName = Path.GetInvalidFileNameChars().Aggregate(originalName, (current, c) => current.Replace(c, '-'));

		originalName = FileNameRegex().Replace(originalName, "-")
			.Trim('-')
			.ToLower();
	
		// Add timestamp
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var finalFileName = $"{originalName}-{timestamp}{ext}";
	
		// Upload folder
		var uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "posts");
	
		// Ensure exists
		if (!Directory.Exists(uploadFolder))
			Directory.CreateDirectory(uploadFolder);
		
		// Save file
		var fullPath = Path.Combine(uploadFolder, finalFileName);

		await using var stream = new FileStream(fullPath, FileMode.Create);
		await file.CopyToAsync(stream);
		
		var media = new Media
		{
			Name = finalFileName
		};
		
		context.Media.Add(media);
		await context.SaveChangesAsync();

		return Ok(new { name = finalFileName, id = media.Id });
		
	}

	[HttpDelete]
	public async Task<IActionResult> DeleteFile(int id)
	{
		var media = context.Media.AsTracking().FirstOrDefault(m => m.Id == id)!;
		
		var uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "posts");
		var fullPath = Path.Combine(uploadFolder, media.Name);
		
		context.Media.Remove(media);
		await context.SaveChangesAsync();
		
		Sysfile.Delete(fullPath);
		return Ok();

	}
	
	
	private async Task AttachMedia(PostViewModel model, int id)
	{
		
		List<int> allIds =
		[
			model.ImageId,
			model.VideoId,
			..model.MediaFilesIds,   // spread operator: adds items from collection
		];
		
		(await context.Media
			.Where(m => allIds.Contains(m.Id))
			.AsTracking()
			.ToListAsync()).ForEach(m => m.PostId = id);
		
		await context.SaveChangesAsync();
	}

	private async Task NotifyAdminsOnNewPostAsync(Post post)
	{
		var adminUsers = (await userManager.GetUsersInRoleAsync("Admin"))
			.Concat(await userManager.GetUsersInRoleAsync("SuperAdmin"))
			.GroupBy(user => user.Id)
			.Select(group => group.First())
			.ToList();

		if (adminUsers.Count == 0)
			return;

		var reviewUrl = $"/Admin/Posts/Review/{post.Id}";
		var notificationContent = $"New post is awaiting approval: <a href='{reviewUrl}'>Review post</a>";

		foreach (var admin in adminUsers)
		{
			context.Notifications.Add(new Notification
			{
				UserId = admin.Id,
				Content = notificationContent,
				Label = "Approval"
			});
		}

		await context.SaveChangesAsync();

		foreach (var admin in adminUsers)
		{
			var unreadCount = await context.Notifications
				.CountAsync(notification => notification.UserId == admin.Id && !notification.IsRead);

			await notificationHub.Clients.Group($"user_{admin.Id}")
				.SendAsync("NotificationCountUpdated", unreadCount);
		}
	}
	
	
	[HttpPost]
	[Authorize]
	public async Task<IActionResult> ToggleLike(int postId)
	{
		
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		
		var post = await context.Posts
			.AsTracking()
			.FirstOrDefaultAsync(p => p.Id == postId);

		var existingLike = await context.Likes
			.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

		bool liked;

		if (existingLike == null)
		{
			context.Likes.Add(new Like
			{
				PostId = postId,
				UserId = userId!
			});
			liked = true;
			
			post!.Likes += 1;
			
			
		}
		else
		{
			context.Likes.Remove(existingLike);
			liked = false;
			
			post!.Likes = Math.Max(0, post.Likes - 1);
			
		}

		await context.SaveChangesAsync();

		var totalLikes = await context.Likes.CountAsync(l => l.PostId == postId);
		

		return Json(new
		{
			liked,
			totalLikes
		});
	}
	
	
	[HttpPost]
	[Authorize]
	public async Task<IActionResult> ToggleFavourite(int postId)
	{
		
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		
		var post = await context.Posts
			.AsTracking()
			.FirstOrDefaultAsync(p => p.Id == postId);

		var existingFavourite = await context.Favourites
			.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

		bool favourited;

		if (existingFavourite == null)
		{
			context.Favourites.Add(new Favourite
			{
				PostId = postId,
				UserId = userId!
			});
			favourited = true;
			
			post!.Favourites += 1;
			
			
		}
		else
		{
			context.Favourites.Remove(existingFavourite);
			favourited = false;
			
			post!.Favourites = Math.Max(0, post.Favourites - 1);
			
		}

		await context.SaveChangesAsync();

		var totalFavourites = await context.Favourites.CountAsync(l => l.PostId == postId);
		

		return Json(new
		{
			favourited,
			totalFavourites
		});
	}

	
	[HttpPost]
	[Authorize]
	public async Task<IActionResult> IncrementShare(int postId)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		
		var post = await context.Posts
			.AsTracking()
			.FirstOrDefaultAsync(p => p.Id == postId);

		post!.Shares += 1;
		
		context.Shares.Add(new Share
		{
			PostId = postId,
			UserId = userId!
		});

		await context.SaveChangesAsync();

		return Json(new { totalShares = post.Shares });
	}
	
	[HttpPost]
	[Authorize]
	public async Task<IActionResult> ToggleStatus(int postId)
	{
		var post = await context.Posts
			.AsTracking()
			.FirstOrDefaultAsync(p => p.Id == postId);

		if (post == null)
			return NotFound();

		post.Status = post.Status == PostStatus.Completed
			? PostStatus.InProcess
			: PostStatus.Completed;

		var currentStatus = PostHelper.InvertStatus( (PostStatus)post.Status );

		post.UpdatedAt = DateTime.UtcNow;

		await context.SaveChangesAsync();

		return Ok(new
		{
			btn = PostHelper.RenderBtn(currentStatus, post.Id)
		});
	}

	public async Task<IActionResult> AdvancedSearch(
		[FromQuery(Name = "goal_start")] decimal? goalStart,
		[FromQuery(Name = "goal_end")] decimal? goalEnd,
		[FromQuery(Name = "raised_start")] decimal? raisedStart,
		[FromQuery(Name = "raised_end")] decimal? raisedEnd,
		[FromQuery(Name = "remaining_start")] decimal? remainingStart,
		[FromQuery(Name = "remaining_end")] decimal? remainingEnd,
		[FromQuery(Name = "achieved_start")] decimal? achievedStart,
		[FromQuery(Name = "achieved_end")] decimal? achievedEnd,
		[FromQuery(Name = "donated_by_start")] int? donatedByStart,
		[FromQuery(Name = "donated_by_end")] int? donatedByEnd,
		[FromQuery(Name = "users")] List<string>? userIds,
		[FromQuery(Name = "statuses")] List<PostStatus>? statuses,
		string? @as,
		double? lat,
		double? lng,
		int? radius,
		int page = 1)
	{
		// Redirect if no filters are provided
		var hasFilters =
			!string.IsNullOrWhiteSpace(@as) ||
			lat.HasValue && lng.HasValue ||
			goalStart.HasValue || goalEnd.HasValue ||
			raisedStart.HasValue || raisedEnd.HasValue ||
			remainingStart.HasValue || remainingEnd.HasValue ||
			achievedStart.HasValue || achievedEnd.HasValue ||
			donatedByStart.HasValue || donatedByEnd.HasValue ||
			userIds is { Count: > 0 } ||
			statuses is { Count: > 0 };
		
		if (!hasFilters)
			return RedirectToAction("Search", "Posts");
	
		ViewData["Title"] = ViewData["s"] = "Advanced Search Results";
	
		var pageSize = int.Parse(configuration["Paging:PageSize"]!);
		var query = await BasePostQuery();
	
		// ---------------------------
		// TEXT SEARCH
		// ---------------------------
		if (!string.IsNullOrWhiteSpace(@as))
		{
			var pattern = $"%{@as}%";
			query = query.Where(p =>
				EF.Functions.Like(p.Address, pattern) ||
				EF.Functions.Like(p.GoogleAddress, pattern) ||
				EF.Functions.Like(p.Description!, pattern)
			);
		}
		
		// ---------------------------
		// GEO FILTER (PostGIS)
		// ---------------------------
		if (lat.HasValue && lng.HasValue && radius.HasValue)
		{
			var userPoint = new Point(lng.Value, lat.Value) { SRID = 4326 };
			var distanceMeters = (radius ?? 10) * 1000;

			query = query.Where(p =>
				EF.Functions.IsWithinDistance(p.Location, userPoint, distanceMeters, false)
			);
		}
	
		// ---------------------------
		// COST (GOAL)
		// ---------------------------
		if (goalStart.HasValue)
			query = query.Where(p => p.Cost >= goalStart.Value);
	
		if (goalEnd.HasValue)
			query = query.Where(p => p.Cost <= goalEnd.Value);
	
		// ---------------------------
		// RAISED
		// ---------------------------
		if (raisedStart.HasValue)
			query = query.Where(p => p.Raised >= raisedStart.Value);
	
		if (raisedEnd.HasValue)
			query = query.Where(p => p.Raised <= raisedEnd.Value);
	
		// ---------------------------
		// DONATED BY
		// ---------------------------
		if (donatedByStart.HasValue)
			query = query.Where(p => p.PeopleDonated >= donatedByStart.Value);
	
		if (donatedByEnd.HasValue)
			query = query.Where(p => p.PeopleDonated <= donatedByEnd.Value);
	
		// ---------------------------
		// STATUS
		// ---------------------------
		
		if (statuses != null && statuses.Count != 0)
		{
			query = query.Where(p => statuses.Contains((PostStatus)p.Status!)); // assuming p.UserId stores the creator/donor
		}
		
		
		if (userIds != null && userIds.Count != 0)
		{
			query = query.Where(p => userIds.Contains(p.UserId)); // assuming p.UserId stores the creator/donor
		}
	
		// ---------------------------
		// CALCULATED FILTERS (in-memory)
		// ---------------------------
		var posts = await query.ToListAsync();
	
		if (remainingStart.HasValue)
			posts = posts.Where(p =>
				PostHelper.GetRemaining(p.Cost, p.Raised) >= remainingStart.Value).ToList();
	
		if (remainingEnd.HasValue)
			posts = posts.Where(p =>
				PostHelper.GetRemaining(p.Cost, p.Raised) <= remainingEnd.Value).ToList();
	
		if (achievedStart.HasValue)
			posts = posts.Where(p =>
				PostHelper.GetRaisedPercentage(p.Cost, p.Raised) >= achievedStart.Value).ToList();
	
		if (achievedEnd.HasValue)
			posts = posts.Where(p =>
				PostHelper.GetRaisedPercentage(p.Cost, p.Raised) <= achievedEnd.Value).ToList();
	
		// ---------------------------
		// PAGINATION
		// ---------------------------
		var paged = posts.ToPagedList(page, pageSize);
	
		return View("PostList", paged);
	}
	
	
	[HttpGet]
	public async Task<IActionResult> MapPosts(
		double neLat, double neLng,
		double swLat, double swLng,
		string statuses,
		string userids,
		[FromServices] GeometryFactory geometryFactory) 
	{
		
		var viewportEnvelope = new Envelope(
			swLng, // Min X (West)
			neLng, // Max X (East)
			swLat, // Min Y (South)
			neLat  // Max Y (North)
		);

		// Convert the envelope to a Geometry (Polygon) so EF Core can understand it
		var viewportGeometry = geometryFactory.ToGeometry(viewportEnvelope);

		// 2. Query
		var query = await BasePostQuery();
		
		query = query.Where(p => viewportGeometry.Intersects(p.Location));

		var users = Enumerable.Empty<object>();

		if (!string.IsNullOrEmpty(userids))
		{
			IEnumerable<string> userList = userids.Split(',');
			query = query.Where(p => userList.Contains(p.UserId));
			
			users = await context.UserData
				.Where(u => userList.Contains(u.UserId))
				.Select(u => new
				{
					fullName = ProfileHelper.GetFullName(u),
					lat = u.Latitude,
					lng = u.Longitude,
				})
				.ToListAsync();
			
		}
		
		IEnumerable<string> statusList = statuses.Split(',');

		query = query.Where(p => statusList.Contains(p.Status.ToString()));
		
		var posts = await query
			.Select(p => new {
				p.Id,
				p.Description,
				lat = p.Latitude,  
				lng = p.Longitude,
				Status = p.Status.ToString(),
				raisedPercentage = PostHelper.GetRaisedPercentage(p.Cost, p.Raised),
				remainingAmount = PostHelper.GetRemaining(p.Cost, p.Raised).ToPkr(),
				cost = p.Cost.ToPkr(),
				peopleDonated = p.PeopleDonated,
				totalRaised = p.Raised.ToPkr(),
				date = p.CreatedAt.ToPrettyFormat(),
				since = p.CreatedAt.ToAgo(),
				user = ProfileHelper.GetFullName(p.User!.UserData!)
			})
			.ToListAsync();

		var response = new[] {
			posts,
			users
		};

		return Json(response);
	}
	
	[System.Text.RegularExpressions.GeneratedRegex(@"[^a-zA-Z0-9_-]+")]
	private static partial System.Text.RegularExpressions.Regex FileNameRegex();
	
}

