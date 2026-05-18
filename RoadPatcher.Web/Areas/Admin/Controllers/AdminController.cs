using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Entities;
using RoadPatcher.Helpers;

namespace RoadPatcher.Web.Areas.Admin.Controllers;

[Route("Admin")]
[Area("Admin")]
[Authorize]
public class AdminController(UserManager<ApplicationUser> userManager) : Controller
{
	[HttpGet]
	public async Task<IActionResult> Index()
	{
		if (User.IsUser())
		{
			return RedirectToAction("Pending", "Posts", new { area = "" });
		}

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (string.IsNullOrEmpty(userId))
		{
			return Unauthorized();
		}

		var user = await userManager.Users
			.Include(u => u.UserData)
			.FirstOrDefaultAsync(u => u.Id == userId);

		return View(user);
	}
}
