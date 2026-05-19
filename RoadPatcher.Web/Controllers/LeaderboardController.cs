using Microsoft.AspNetCore.Mvc;
using RoadPatcher.Services;

namespace RoadPatcher.Controllers;

public class LeaderboardController(IPointsService pointsService) : Controller
{
    public async Task<IActionResult> Index(int top = 20)
    {
        var entries = await pointsService.GetLeaderboardAsync(top);
        return View(entries);
    }
}
