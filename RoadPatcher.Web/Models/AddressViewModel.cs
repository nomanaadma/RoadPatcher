using System.ComponentModel.DataAnnotations;

namespace RoadPatcher.Models;

public class AddressViewModel
{
	[Required(ErrorMessage = "Address is required. You may use the 'Copy to Full Address' button below to copy google address to full address.")]
	public required string Address { get; init; }
	
	[Display(Name = "Google Address")]
	public required string GoogleAddress { get; init; }
	
	public required double Latitude { get; init; }
	
	public required double Longitude { get; init; }
}