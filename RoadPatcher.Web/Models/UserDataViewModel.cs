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
	
	[Required]
	public required string GoogleAddress { get; init; }
	
	public required double Latitude { get; init; }
	
	public required double Longitude { get; init; }
	
	// Logic for Image Upload
	[Display(Name = "Profile Image")] 
	public IFormFile? ImageUpload { get; init; }
	
	// Existing image filename stored on server (e.g. uploads/users/filename.jpg)
	public string? ExistingImage { get; init; }
}