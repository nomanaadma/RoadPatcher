using Microsoft.AspNetCore.Identity;

namespace RoadPatcher.Application.Entities;

public class ApplicationUser : IdentityUser
{
	public UserData? UserData { get; set; }

	public string? ExternalImage { get; set; }

}