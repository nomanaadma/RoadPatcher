using System;

namespace RoadPatcher.Application.Entities;

public class PointsTransaction
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;
    public ApplicationUser? User { get; set; }

    // Number of points added (positive) or subtracted (negative)
    public int Points { get; set; }

    // Short code describing source, e.g. "Donation", "AdminAdjustment"
    public string Source { get; set; } = string.Empty;

    // Optional related entity id (e.g., DonationId)
    public int? RelatedEntityId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
