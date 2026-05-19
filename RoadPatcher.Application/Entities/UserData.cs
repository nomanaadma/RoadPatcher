namespace RoadPatcher.Application.Entities;

public class UserData
{
	public required string UserId { get; init; }
	
	public ApplicationUser? User { get; init; }
	
	public required string FirstName { get; set; }
	public required string LastName { get; set; }
	public required string Address { get; set; }
	
	public required string GoogleAddress { get; set; }

	public required double Latitude { get; set; }
	public required double Longitude { get; set; }

	public string? Image { get; set; }

	public string? Badge { get; set; }

	public DateTime? BadgeAwardedDate { get; set; }
	
}


