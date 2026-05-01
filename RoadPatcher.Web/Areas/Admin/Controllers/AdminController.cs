using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Web.Areas.Admin.Controllers;

[Route("Admin")]
[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController(UserManager<ApplicationUser> userManager) : Controller
{
	[HttpGet]
	public async Task<IActionResult> Index()
	{
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
