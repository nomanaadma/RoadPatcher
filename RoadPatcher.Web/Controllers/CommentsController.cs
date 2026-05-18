using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Helpers;
using RoadPatcher.Models;
using RoadPatcher.Services;
using Microsoft.AspNetCore.Identity;

namespace RoadPatcher.Controllers;

[Route("[controller]")]
[Authorize]
[ApiController]
public class CommentsController(ApplicationDbContext db, INotificationService notificationService, UserManager<ApplicationUser> userManager) : ControllerBase
{
	
	// GET: comments/post/5
    [HttpGet("post/{postId:int}")]
    public async Task<IActionResult> GetCommentsForPost(int postId)
    {
        var adminIds = await userManager.GetUsersInRoleAsync("Admin");
        var superAdminIds = await userManager.GetUsersInRoleAsync("SuperAdmin");
        var adminUserIds = adminIds.Select(user => user.Id)
            .Concat(superAdminIds.Select(user => user.Id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var comments = await db.Comments
            .Include(c => c.User)
            .ThenInclude(u => u!.UserData)
            .Where(c => c.PostId == postId)
            .OrderByDescending(c => adminUserIds.Contains(c.UserId))
            .ThenBy(c => c.ParentId)
            .ThenBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                PostId = c.PostId,
                Parent = c.ParentId,
                Fullname = UserHelper.GetFullName(c.User!.UserData!),
                ProfilePictureUrl = UserHelper.GetProfileImage(c.User.UserData!.Image),
                Content = c.Content,
                Created = c.CreatedAt,
                CreatedByAdmin = adminUserIds.Contains(c.UserId)
            })
            .ToListAsync();

        return Ok(comments);
    }

    // POST: comments
    [HttpPost]
    public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest req)
    {
        // Prefer server-side authenticated user id to avoid spoofing
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(req.Content) || req.PostId <= 0 || string.IsNullOrWhiteSpace(userId))
            return BadRequest("Missing required fields");
        
        var post = await db.Posts.AsTracking().FirstOrDefaultAsync(p => p.Id == req.PostId);
        if (post == null)
	        return NotFound("Post not found");

        var commenter = await db.Users
	        .Include(u => u.UserData)
	        .FirstOrDefaultAsync(u => u.Id == userId);

        var commenterName = commenter == null
	        ? "Someone"
	        : UserHelper.GetDisplayName(commenter);

        var postViewUrl = $"/Posts/{post.Id}?open=comments";
        

        var comment = new Comment {
            PostId = req.PostId,
            ParentId = req.ParentId,
            UserId = userId,
            Content = req.Content,
            CreatedAt = DateTime.UtcNow
        };

        db.Comments.Add(comment);
        post.Discussions += 1;
        await db.SaveChangesAsync();

        var isCurrentUserAdmin = User.IsAdminOrSuperAdmin();

        var notificationContent = isCurrentUserAdmin
            ? $"<strong>[Admin]</strong> {commenterName} Commented on Post #{post.Id} <a href='{postViewUrl}'>View</a>"
            : $"{commenterName} Commented on Post #{post.Id} <a href='{postViewUrl}'>View</a>";

        await notificationService.NotifyPostSubscribersAsync(
            req.PostId,
            userId,
            notificationContent,
            "Comment");

        var savedComment = await db.Comments
            .Include(c => c.User)
            .ThenInclude(u => u!.UserData)
            .FirstAsync(c => c.Id == comment.Id);

        var dto = new CommentDto
        {
            Id = savedComment.Id,
            PostId = savedComment.PostId,
            Parent = savedComment.ParentId,
            Fullname = UserHelper.GetFullName(savedComment.User!.UserData!),
            ProfilePictureUrl = UserHelper.GetProfileImage(savedComment.User.UserData!.Image),
            Content = savedComment.Content,
            Created = savedComment.CreatedAt,
            CreatedByAdmin = isCurrentUserAdmin
        };
        
		var totalDiscussions = await db.Comments.CountAsync(l => l.PostId == req.PostId);
		
        dto.TotalDiscussion = totalDiscussions;

        return Ok(dto);
    }
}
