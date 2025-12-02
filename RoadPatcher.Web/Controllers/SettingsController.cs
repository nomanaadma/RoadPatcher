using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoadPatcher.Models;

namespace RoadPatcher.Controllers;

[Authorize]
public class SettingsController(
	UserManager<IdentityUser> userManager,
	SignInManager<IdentityUser> signInManager
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
	
	public IActionResult EditProfile()
	{
		return View();
	}

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
	
	
}