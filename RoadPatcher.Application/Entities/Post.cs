namespace RoadPatcher.Application.Entities;

public class Post
{
	public int Id { get; init; }

	public required string UserId { get; init; }
	public ApplicationUser? User { get; init; }

	public required string Address { get; set; }
	public required string GoogleAddress { get; set; }
	public double Latitude { get; set; }
	public double Longitude { get; set; }

	public string? Description { get; set; }

	public decimal Cost { get; set; }
	public decimal Raised { get; init; }
	public int PeopleDonated { get; init; }
	public int Likes { get; set; }
	
	public int Discussions { get; set; }
	public int Shares { get; set; }
	public int Favourites { get; set; }

	public PostStatus? Status { get; init; } = PostStatus.Pending;

	public int ImageId { get; set; }
	public Media Image { get; init; } = null!;

	public int VideoId { get; set; }
	public Media Video { get; init; } = null!;
	
	public List<Media> MediaFiles { get; init; } = [];

	public bool AllowEdit { get; init; } = true;
	
	public DateTime CreatedAt { get; init; }
	public DateTime UpdatedAt { get; set; }
}
