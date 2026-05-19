using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Services;
using System.Linq;
using System;

namespace RoadPatcher.Services;

public record LeaderboardEntry(string UserId, string UserName, int Points, string? Badge, DateTime? BadgeAwardedDate);

public interface IPointsService
{
    Task AwardPointsAsync(string userId, int points, string source, int? relatedEntityId = null);
    Task<int> GetUserPointsAsync(string userId);
    Task<List<LeaderboardEntry>> GetLeaderboardAsync(int top = 20);
}

public class PointsService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, INotificationService notificationService) : IPointsService
{
    public async Task AwardPointsAsync(string userId, int points, string source, int? relatedEntityId = null)
    {
        if (string.IsNullOrEmpty(userId) || points == 0)
            return;

        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return;

        var tx = new PointsTransaction
        {
            UserId = userId,
            Points = points,
            Source = source ?? "",
            RelatedEntityId = relatedEntityId
        };

        db.Add(tx);

        // update cumulative points
        user.Points += points;

        await db.SaveChangesAsync();

        // Auto-award badges whose threshold is now met
        var eligibleBadges = await db.Set<Badge>()
            .Where(b => b.ThresholdPoints.HasValue && b.ThresholdPoints.Value <= user.Points)
            .ToListAsync();

        if (eligibleBadges.Any())
        {
            var existing = await db.Set<UserBadge>()
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync();

            var newAwards = new List<UserBadge>();

            foreach (var badge in eligibleBadges)
            {
                if (existing.Contains(badge.Id))
                    continue;

                var award = new UserBadge
                {
                    UserId = userId,
                    BadgeId = badge.Id,
                    AwardedAt = DateTime.UtcNow
                };
                newAwards.Add(award);
                db.Add(award);

                // notify user about new badge
                await notificationService.CreateAsync(userId, $"You've earned the '{badge.Name}' badge!", "Badge");
            }

            if (newAwards.Any())
            {
                await db.SaveChangesAsync();
            }
        }
    }

    public async Task<int> GetUserPointsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return 0;

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return user?.Points ?? 0;
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int top = 20)
    {
        return await db.Users
            .AsNoTracking()
            .Include(u => u.UserData)
            .OrderByDescending(u => u.Points)
            .Take(top)
            .Select(u => new LeaderboardEntry(
                u.Id, 
                (u.UserName ?? u.Email)!, 
                u.Points,
                u.UserData != null ? u.UserData.Badge : null,
                u.UserData != null ? u.UserData.BadgeAwardedDate : null
            ))
            .ToListAsync();
    }
}
