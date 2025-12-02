using System.ComponentModel.DataAnnotations;

namespace RoadPatcher.Models;

public class ChangePasswordViewModel
{
	[Required]
	[DataType(DataType.Password)]
	[Display(Name = "Current Password")]
	public required string OldPassword { get; init; }

	[Required]
	[DataType(DataType.Password)]
	[Display(Name = "New Password")]
	public required string NewPassword { get; init; }

	[Required]
	[DataType(DataType.Password)]
	[Display(Name = "Confirm New Password")]
	[Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
	public required string ConfirmPassword { get; init; }
}