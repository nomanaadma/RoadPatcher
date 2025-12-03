using System.ComponentModel.DataAnnotations;

namespace RoadPatcher.Models;

public class UserDataViewModel
{
	[Required(ErrorMessage = "First Name is required")]
	[Display(Name = "First Name")]
	public required string FirstName { get; init; }

	[Required(ErrorMessage = "Last Name is required")]
	[Display(Name = "Last Name")]
	public required string LastName { get; init; }
	
	[Required]
	public required string Address { get; init; }
	
	[Range(-90, 90)]
	public required double Latitude { get; init; }
	
	[Range(-180, 180)]
	public required double Longitude { get; init; }
	
	// Logic for Image Upload
	[Display(Name = "Profile Image")] 
	public IFormFile? ImageUpload { get; init; }
}