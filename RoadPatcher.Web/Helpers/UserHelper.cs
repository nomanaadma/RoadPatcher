using RoadPatcher.Application.Entities;
using System.Security.Claims;

namespace RoadPatcher.Helpers;

public static class UserHelper
{
	public static string GetProfileImage(string? imageFileName)
	{
		return string.IsNullOrEmpty(imageFileName) ? "/img/profile.png" : $"/uploads/users/{imageFileName}";
	}
	
	public static string GetFullName(UserData userData)
	{
		return userData.FirstName + ' ' + userData.LastName;
	}

	public static string GetDisplayName(ApplicationUser user)
	{
		if (user?.UserData != null)
		{
			return $"{user.UserData.FirstName} {user.UserData.LastName}";
		}

		return user?.Email ?? "Unknown User";
	}

	public static bool IsAdminOrSuperAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
    }

	public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin");
    }

	public static bool IsSuperAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("SuperAdmin");
    }
	
}