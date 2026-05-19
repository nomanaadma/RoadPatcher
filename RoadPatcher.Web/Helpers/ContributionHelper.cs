namespace RoadPatcher.Helpers;

public static class ContributionHelper
{
	public const string BronzeBadge = "Bronze";
	public const string SilverBadge = "Silver";
	public const string GoldBadge = "Gold";

	public static string? ResolveBadge(decimal totalDonated)
	{
		return totalDonated switch
		{
			>= 10000m => GoldBadge,
			>= 5000m => SilverBadge,
			>= 1000m => BronzeBadge,
			_ => null
		};
	}

	public static string? GetBadgeColor(string? badge)
	{
		return badge?.Trim().ToLowerInvariant() switch
		{
			"bronze" => "#cd7f32",
			"silver" => "#c0c0c0",
			"gold" => "#d4af37",
			_ => null
		};
	}

	public static bool HasBadge(string? badge)
	{
		return GetBadgeColor(badge) != null;
	}
}