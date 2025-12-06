using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Models;

namespace RoadPatcher.Controllers;

[Authorize]
public partial class PostsController(
	ApplicationDbContext context,
	IWebHostEnvironment webHostEnvironment
	) : Controller
{
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
			Remaining = model.Remaining,
			ImageId = model.ImageId,
			VideoId = model.VideoId
		};
		
		context.Posts.Add(post);
		
		await context.SaveChangesAsync();
		
		
		var allIds = new List<int>();
		
		allIds.AddRange(model.MediaFiles);
		
		allIds.Add(model.ImageId);
		
		allIds.Add(model.VideoId);
		
		(await context.Media
			.Where(m => allIds.Contains(m.Id))
			.AsTracking()
			.ToListAsync()).ForEach(m => m.PostId = post.Id);
		
		await context.SaveChangesAsync();
		
		TempData["Success"] = "Post Created successfully!";
		return RedirectToAction("Create", new { id = post.Id } );
		
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Upload(IFormFile file)
	{

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

	[System.Text.RegularExpressions.GeneratedRegex(@"[^a-zA-Z0-9_-]+")]
	private static partial System.Text.RegularExpressions.Regex FileNameRegex();
	
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}