namespace RoadPatcher.Helpers;

public static class ProfileHelper
{
	public static string GetProfileImage(string? imageFileName)
	{
		return string.IsNullOrEmpty(imageFileName) ? "/img/profile.png" : $"/uploads/users/{imageFileName}";
	}
}