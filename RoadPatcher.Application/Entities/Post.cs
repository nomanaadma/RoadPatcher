namespace RoadPatcher.Application.Entities;

public class Post
{
	public int Id { get; init; }

	public required string UserId { get; init; }
	public required ApplicationUser User { get; init; }

	public required string Address { get; init; }
	public required string GoogleAddress { get; init; }
	public double Latitude { get; init; }
	public double Longitude { get; init; }

	public required string Description { get; init; }

	public decimal Cost { get; init; }
	public decimal Raised { get; init; }
	public decimal Remaining { get; init; }

	public int PeopleDonated { get; init; }
	public int Likes { get; init; }
	public int Comments { get; init; }
	public int Shares { get; init; }
	public int Saves { get; init; }

	public PostStatus Status { get; init; }

	public int ImageId { get; init; }
	public Media Image { get; init; } = null!;

	public int VideoId { get; init; }
	public Media Video { get; init; } = null!;
	
	public List<Media> MediaFiles { get; init; } = [];
	
	public bool AllowEdit { get; init; }
	
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
