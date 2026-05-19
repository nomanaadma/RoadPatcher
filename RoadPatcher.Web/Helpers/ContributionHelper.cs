using System.Drawing;

namespace RoadPatcher.Helpers;

public static class ContributionHelper
{
	public const string BronzeBadge = "Bronze";
	public const string SilverBadge = "Silver";
	public const string GoldBadge = "Gold";
	public const string PlatinumBadge = "Platinum";
	public const string EmeraldBadge = "Emerald";
	public const string DiamondBadge = "Diamond";

	public static string? ResolveBadge(decimal totalDonated)
	{
		return totalDonated switch
		{
			>= 100000m => DiamondBadge,
			>= 50000m => EmeraldBadge,
			>= 25000m => PlatinumBadge,
			>= 10000m => GoldBadge,
			>= 5000m => SilverBadge,
			>= 1000m => BronzeBadge,
			_ => null
		};
	}

	public static string? GetBadgeSvg(string? badge, int size = 20)
	{
		return badge?.Trim().ToLowerInvariant() switch
		{
			"bronze" => BuildBronzeSvg(size),
			"silver" => BuildSilverSvg(size),
			"gold" => BuildGoldSvg(size),
			"platinum" => BuildPlatinumSvg(size),
			"emerald" => BuildEmeraldSvg(size),
			"diamond" => BuildDiamondSvg(size),
			_ => null
		};
	}

	private static string BuildBronzeSvg(int size) => BuildBadgeSvg(
		"bronze",
		"""
		<path d="M 50 5 L 89 27.5 L 89 72.5 L 50 95 L 11 72.5 L 11 27.5 Z M 50 16 L 79 32.5 L 79 67.5 L 50 84 L 21 67.5 L 21 32.5 Z" fill="url(#{id})" fill-rule="evenodd" />
		<polygon points="50,40 59,45 59,55 50,60 41,55 41,45" fill="url(#{id})" />
		""",
		"""
		<stop offset="50%" stop-color="#8E542D" />
		<stop offset="100%" stop-color="#5C361D" />
		""",
		size
	);

	private static string BuildSilverSvg(int size) => BuildBadgeSvg(
		"silver",
		"""
		<path d="M 50 5 L 89 27.5 L 89 72.5 L 50 95 L 11 72.5 L 11 27.5 Z M 50 16 L 79 32.5 L 79 67.5 L 50 84 L 21 67.5 L 21 32.5 Z" fill="url(#{id})" fill-rule="evenodd" />
		<polygon points="50,35 63,42.5 63,57.5 50,65 37,57.5 37,42.5" fill="url(#{id})" />
		""",
		"""
		<stop offset="50%" stop-color="#BDBDBD" />
		<stop offset="100%" stop-color="#757575" />
		""",
		size
	);

	private static string BuildGoldSvg(int size) => BuildBadgeSvg(
		"gold",
		"""
		<path d="M 50 5 L 89 27.5 L 89 72.5 L 50 95 L 11 72.5 L 11 27.5 Z M 50 16 L 79 32.5 L 79 67.5 L 50 84 L 21 67.5 L 21 32.5 Z" fill="url(#{id})" fill-rule="evenodd" />
		<polygon points="50,25 71.6,37.5 71.6,62.5 50,75 28.4,62.5 28.4,37.5" fill="url(#{id})" />
		""",
		"""
		<stop offset="50%" stop-color="#D4AF37" />
		<stop offset="100%" stop-color="#AA7900" />
		""",
		size
	);

	private static string BuildPlatinumSvg(int size) => BuildBadgeSvg(
		"platinum",
		"""
		<path d="M 50 5 L 89 27.5 L 89 72.5 L 50 95 L 11 72.5 L 11 27.5 Z M 50 16 L 79 32.5 L 79 67.5 L 50 84 L 21 67.5 L 21 32.5 Z" fill="url(#{id})" fill-rule="evenodd" />
		<polygon points="50,26 55.8,41.5 73.7,41.5 59.3,52 64.7,67.6 50,58.5 35.3,67.6 40.7,52 26.3,41.5 44.2,41.5" fill="url(#{id})" />
		""",
		"""
		<stop offset="100%" stop-color="#78909C" />
		""",
		size
	);

	private static string BuildEmeraldSvg(int size) => BuildBadgeSvg(
		"emerald",
		"""
		<path d="M 50 5 L 89 27.5 L 89 72.5 L 50 95 L 11 72.5 L 11 27.5 Z M 50 16 L 79 32.5 L 79 67.5 L 50 84 L 21 67.5 L 21 32.5 Z" fill="url(#{id})" fill-rule="evenodd" />
		<polygon points="50,25 71.6,37.5 71.6,62.5 50,75 28.4,62.5 28.4,37.5" fill="url(#{id})" />
		<polygon points="50,33 63.8,41 63.8,59 50,67 36.2,59 36.2,41" fill="#A5D6A7" opacity="0.4" />
		""",
		"""
		<stop offset="50%" stop-color="#4CAF50" />
		<stop offset="100%" stop-color="#2E7D32" />
		""",
		size
	);

	private static string BuildDiamondSvg(int size) => BuildBadgeSvg(
		"diamond",
		"""
		<path d="M 50 5 L 89 27.5 L 89 72.5 L 50 95 L 11 72.5 L 11 27.5 Z M 50 16 L 79 32.5 L 79 67.5 L 50 84 L 21 67.5 L 21 32.5 Z" fill="url(#{id})" fill-rule="evenodd" />
		<polygon points="35,30 65,30 75,45 50,75 25,45" fill="url(#{id})" />
		<polygon points="40,30 60,30 66,45 50,70 34,45" fill="#E1F5FE" opacity="0.4" />
		""",
		"""
		<stop offset="50%" stop-color="#29B6F6" />
		<stop offset="100%" stop-color="#0277BD" />
		""",
		size
	);

	private static string BuildBadgeSvg(string prefix, string shapeMarkup, string gradientStops, int size)
	{
		var id = $"{prefix}-{Guid.NewGuid():N}";

        var resolvedShapeMarkup = shapeMarkup.Replace("{id}", id);

		return $"""
		<svg width="{size}" viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
		  <defs>
		    <linearGradient id="{id}" x1="0%" y1="0%" x2="100%" y2="100%">
		      {gradientStops}
		    </linearGradient>
		  </defs>
		  {resolvedShapeMarkup}
		</svg>
		""";
	}
}