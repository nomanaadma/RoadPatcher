using System.ComponentModel.DataAnnotations;

namespace RoadPatcher.Models;

public class AddressViewModel
{
	public required string Address { get; init; }
	
	[Display(Name = "Google Address")]
	public required string GoogleAddress { get; init; }
	
	public required double Latitude { get; init; }
	
	public required double Longitude { get; init; }
}