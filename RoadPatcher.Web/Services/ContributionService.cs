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
		var diamondContributors = await BuildTierRowsAsync(ContributionHelper.DiamondBadge);
		var emeraldContributors = await BuildTierRowsAsync(ContributionHelper.EmeraldBadge);
		var platinumContributors = await BuildTierRowsAsync(ContributionHelper.PlatinumBadge);
		var goldContributors = await BuildTierRowsAsync(ContributionHelper.GoldBadge);
		var silverContributors = await BuildTierRowsAsync(ContributionHelper.SilverBadge);
		var bronzeContributors = await BuildTierRowsAsync(ContributionHelper.BronzeBadge);

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
                d.User!.UserData!.FirstName,
                d.User.UserData.LastName,
				d.User.UserData.Badge
			})
			.ToListAsync();

		return new LeaderboardPageViewModel
		{
			DiamondContributors = diamondContributors,
			EmeraldContributors = emeraldContributors,
			PlatinumContributors = platinumContributors,
			GoldContributors = goldContributors,
			SilverContributors = silverContributors,
			BronzeContributors = bronzeContributors,
			RecentDonations = recentDonations
				.Select((donation, index) => new RecentDonationRowViewModel
				{
					Rank = index + 1,
					UserName = $"{donation.FirstName} {donation.LastName}",
					Amount = donation.Amount,
					Badge = donation.Badge!,
					DonationDate = donation.CreatedAt,
					PostId = donation.PostId
				})
				.ToList()
		};
	}

	private async Task<IReadOnlyList<LeaderboardContributorRowViewModel>> BuildTierRowsAsync(string badge)
	{
		var rows = await db.UserData
			.AsNoTracking()
			.Where(userData => userData.Badge == badge)
			.Select(userData => new
			{
				userData.FirstName,
				userData.LastName,
				userData.BadgeAwardedDate,
				Amount = db.Donations
					.Where(d => d.UserId == userData.UserId && d.Status == DonationStatus.Completed)
					.Select(d => (decimal?)d.Amount)
					.Sum() ?? 0m
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