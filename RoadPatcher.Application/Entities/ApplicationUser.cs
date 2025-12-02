using Microsoft.AspNetCore.Identity;

namespace RoadPatcher.Application.Entities;

// ReSharper disable once ClassNeverInstantiated.Global
public class ApplicationUser : IdentityUser
{
	public virtual UserData? UserData { get; set; }
}



