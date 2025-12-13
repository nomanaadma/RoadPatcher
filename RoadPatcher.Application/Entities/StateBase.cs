namespace RoadPatcher.Application.Entities;

public class StateBase
{
	public int Id { get; init; }
	public required string UserId { get; init; }
	public ApplicationUser? User { get; init; }
	public required int PostId { get; init; }
	public Post Post { get; init; } = null!;
	public DateTime CreatedAt { get; init; }
}