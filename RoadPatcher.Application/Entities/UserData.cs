namespace RoadPatcher.Application.Entities;

public class UserData
{
	public required string UserId { get; init; }
	public required string FirstName { get; init; }
	public required string LastName { get; init; }
	public required string Address { get; init; }
	public double Latitude { get; init; }
	public double Longitude { get; init; }

	public string? Image { get; init; }
	
	public required ApplicationUser User { get; init; }
}

