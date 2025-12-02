using Microsoft.AspNetCore.Identity;

namespace RoadPatcher.Application.Entities;

public class ApplicationUser : IdentityUser
{
	public virtual UserData? UserData { get; set; }
}



