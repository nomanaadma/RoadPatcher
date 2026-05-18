using NetTopologySuite.Geometries;

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
	
	// Spatial property
	public Point Location
	{
		get => new(Longitude, Latitude) { SRID = 4326 };
		init
		{
			Latitude = value.Y;
			Longitude = value.X;
		}
	}

	public string? Description { get; set; }

	public decimal Cost { get; set; }
	public decimal Raised { get; set; }
	public int PeopleDonated { get; set; }
	public int Likes { get; set; }
	
	public int Discussions { get; set; }
	public int Shares { get; set; }
	public int Favourites { get; set; }

	public PostStatus? Status { get; set; } = PostStatus.Pending;

	// Review/Approval status
	public ApprovalStatus ReviewStatus { get; set; } = ApprovalStatus.InReview;

	// Admin who claimed/reviewing the post (nullable until claimed)
	public string? ReviewerId { get; set; }

	public ApplicationUser? Reviewer { get; init; }
	public int ImageId { get; set; }
	public Media Image { get; init; } = null!;

	public int VideoId { get; set; }
	public Media Video { get; init; } = null!;

	public int? CompletionImageId { get; set; }
	public Media? CompletionImage { get; init; }
	
	public List<Media> MediaFiles { get; init; } = [];
	
	public DateTime CreatedAt { get; init; }
	public DateTime UpdatedAt { get; set; }
}
