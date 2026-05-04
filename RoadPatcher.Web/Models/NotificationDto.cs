using Newtonsoft.Json;

namespace RoadPatcher.Models;

public record NotificationDto
{
    public int Id { get; init; }

    public string Content { get; init; } = null!;

    public string Label { get; init; } = null!;

    [JsonProperty("is_read")]
    public bool IsRead { get; init; }

    [JsonProperty("toast_status")]
    public bool ToastStatus { get; init; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonProperty("created_at_formatted")]
    public string CreatedAtFormatted { get; init; } = null!;
}
