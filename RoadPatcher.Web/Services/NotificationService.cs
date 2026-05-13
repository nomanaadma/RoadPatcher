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
}
