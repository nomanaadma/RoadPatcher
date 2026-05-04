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

        await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.ToastStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.ToastStatus, true)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow));

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
                ToastStatus = n.ToastStatus,
                CreatedAt = n.CreatedAt,
                CreatedAtFormatted = DateHelper.ToAgo(n.CreatedAt)
            })
            .ToPagedListAsync(page, _pageSize);

        // Store search in ViewBag for view display
        ViewBag.CurrentSearch = search;

        return View(notificationDtos);
    }

    /// <summary>
    /// POST: /Notifications/MarkAsRead/{id} - Toggle notification read status and clear toast flag
    /// </summary>
    [HttpPost]
    [Route("Notifications/MarkAsRead/{id:int}")]
    public async Task<IActionResult> MarkAsRead(int id, string? returnUrl = null)
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
        notification.ToastStatus = true;
        notification.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST: /Notifications/MarkToastAsSeen/{id} - Mark toast as handled so it won't show again.
    /// </summary>
    [HttpPost]
    [Route("Notifications/MarkToastAsSeen/{id:int}")]
    public async Task<IActionResult> MarkToastAsSeen(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var notification = await db.Notifications
            .AsTracking()
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
            return NotFound();

        notification.ToastStatus = true;
        notification.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = "Toast marked as seen"
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

    /// <summary>
    /// GET: /Notifications/GetPendingToasts - Get notifications whose toast has not yet been seen.
    /// </summary>
    [HttpGet]
    [Route("Notifications/GetPendingToasts")]
    public async Task<IActionResult> GetPendingToasts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var notifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead && !n.ToastStatus)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Content = n.Content,
                IsRead = n.IsRead,
                ToastStatus = n.ToastStatus,
                CreatedAt = n.CreatedAt,
                CreatedAtFormatted = DateHelper.ToAgo(n.CreatedAt)
            })
            .ToListAsync();

        return Json(notifications);
    }
}
