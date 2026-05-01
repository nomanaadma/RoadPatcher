using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadPatcher.Models;

namespace RoadPatcher.Controllers;

[Authorize]
public class HomeController : Controller
{
	public IActionResult Index()
	{
		// Redirect admin and super admin to admin dashboard
		if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
		{
			return Redirect("/Admin");
		}

		return RedirectToAction("Index", "Posts");
	}
	
	[HttpGet("{id:int}")]
	public IActionResult RedirectToPost(int id)
	{
		return RedirectToActionPermanent("SinglePost", "Posts", new { id });
	}
	
}