using Microsoft.AspNetCore.Mvc;
using RoadPatcher.Services;

namespace RoadPatcher.Controllers;

public class LeaderboardController(IContributionService contributionService) : Controller
{
	public async Task<IActionResult> Index()
	{
		var model = await contributionService.GetLeaderboardAsync();
		return View(model);
	}
}