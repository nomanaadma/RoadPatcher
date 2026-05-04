namespace RoadPatcher.Application.Entities;

/// <summary>
/// Represents a user notification (e.g., donation, comment, share updates).
/// Standalone entity without FK to Post/Donation/Comment for flexibility.
/// </summary>
public class Notification
{
    public int Id { get; init; }

    public required string UserId { get; init; }
    public ApplicationUser? User { get; init; }

    /// <summary>
    /// Notification message content (e.g., "John donated Rs. 500 to your post")
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Notification type label (e.g., Donation, Approval, Comment).
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Read/Unread status. False = unread, True = read.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Controls whether the toast has already been shown/acknowledged.
    /// False = toast still eligible to show, True = toast already handled.
    /// </summary>
    public bool ToastStatus { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
