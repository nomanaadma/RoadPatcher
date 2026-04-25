using System.ComponentModel.DataAnnotations;

namespace RoadPatcher.Models;

public class ChangePasswordViewModel
{
	[DataType(DataType.Password)]
	[Display(Name = "Current Password")]
	public string? OldPassword { get; init; }

	[Required]
	[DataType(DataType.Password)]
	[Display(Name = "New Password")]
	public string NewPassword { get; init; } = string.Empty;

	[Required]
	[DataType(DataType.Password)]
	[Display(Name = "Confirm New Password")]
	[Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
	public string ConfirmPassword { get; init; } = string.Empty;
}