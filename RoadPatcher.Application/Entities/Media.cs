namespace RoadPatcher.Application.Entities;

public class Media
{
	public int Id { get; init; }

	public int? PostId { get; init; }
	public Post? Post { get; init; }

	public required string Name { get; init; }
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
