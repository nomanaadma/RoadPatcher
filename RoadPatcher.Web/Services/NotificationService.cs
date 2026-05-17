using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Hubs;

namespace RoadPatcher.Services;

public interface INotificationService
{
    Task CreateAsync(string? userId, string content, string label);
    Task NotifyPostSubscribersAsync(int postId, string? currentUserId, string content, string label, bool exclude = false);
}

public class NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub, UserManager<ApplicationUser> userManager) : INotificationService
{

    public async Task CreateAsync(string? userId, string content, string label)
    {
        if (string.IsNullOrEmpty(userId))
        {
            var superAdmin = (await userManager.GetUsersInRoleAsync("SuperAdmin")).FirstOrDefault();
            if (superAdmin == null)
                return;

            userId = superAdmin.Id;
        }

        var notification = new Notification
        {
            UserId = userId,
            Content = content,
            Label = label
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var unreadCount = await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();

        await hub.Clients.User(userId).SendAsync("NotificationCountUpdated", unreadCount);
    }

    public async Task NotifyPostSubscribersAsync(int postId, string? exceptUserId, string content, string label, bool include = false)
    {
        var subscriberIds = await db.PostNotificationSubscriptions
            .Where(subscription => subscription.PostId == postId)
            .Select(subscription => subscription.UserId)
            .ToListAsync();

        var superAdminUser = (await userManager.GetUsersInRoleAsync("SuperAdmin")).FirstOrDefault()!;

        var recipientQuery = subscriberIds.Concat([superAdminUser.Id]);

        if(!include) {
            recipientQuery = recipientQuery.Where(userId => !string.Equals(userId, exceptUserId, StringComparison.OrdinalIgnoreCase));
        }

        var recipientIds = recipientQuery.ToList();

        foreach (var recipientId in recipientIds)
        {
            await CreateAsync(recipientId, content, label);
        }
    }
}
