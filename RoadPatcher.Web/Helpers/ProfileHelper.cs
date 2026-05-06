using RoadPatcher.Application.Entities;

namespace RoadPatcher.Helpers;

public static class ProfileHelper
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
	
}