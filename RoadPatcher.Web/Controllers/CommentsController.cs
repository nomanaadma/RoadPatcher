using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using RoadPatcher.Helpers;
using RoadPatcher.Models;
using RoadPatcher.Services;

namespace RoadPatcher.Controllers;

[Route("[controller]")]
[Authorize]
[ApiController]
public class CommentsController(ApplicationDbContext db, INotificationService notificationService) : ControllerBase
{
	
	// GET: comments/post/5
    [HttpGet("post/{postId:int}")]
    public async Task<IActionResult> GetCommentsForPost(int postId)
    {
        var comments = await db.Comments
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .Select(CommentHelper.ToDto)
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

        await notificationService.NotifyPostSubscribersAsync(
            req.PostId,
            userId,
            $"{commenterName} Commented on Post #{post.Id} <a href='{postViewUrl}'>View</a>",
            "Comment");
        
        var dto = await db.Comments
	        .Where(c => c.Id == comment.Id)
	        .Select(CommentHelper.ToDto)
	        .FirstAsync();
        
		var totalDiscussions = await db.Comments.CountAsync(l => l.PostId == req.PostId);
		
        dto.TotalDiscussion = totalDiscussions;

        return Ok(dto);
    }
}
