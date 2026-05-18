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
using RoadPatcher.Services;
using X.PagedList;
using X.PagedList.EF;
using X.PagedList.Extensions;
using Sysfile = System.IO.File;
using CSnakes.Runtime;

namespace RoadPatcher.Controllers;

[Authorize]
public partial class PostsController(
	ApplicationDbContext context,
	IWebHostEnvironment webHostEnvironment,
	IConfiguration configuration,
	UserManager<ApplicationUser> userManager,
	INotificationService notificationService
	) : Controller
{
	private const string ManageScopeMyPosts = "myPosts";
	private const string ManageScopeAssigned = "assignedPosts";
	private const string ManageScopeAll = "allPosts";
	
	public IActionResult Index()
	{
		return RedirectToAction("Pending");
	}

	public IActionResult Testing()
	{
		// pythonEnv.Dispose();
		// var file = pythonEnv.MyScript();
		// var sum = file.AddNumbers(3, 4);
		var user = new { Name = "Alice", Age = 7, Role = "Admin" };
		return Json(user);
	}

	[HttpGet("Posts/Manage")]
	public async Task<IActionResult> Manage(
		string? scope = null,
		string? search = null,
		ApprovalStatus? reviewStatus = null,
		PostStatus? postStatus = null,
		string? creatorId = null,
		int page = 1)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(userId))
		{
			return RedirectToAction("Login", "Account", new { area = "Identity" });
		}

		var allowedScopes = GetManageScopes(User);

		var currentScope = allowedScopes[0].Value;

		if(allowedScopes.Any(option => option.Value == scope))
		{
			currentScope = scope!;
		}
		else
		{
			if (User.IsSuperAdmin())
			{
				currentScope = allowedScopes[2].Value;
			}

			if(User.IsAdmin())
			{
				currentScope = allowedScopes[1].Value;
			}
			
		}


		var query = BuildManageScopeQuery(currentScope, userId);
		var creatorOptions = await GetManageCreatorOptionsAsync(query);
		var pageSize = int.Parse(configuration["Paging:PageSize"]!);

		if (!string.IsNullOrWhiteSpace(search))
		{
			var pattern = $"%{search}%";
			query = query.Where(p => EF.Functions.ILike(p.Address, pattern)
				|| EF.Functions.ILike(p.GoogleAddress, pattern)
				|| (p.Description != null && EF.Functions.ILike(p.Description, pattern)));
		}

		if (reviewStatus.HasValue)
		{
			query = query.Where(p => p.ReviewStatus == reviewStatus.Value);
		}

		if (postStatus.HasValue)
		{
			query = query.Where(p => p.Status == postStatus.Value);
		}

		if (currentScope != ManageScopeMyPosts && !string.IsNullOrWhiteSpace(creatorId))
		{
			query = query.Where(p => p.UserId == creatorId);
		}

		query = query
			.OrderByDescending(p => p.CreatedAt)
			.ThenByDescending(p => p.Id);

		var pagedPosts = await query.ToPagedListAsync(page, pageSize);
		var rows = pagedPosts.Select(post => new PostManageListItemViewModel
		{
			Id = post.Id,
			Address = post.Address,
			GoogleAddress = post.GoogleAddress,
			CreatedAt = post.CreatedAt,
			Status = post.Status,
			ReviewStatus = post.ReviewStatus,
			Cost = post.Cost,
			Raised = post.Raised,
			CreatorId = post.UserId,
			CreatorName = UserHelper.GetDisplayName(post.User!),
			CreatorImage = post.User?.UserData?.Image,
			ReviewerId = post.ReviewerId,
			ReviewerName = post.Reviewer != null ? UserHelper.GetDisplayName(post.Reviewer) : null,
			ReviewerImage = post.Reviewer?.UserData?.Image
		}).ToList();

		ViewData["Title"] = GetManageScopeLabel(currentScope);

		return View(new PostManagePageViewModel
		{
			Posts = new StaticPagedList<PostManageListItemViewModel>(rows, pagedPosts.PageNumber, pagedPosts.PageSize, pagedPosts.TotalItemCount),
			CurrentScope = currentScope,
			Search = search,
			ReviewStatus = reviewStatus,
			PostStatus = postStatus,
			CreatorId = creatorId,
			AvailableScopes = allowedScopes,
			AvailableCreators = creatorOptions
		});
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
		
		var query = await BasePostApproveQuery();

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
		
		var query = await BasePostApproveQuery();

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
		
		var query = await BasePostApproveQuery();
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

		var query = await BasePostApproveQuery();
		
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

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		var isUnauthorized =
			post != null &&
			post.ReviewStatus != ApprovalStatus.Approved &&
			post.UserId != userId &&
			post.ReviewerId != userId;

		if (post == null || isUnauthorized)
		{
			return NotFound();
		}

		ViewData["Title"] = $"Post #{post?.Id} Details";
	
		return View(post);
	}
	
	private async Task<IQueryable<Post>> BasePostQuery()
	{
		return context.Posts
			.Include(p => p.User)
			.ThenInclude(u => u!.UserData)
			.Include(p => p.MediaFiles);
	}

	private async Task<IQueryable<Post>> BasePostLocationQuery()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		var userData = await context.UserData.FirstOrDefaultAsync(x => x.UserId == userId);
		
		var userLocation = new Point(userData!.Longitude, userData.Latitude) { SRID = 4326 };

		var query = await BasePostQuery();
		return query.OrderBy(p => p.Location.Distance(userLocation));
	}

	private async Task<IQueryable<Post>> BasePostApproveQuery()
	{
		var query = await BasePostLocationQuery();
		return query.Where(p => p.ReviewStatus == ApprovalStatus.Approved);
	}

	private static List<PostScopeOption> GetManageScopes(ClaimsPrincipal user)
	{
		var scopes = new List<PostScopeOption>
		{
			new() { Value = ManageScopeMyPosts, Label = "My Posts" }
		};

		if (user.IsAdminOrSuperAdmin())
		{
			scopes.Add(new PostScopeOption { Value = ManageScopeAssigned, Label = "Review Posts" });
		}

		if (user.IsSuperAdmin())
		{
			scopes.Add(new PostScopeOption { Value = ManageScopeAll, Label = "All Posts" });
		}

		return scopes;
	}

	private IQueryable<Post> BuildManageScopeQuery(string scope, string userId)
	{
		var query = context.Posts
			.Include(p => p.User)
			.ThenInclude(u => u!.UserData)
			.Include(p => p.Reviewer)
			.ThenInclude(r => r!.UserData)
			.AsQueryable();

		return scope switch
		{
			ManageScopeAssigned => query.Where(p => p.ReviewerId == userId),
			ManageScopeAll => User.IsSuperAdmin() ? query : query.Where(p => p.UserId == userId),
			_ => query.Where(p => p.UserId == userId)
		};
	}

	private async Task<List<PostCreatorOption>> GetManageCreatorOptionsAsync(IQueryable<Post> query)
	{
		var creatorIds = await query
			.Select(p => p.UserId)
			.Distinct()
			.ToListAsync();

		var users = await context.Users
			.Where(user => creatorIds.Contains(user.Id))
			.Include(user => user.UserData)
			.ToListAsync();

		return users
			.OrderBy(user => user.UserData?.FirstName)
			.ThenBy(user => user.UserData?.LastName)
			.Select(user => new PostCreatorOption
			{
				Id = user.Id,
				Name = UserHelper.GetDisplayName(user),
				Image = UserHelper.GetProfileImage(user.UserData?.Image)
			})
			.ToList();
	}

	private static string GetManageScopeLabel(string scope)
	{
		return scope switch
		{
			ManageScopeAssigned => "Assigned Reviews",
			ManageScopeAll => "All Posts",
			_ => "My Posts"
		};
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
		
		var superAdminUser = (await userManager.GetUsersInRoleAsync("SuperAdmin")).FirstOrDefault()!;

		// Determine reviewer assignment and review status based on creator role
		var reviewerId = superAdminUser?.Id;
		var reviewStatus = ApprovalStatus.InReview;

		if (User.IsSuperAdmin())
		{
			// SuperAdmin created post -> auto-approve (no review needed)
			reviewStatus = ApprovalStatus.Approved;
		}
		else if (User.IsUser())
		{
			// Regular user -> assign to least loaded admin
			reviewerId = await GetLeastLoadedReviewerIdAsync();
			reviewStatus = ApprovalStatus.InReview;
		}
		
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
			VideoId = model.VideoId,
			ReviewerId = reviewerId,
			ReviewStatus = reviewStatus
		};
		
		context.Posts.Add(post);
		
		await context.SaveChangesAsync();
		
		await AttachMedia(model, post.Id);

		// Auto-subscribe post owner
		if (!string.IsNullOrWhiteSpace(userId))
		{
			var ownerSubscribed = await context.PostNotificationSubscriptions
				.AnyAsync(s => s.PostId == post.Id && s.UserId == userId);

			if (!ownerSubscribed)
			{
				context.PostNotificationSubscriptions.Add(new PostNotificationSubscription
				{
					PostId = post.Id,
					UserId = userId
				});
			}
		}

		// Auto-subscribe assigned reviewer (if any and not the owner)
		if (!string.IsNullOrWhiteSpace(post.ReviewerId) && post.ReviewerId != userId)
		{
			var reviewerSubscribed = await context.PostNotificationSubscriptions
				.AnyAsync(s => s.PostId == post.Id && s.UserId == post.ReviewerId);

			if (!reviewerSubscribed)
			{
				context.PostNotificationSubscriptions.Add(new PostNotificationSubscription
				{
					PostId = post.Id,
					UserId = post.ReviewerId
				});
			}
		}

		await context.SaveChangesAsync();

		if(!User.IsSuperAdmin()) {
			await NotifyAdminsOnNewPostAsync(post);
		}

		TempData["Success"] = "Post submitted for review successfully. You will be notified once it has been reviewed.";
		return RedirectToAction("SinglePost", new { id = post.Id } );
		
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		var post = await context.Posts
			.Include(p => p.MediaFiles)
			.FirstOrDefaultAsync(p => p.Id == id);

		if (post == null)
			return NotFound();

		var forbidden = true;

		if(
			User.IsSuperAdmin()	|| 
			(userId == post.UserId && post.ReviewStatus == ApprovalStatus.ChangesRequested) ||
			(userId == post.ReviewerId && post.ReviewStatus == ApprovalStatus.InReview) ||
			(userId == post.ReviewerId && post.Status == PostStatus.InProcess)
		) {
			forbidden = false;
		}

		if(forbidden)
		{
			return NotFound();
		}

		var model = new PostViewModel
		{
			Id = post.Id,
			UserId = post.UserId,
			ReviewStatus = post.ReviewStatus,
			ReviewerId = post.ReviewerId,
			Address = post.Address,
			GoogleAddress = post.GoogleAddress,
			Description = post.Description,
			Latitude = post.Latitude,
			Longitude = post.Longitude,
			Cost = post.Cost,
			Raised = post.Raised,
			ImageId = post.ImageId,
			VideoId = post.VideoId,
			CompletionImageId = post.CompletionImageId,
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
		post.CompletionImageId = model.CompletionImageId;
		post.UpdatedAt = DateTime.UtcNow;
		
		
		var previousReviewStatus = post.ReviewStatus;
		var submitAction = Request.Form["submitAction"].ToString();

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		var postViewAnchor = $"<a href='/Posts/{post.Id}'>View Post</a>";
		var postAnchor = $"<a href='/Posts/Edit/{post.Id}'>Edit Post</a>";

		if (submitAction == "Complete")
		{
			if (User.IsUser())
				return Forbid();

			if (post.Status != PostStatus.InProcess)
				return BadRequest("Post must be in process before it can be completed.");

			if (!model.CompletionImageId.HasValue)
			{
				ModelState.AddModelError(nameof(model.CompletionImageId), "Completion image is required when marking a post as completed.");
				TempData["FormErrors"] = string.Join("||",
					ModelState.Values
						.SelectMany(v => v.Errors)
						.Select(e => e.ErrorMessage));

				return View(model);
			}

			post.Status = PostStatus.Completed;
			post.UpdatedAt = DateTime.UtcNow;

			await context.SaveChangesAsync();
			await AttachMedia(model, post.Id);

			var completionMessage = $"We have great news! The road repair work for Post #{post.Id} has been successfully completed. <a href='/Posts/{post.Id}'>View Post</a>";
			await notificationService.NotifyPostSubscribersAsync(post.Id, userId, completionMessage, "Completed");

			TempData["Success"] = "Post marked as completed successfully!";
			return RedirectToAction("SinglePost", new { id = post.Id });
		}

		if(userId == post.UserId)
		{
			post.ReviewStatus = ApprovalStatus.InReview;

			var notificationMessage = $"Post #{post.Id} has been updated and requires review. {postViewAnchor}";
			await notificationService.CreateAsync(post.ReviewerId, notificationMessage, "Review");
			await notificationService.CreateAsync(null, notificationMessage, "Review");
		}
		else if (userId == post.ReviewerId || User.IsSuperAdmin())
		{
			post.ReviewStatus = ApprovalStatus.Approved;

			var notificationMessage = $"Post #{post.Id} has been approved! {postViewAnchor}";
			await notificationService.CreateAsync(post.UserId, notificationMessage, "Approved");
		}

		await context.SaveChangesAsync();

		await AttachMedia(model, post.Id);
		
		TempData["Success"] = "Post updated successfully!";
		return RedirectToAction("SinglePost", new { id = post.Id });
	}

	[HttpPost]
	[Authorize]
	public async Task<IActionResult> Review(int id, [FromBody] ReviewRequest request)
	{
		var post = await context.Posts
			.AsTracking()
			.Include(p => p.User)
			.FirstOrDefaultAsync(p => p.Id == id);

		if (post == null)
			return NotFound(new { message = "Post not found" });

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
		var isSuperAdmin = User.IsSuperAdmin();

		// Only reviewer or superadmin can submit review
		if (post.ReviewerId != userId && !isSuperAdmin)
			return Forbid();

		// Only can review if status is InReview
		if (post.ReviewStatus != ApprovalStatus.InReview)
			return BadRequest(new { message = "Post is not in review status" });

		var postAnchor = $"<a href='/Posts/Edit/{post.Id}'>Edit Post</a>";
		var postViewAnchor = $"<a href='/Posts/{post.Id}'>View Post</a>";

		try
		{
			// 1. Set up variables to hold state specific to each action
			ApprovalStatus newStatus;
			string notificationMessage;
			string notificationLabel;
			string responseMessage;

			switch (request.Action)
			{
				case "RequestChanges":
					newStatus = ApprovalStatus.ChangesRequested;
					notificationMessage = $"Admin requested changes on post #{post.Id}. \"{request.Comment}\" {postViewAnchor}";
					notificationLabel = "Change Requested";
					responseMessage = "Changes requested notification sent";
					break;

				case "Approve":
					newStatus = ApprovalStatus.Approved;
					notificationMessage = $"Post #{post.Id} has been approved! {postViewAnchor}";
					notificationLabel = "Approved";
					responseMessage = "Post approved";
					break;

				case "Reject":
					newStatus = ApprovalStatus.Rejected;
					notificationMessage = string.IsNullOrEmpty(request.Comment)
						? $"Post #{post.Id} has been rejected {postViewAnchor}"
						: $"Post #{post.Id} has been rejected. {postViewAnchor} Reason: {request.Comment}";
					notificationLabel = "Rejected";
					responseMessage = "Post rejected";
					break;

				default:
					return BadRequest(new { message = "Invalid action" });
			}

			// 2. Apply the updates to the Post
			post.ReviewStatus = newStatus;

			// Persist post changes first
			await context.SaveChangesAsync();

			// 3. Create and dispatch notifications (owner + superadmin)
			await notificationService.CreateAsync(post.UserId, notificationMessage, notificationLabel);
			await notificationService.CreateAsync(null, notificationMessage, notificationLabel);

			return Ok(new { message = responseMessage });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "Error processing review: " + ex.Message });
		}
	}

	public class ReviewRequest
	{
		public required string Action { get; set; }
		public string? Comment { get; set; }
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
			model.CompletionImageId ?? 0,
			..model.MediaFilesIds,   // spread operator: adds items from collection
		];

		allIds = allIds.Where(id => id > 0).Distinct().ToList();
		
		(await context.Media
			.Where(m => allIds.Contains(m.Id))
			.AsTracking()
			.ToListAsync()).ForEach(m => m.PostId = id);
		
		await context.SaveChangesAsync();
	}

	private async Task<string?> GetLeastLoadedReviewerIdAsync()
	{
		// Only consider Admin role for reviewer assignment; SuperAdmin is never assigned
		var adminUsers = (await userManager.GetUsersInRoleAsync("Admin")).ToList();

		if (adminUsers.Count == 0)
			return null; // No admins available; post will be created but unassigned

		var adminUserIds = adminUsers.Select(user => user.Id).ToList();
		var activeReviewLoads = await context.Posts
			.Where(post => post.ReviewerId != null
				&& adminUserIds.Contains(post.ReviewerId)
				&& post.ReviewStatus == ApprovalStatus.InReview)
			.GroupBy(post => post.ReviewerId)
			.Select(group => new
			{
				ReviewerId = group.Key!,
				Load = group.Count()
			})
			.ToListAsync();

		var loadLookup = activeReviewLoads.ToDictionary(item => item.ReviewerId, item => item.Load);
		var lowestLoad = adminUsers.Min(user => loadLookup.TryGetValue(user.Id, out var load) ? load : 0);
		var leastLoadedAdmins = adminUsers
			.Where(user => (loadLookup.TryGetValue(user.Id, out var load) ? load : 0) == lowestLoad)
			.ToList();

		return leastLoadedAdmins[Random.Shared.Next(leastLoadedAdmins.Count)].Id;
	}

	private async Task NotifyAdminsOnNewPostAsync(Post post)
	{

		var reviewUrl = $"/Posts/{post.Id}";

		var assignedAdmin = await context.Users
				.Include(u => u.UserData)
				.FirstOrDefaultAsync(u => u.Id == post.ReviewerId);
		
		if (assignedAdmin != null)
		{

			var assignedAdminName = UserHelper.GetDisplayName(assignedAdmin);

			await notificationService.CreateAsync(assignedAdmin.Id,
				$"You have a post <span class='badge bg-light-info'>#{post.Id}</span> to <a href='{reviewUrl}'>Review</a>", "Review");

			await notificationService.CreateAsync(null,
				$"Admin <strong>{assignedAdminName}</strong> has been assigned to review <a href='{reviewUrl}'><span class='badge bg-light-info'>Post #{post.Id}</span></a>", "Review");

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
	public async Task<IActionResult> TogglePostNotification(int postId)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(userId))
			return Unauthorized();

		var postExists = await context.Posts
			.AnyAsync(p => p.Id == postId);

		if (!postExists)
			return NotFound();

		var existingSubscription = await context.PostNotificationSubscriptions
			.AsTracking()
			.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId);

		bool subscribed;

		if (existingSubscription == null)
		{
			context.PostNotificationSubscriptions.Add(new PostNotificationSubscription
			{
				PostId = postId,
				UserId = userId
			});

			subscribed = true;
		}
		else
		{
			context.PostNotificationSubscriptions.Remove(existingSubscription);
			subscribed = false;
		}

		await context.SaveChangesAsync();

		return Json(new
		{
			subscribed
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
		var query = await BasePostApproveQuery();
	
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
		var query = await BasePostApproveQuery();
		
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
					fullName = UserHelper.GetFullName(u),
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
				user = UserHelper.GetFullName(p.User!.UserData!)
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

