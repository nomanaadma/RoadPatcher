using System.Diagnostics;
using System.Security.Claims;
using SysFile = System.IO.File;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Models;

namespace RoadPatcher.Controllers;

[Authorize]
public partial class SettingsController(
	UserManager<ApplicationUser> userManager,
	SignInManager<ApplicationUser> signInManager,
	ApplicationDbContext context,
	IWebHostEnvironment webHostEnvironment
	) : Controller
{
	public IActionResult Index()
	{
		return View();
	}
	
	[HttpGet]
	public IActionResult ChangePassword()
	{
		return View();
	}
	
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
	{
		
		if (!ModelState.IsValid)
		{
			TempData["FormErrors"] = string.Join("||", 
				ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage));

			return View(model);
		}

		var user = await userManager.GetUserAsync(User);

		if (user == null)
		{
			await signInManager.SignOutAsync();
			return Challenge();
		}

		var result = await userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

		if (!result.Succeeded)
		{
			
			TempData["FormErrors"] = string.Join("||",
				result.Errors.Select(e => e.Description));

			return View(model);
		}

		await signInManager.RefreshSignInAsync(user);

		TempData["Success"] = "Your password has been changed successfully.";
		return RedirectToAction("ChangePassword");
	}
	
	[HttpGet]
	public async Task<IActionResult> EditProfile()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		var existing = await context.UserData.FirstOrDefaultAsync(x => x.UserId == userId);

		if (existing == null)
			return View();

		var model = new UserDataViewModel
		{
			FirstName = existing.FirstName,
			LastName = existing.LastName,
			Address = existing.Address,
			Latitude = existing.Latitude,
			Longitude = existing.Longitude,
			ExistingImage = existing.Image,
		};

		return View(model);
	}
	
	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> EditProfile(UserDataViewModel model)
	{
		if (!ModelState.IsValid)
		{
			TempData["FormErrors"] = string.Join("||", 
				ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage));

			return View(model);
		}

		var user = await userManager.GetUserAsync(User);

		if (user == null)
		{
			await signInManager.SignOutAsync();
			return Challenge();
		}
		
		var savedImage = await UploadProfileImage(model);
		
		await InsertOrUpdateProfile(model, user, savedImage);

		TempData["Success"] = "Profile saved successfully!";
		return RedirectToAction("EditProfile");
		
	}

	private async Task InsertOrUpdateProfile(UserDataViewModel model, 
		ApplicationUser user, string? savedImage)
	{
		
		var existing = await context.UserData
			.AsTracking()
			.FirstOrDefaultAsync(x => x.UserId == user.Id);
	
		if (existing == null)
		{
			// Insert new
			var userData = new UserData
			{
				UserId = user.Id,
				FirstName = model.FirstName,
				LastName = model.LastName,
				Address = model.Address,
				Latitude = model.Latitude,
				Longitude = model.Longitude,
				Image = savedImage,
			};
	
			context.UserData.Add(userData);
		}
		else
		{
			
			if (savedImage != null && existing.Image != null)
			{
				var oldFile = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "users", existing.Image);

				if (SysFile.Exists(oldFile))
					SysFile.Delete(oldFile);
			}
			
			existing.FirstName = model.FirstName;
			existing.LastName = model.LastName;
			existing.Address = model.Address;
			existing.Latitude = model.Latitude;
			existing.Longitude = model.Longitude;
			existing.Image = savedImage ?? existing.Image;
		}
	
		await context.SaveChangesAsync();
	}

	private async Task<string?> UploadProfileImage(UserDataViewModel model)
	{
		string? savedImagePath = null;

		if (model.ImageUpload is not { Length: > 0 }) return savedImagePath;
		
		// Allowed extensions
		string[] allowedExtensions = [".jpg", ".jpeg", ".png"];
		var ext = Path.GetExtension(model.ImageUpload.FileName).ToLower();
	
		if (!allowedExtensions.Contains(ext))
		{
			ModelState.AddModelError("ImageUpload", "Only JPG, JPEG, and PNG files are allowed.");
			return savedImagePath;
		}
	
		// ORIGINAL FILENAME CLEANED
		var originalName = Path.GetFileNameWithoutExtension(model.ImageUpload.FileName);
	
		// Replace special chars + spaces with "-"
		originalName = Path.GetInvalidFileNameChars().Aggregate(originalName, (current, c) => current.Replace(c, '-'));

		originalName = FileNameRegex().Replace(originalName, "-")
            .Trim('-')
			.ToLower();
	
		// Add timestamp
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var finalFileName = $"{originalName}-{timestamp}{ext}";
	
		// Upload folder
		var uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "uploads", "users");
	
		// Ensure exists
		if (!Directory.Exists(uploadFolder))
			Directory.CreateDirectory(uploadFolder);
	
		// Save file
		var fullPath = Path.Combine(uploadFolder, finalFileName);

		await using var stream = new FileStream(fullPath, FileMode.Create);
		await model.ImageUpload.CopyToAsync(stream);

		return finalFileName;
	}
	
	[System.Text.RegularExpressions.GeneratedRegex(@"[^a-zA-Z0-9_-]+")]
	private static partial System.Text.RegularExpressions.Regex FileNameRegex();


	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
	
}