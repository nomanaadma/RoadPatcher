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

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _hub = hub;
        _userManager = userManager;
    }

    public async Task CreateAsync(string? userId, string content, string label)
    {
        if (string.IsNullOrEmpty(userId))
        {
            var superAdmin = (await _userManager.GetUsersInRoleAsync("SuperAdmin")).FirstOrDefault();
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

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        var unreadCount = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();

        await _hub.Clients.User(userId).SendAsync("NotificationCountUpdated", unreadCount);
    }
}
