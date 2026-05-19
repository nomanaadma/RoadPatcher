namespace RoadPatcher.Models;

public class LeaderboardPageViewModel
{
	public required IReadOnlyList<LeaderboardContributorRowViewModel> GoldContributors { get; init; }
	public required IReadOnlyList<LeaderboardContributorRowViewModel> SilverContributors { get; init; }
	public required IReadOnlyList<LeaderboardContributorRowViewModel> BronzeContributors { get; init; }
	public required IReadOnlyList<RecentDonationRowViewModel> RecentDonations { get; init; }
}

public class LeaderboardContributorRowViewModel
{
	public required int Rank { get; init; }
	public required string UserName { get; init; }
	public required decimal Amount { get; init; }
	public DateTime? BadgeAwardedDate { get; init; }
}

public class RecentDonationRowViewModel
{
	public required int Rank { get; init; }
	public required string UserName { get; init; }
	public required decimal Amount { get; init; }
	public required DateTime DonationDate { get; init; }
	public required int PostId { get; init; }
}