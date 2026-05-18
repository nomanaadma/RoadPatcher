using System.ComponentModel.DataAnnotations;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Models;

public class PostViewModel : AddressViewModel
{
	public int Id { get; set; }

	public string? UserId { get; set; }

	public ApprovalStatus ReviewStatus { get; set; }

	public string? ReviewerId { get; set; }

	public string? Description { get; init; }

	[Range(200, double.MaxValue, ErrorMessage = "Minimum cost must be at least 200.")]
	public decimal Cost { get; init; }

	public decimal Raised { get; init; }

	public decimal Remaining { get; init; }

	[Display(Name = "Image")]
	public int ImageId { get; init; }
	public Media? Image { get; init; }
	

	[Display(Name = "Video")]
	public int VideoId { get; init; }
	public Media? Video { get; init; }

	[Display(Name = "Completion Image")]
	public int? CompletionImageId { get; init; }
	public Media? CompletionImage { get; init; }
	

	public List<Media> MediaFiles { get; init; } = [];
	
	public List<int> MediaFilesIds { get; init; } = [];
	
}