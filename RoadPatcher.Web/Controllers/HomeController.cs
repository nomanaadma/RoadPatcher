using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RoadPatcher.Controllers;

[Authorize]
public class HomeController : Controller
{
	public IActionResult Index()
	{
		return RedirectToAction("Index", "Posts");
	}
	
	[HttpGet("{id:int}")]
	public IActionResult RedirectToPost(int id)
	{
		return RedirectToActionPermanent("SinglePost", "Posts", new { id });
	}
	
}