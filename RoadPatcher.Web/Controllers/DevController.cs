using Microsoft.AspNetCore.Mvc;
using RoadPatcher.Application.Data;

namespace RoadPatcher.Controllers;

public class DevController(ApplicationDbContext dbContext) : Controller
{
	// GET
	public async Task Index()
	{
		await Task.CompletedTask;
	}
}