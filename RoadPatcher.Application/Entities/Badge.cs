using System;
namespace RoadPatcher.Application.Entities;

public class Badge
{
    public int Id { get; set; }

    // Unique machine name
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Points threshold (optional) to auto-award this badge
    public int? ThresholdPoints { get; set; }

    // Optional icon/media id reference
    public int? IconMediaId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
