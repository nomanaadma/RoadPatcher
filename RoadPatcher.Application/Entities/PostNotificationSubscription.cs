namespace RoadPatcher.Application.Entities;

public class PostNotificationSubscription
{
    public int Id { get; init; }

    public required int PostId { get; init; }
    public Post? Post { get; init; }

    public required string UserId { get; init; }
    public ApplicationUser? User { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
