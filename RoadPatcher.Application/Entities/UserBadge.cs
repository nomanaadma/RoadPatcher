using System;

namespace RoadPatcher.Application.Entities;

public class UserBadge
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;
    public ApplicationUser? User { get; set; }

    public int BadgeId { get; set; }
    public Badge? Badge { get; set; }

    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
