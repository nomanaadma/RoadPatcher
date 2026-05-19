using System;

namespace RoadPatcher.Helpers;

public static class BadgeHelper
{
    /// <summary>
    /// Get CSS class and icon for badge display
    /// </summary>
    public static (string CssClass, string Icon, string Color) GetBadgeStyle(string? badge)
    {
        return badge switch
        {
            "Gold" => ("badge-gold", "★", "#FFD700"),
            "Silver" => ("badge-silver", "★", "#C0C0C0"),
            "Bronze" => ("badge-bronze", "★", "#CD7F32"),
            _ => ("", "", "")
        };
    }

    /// <summary>
    /// Get badge threshold description
    /// </summary>
    public static string GetBadgeDescription(string? badge)
    {
        return badge switch
        {
            "Gold" => "Gold Contributor (Rs. 2000+)",
            "Silver" => "Silver Contributor (Rs. 1000+)",
            "Bronze" => "Bronze Contributor (Rs. 500+)",
            _ => ""
        };
    }
}
