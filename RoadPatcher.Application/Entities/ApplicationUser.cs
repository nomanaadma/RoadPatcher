using Microsoft.AspNetCore.Identity;

namespace RoadPatcher.Application.Entities;

public class ApplicationUser : IdentityUser
{
	public UserData? UserData { get; set; }

	public string? ExternalImage { get; set; }

	// Cumulative points awarded to the user (donation-based, etc.)
	public int Points { get; set; }

	// Navigation: badges awarded to the user
	public ICollection<UserBadge>? UserBadges { get; set; }

	// Navigation: points transactions for history/audit
	public ICollection<PointsTransaction>? PointsTransactions { get; set; }
}



