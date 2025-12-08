using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Models;
using Sysfile = System.IO.File;

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
		
		
		await AttachMedia(model, post.Id);

		TempData["Success"] = "Post Created successfully!";
		return RedirectToAction("Edit", new { id = post.Id } );
		
	}

	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		var post = await context.Posts
			.Include(p => p.MediaFiles)
			.Include(post => post.Image)
			.Include(post => post.Video)
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
			Remaining = post.Remaining,
			ImageId = post.ImageId,
			VideoId = post.VideoId,
			Image = post.Image,
			Video = post.Video,
			MediaFiles = post.MediaFiles.Where(m => m.Id != post.ImageId && m.Id != post.VideoId)
				.ToList() // remove image and video from all media files
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
		var allIds = new List<int>();

		if (model.MediaFilesIds.Any()) 
			allIds.AddRange(model.MediaFilesIds);

		allIds.Add(model.ImageId);
		
		allIds.Add(model.VideoId);
		
		(await context.Media
			.Where(m => allIds.Contains(m.Id))
			.AsTracking()
			.ToListAsync()).ForEach(m => m.PostId = id);
		
		await context.SaveChangesAsync();
	}

	[System.Text.RegularExpressions.GeneratedRegex(@"[^a-zA-Z0-9_-]+")]
	private static partial System.Text.RegularExpressions.Regex FileNameRegex();
	
	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}