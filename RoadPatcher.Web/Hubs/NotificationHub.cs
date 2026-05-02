using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using System.Security.Claims;

namespace RoadPatcher.Hubs;

/// <summary>
/// SignalR Hub for real-time notification updates.
/// Handles notification count updates and pushes new notification alerts to clients.
/// </summary>
[Authorize]
public class NotificationHub(ApplicationDbContext db) : Hub
{
    /// <summary>
    /// Called when a client connects to the hub.
    /// Sends the current unread notification count to the client.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to a group for their notifications (e.g., "user_123")
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Send current unread count to the connecting client
            var unreadCount = await db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            await Clients.Caller.SendAsync("NotificationCountUpdated", unreadCount);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Send notification count update to a specific user.
    /// Called from server when a new notification is created or marked as read.
    /// </summary>
    public async Task NotifyNewNotification(string userId, int unreadCount)
    {
        // Send to all clients in the user's group
        await Clients.Group($"user_{userId}")
            .SendAsync("NotificationCountUpdated", unreadCount);
    }

    /// <summary>
    /// Send notification marked-as-read update to a specific user.
    /// </summary>
    public async Task NotifyMarkAsRead(string userId, int unreadCount)
    {
        await Clients.Group($"user_{userId}")
            .SendAsync("NotificationCountUpdated", unreadCount);
    }
}
