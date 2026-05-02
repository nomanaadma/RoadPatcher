using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Helpers;
using RoadPatcher.Models;
using X.PagedList;
using X.PagedList.EF;

namespace RoadPatcher.Controllers;

[Authorize]
public class NotificationsController(ApplicationDbContext db, IConfiguration config) : Controller
{
    private readonly int _pageSize = config.GetValue("Paging:PageSize", 10);

    /// <summary>
    /// GET: /Notifications - List user's notifications with search and pagination
    /// </summary>
    [HttpGet]
    [Route("Notifications")]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account", new { area = "Identity" });

        // Base query: notifications for current user
        var query = db.Notifications
            .Where(n => n.UserId == userId)
            .AsNoTracking();

        // Apply search filter on content
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(n => n.Content.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Order by latest first
        query = query.OrderByDescending(n => n.CreatedAt);

        // Map to DTO with formatted date
        var notificationDtos = await query
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Content = n.Content,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                CreatedAtFormatted = DateHelper.ToAgo(n.CreatedAt)
            })
            .ToPagedListAsync(page, _pageSize);

        // Store search in ViewBag for view display
        ViewBag.CurrentSearch = search;

        return View(notificationDtos);
    }

    /// <summary>
    /// POST: /Notifications/MarkAsRead/{id} - Toggle notification read status (AJAX)
    /// </summary>
    [HttpPost]
    [Route("Notifications/MarkAsRead/{id:int}")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Security: Ensure user can only toggle their own notifications
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
            return NotFound();

        // Toggle IsRead status
        notification.IsRead = !notification.IsRead;
        notification.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var statusText = notification.IsRead ? "Read" : "Unread";
        var message = $"Notification marked as {statusText}";

        return Json(new
        {
            success = true,
            message = message,
            isRead = notification.IsRead
        });
    }

    /// <summary>
    /// GET: /Notifications/GetUnreadCount - Get unread notification count (for SignalR)
    /// </summary>
    [HttpGet]
    [Route("Notifications/GetUnreadCount")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var unreadCount = await db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return Json(new { unreadCount = unreadCount });
    }
}
