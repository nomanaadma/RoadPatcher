namespace RoadPatcher.Application.Entities;

public class Comment
{
    public int Id { get; init; }

    public required int PostId { get; init; }
    public Post? Post { get; init; }

    // Parent comment for nested replies (nullable)
    public int? ParentId { get; init; }
    public Comment? Parent { get; init; }

    // Replies navigation
    public List<Comment> Replies { get; init; } = [];

    public required string UserId { get; init; }
    public ApplicationUser? User { get; init; }

    public required string Content { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

