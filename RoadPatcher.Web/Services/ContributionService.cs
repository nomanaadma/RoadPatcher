using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Helpers;
using RoadPatcher.Models;

namespace RoadPatcher.Services;

public interface IContributionService
{
	Task UpdateContributorBadgeAsync(string userId, DateTime donatedAt);
	Task<LeaderboardPageViewModel> GetLeaderboardAsync();
}

public class ContributionService(ApplicationDbContext db, INotificationService notificationService) : IContributionService
{
	private sealed record DonationTotalRow(string UserId, decimal Amount);

	public async Task UpdateContributorBadgeAsync(string userId, DateTime donatedAt)
	{
		var totalDonated = await db.Donations
			.AsNoTracking()
			.Where(d => d.UserId == userId && d.Status == DonationStatus.Completed)
			.SumAsync(d => d.Amount);

		var userData = await db.UserData
			.AsTracking()
			.FirstOrDefaultAsync(u => u.UserId == userId);

		if (userData == null)
		{
			return;
		}

		var badge = ContributionHelper.ResolveBadge(totalDonated);
		var previousBadge = userData.Badge;
		var badgeChanged = !string.Equals(previousBadge, badge, StringComparison.OrdinalIgnoreCase);

		if (badgeChanged)
		{
			userData.Badge = badge;
			userData.BadgeAwardedDate = badge == null ? null : donatedAt;
		}
		else if (badge != null)
		{
			userData.BadgeAwardedDate = donatedAt;
		}

		await db.SaveChangesAsync();

		if (badgeChanged && !string.IsNullOrWhiteSpace(badge))
		{
			var badgeMessage = string.IsNullOrWhiteSpace(previousBadge)
				? $"You earned the {badge} badge for your community contributions."
				: $"Your contributor badge has been updated to {badge}. Keep it up!";

			await notificationService.CreateAsync(userId, badgeMessage, "Badge");
		}
	}

	public async Task<LeaderboardPageViewModel> GetLeaderboardAsync()
	{
		var completedDonationTotals = db.Donations
			.AsNoTracking()
			.Where(d => d.Status == DonationStatus.Completed)
			.GroupBy(d => d.UserId)
			.Select(group => new DonationTotalRow(group.Key, group.Sum(d => d.Amount)));

		var goldContributors = await BuildTierRowsAsync(ContributionHelper.GoldBadge, completedDonationTotals);
		var silverContributors = await BuildTierRowsAsync(ContributionHelper.SilverBadge, completedDonationTotals);
		var bronzeContributors = await BuildTierRowsAsync(ContributionHelper.BronzeBadge, completedDonationTotals);

		var recentDonations = await db.Donations
			.AsNoTracking()
			.Where(d => d.Status == DonationStatus.Completed)
			.OrderByDescending(d => d.CreatedAt)
			.ThenByDescending(d => d.Id)
			.Take(10)
			.Select(d => new
			{
				d.Amount,
				d.CreatedAt,
				d.PostId,
				FirstName = d.User!.UserData!.FirstName,
				LastName = d.User.UserData!.LastName
			})
			.ToListAsync();

		return new LeaderboardPageViewModel
		{
			GoldContributors = goldContributors,
			SilverContributors = silverContributors,
			BronzeContributors = bronzeContributors,
			RecentDonations = recentDonations
				.Select((donation, index) => new RecentDonationRowViewModel
				{
					Rank = index + 1,
					UserName = $"{donation.FirstName} {donation.LastName}",
					Amount = donation.Amount,
					DonationDate = donation.CreatedAt,
					PostId = donation.PostId
				})
				.ToList()
		};
	}

	private async Task<IReadOnlyList<LeaderboardContributorRowViewModel>> BuildTierRowsAsync(
		string badge,
		IQueryable<DonationTotalRow> completedDonationTotals)
	{
		var rows = await (from userData in db.UserData.AsNoTracking()
			join donationTotal in completedDonationTotals on userData.UserId equals donationTotal.UserId
			where userData.Badge == badge
			select new
			{
				userData.FirstName,
				userData.LastName,
				userData.BadgeAwardedDate,
				donationTotal.Amount
			})
			.OrderByDescending(row => row.BadgeAwardedDate)
			.ThenByDescending(row => row.Amount)
			.ThenBy(row => row.FirstName)
			.ThenBy(row => row.LastName)
			.Take(10)
			.ToListAsync();

		return rows
			.Select((row, index) => new LeaderboardContributorRowViewModel
			{
				Rank = index + 1,
				UserName = $"{row.FirstName} {row.LastName}",
				Amount = row.Amount,
				BadgeAwardedDate = row.BadgeAwardedDate
			})
			.ToList();
	}
}