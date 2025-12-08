using System.ComponentModel.DataAnnotations;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Models;

public class PostViewModel
{
	[Required]
	[Display(Name = "Address")]
	public required string Address { get; init; }

	[Required]
	[Display(Name = "Google Address")]
	public required string GoogleAddress { get; init; }

	[Display(Name = "Description")]
	public string? Description { get; init; }

	[Display(Name = "Latitude")]
	public double Latitude { get; init; }

	[Display(Name = "Longitude")]
	public double Longitude { get; init; }

	[Display(Name = "Cost")]
	public decimal Cost { get; init; }

	[Display(Name = "Raised")]
	public decimal Raised { get; init; }

	[Display(Name = "Remaining")]
	public decimal Remaining { get; init; }

	[Display(Name = "Image Id")]
	public int ImageId { get; init; }
	public Media? Image { get; init; }
	

	[Display(Name = "Video Id")]
	public int VideoId { get; init; }
	public Media? Video { get; init; }
	

	[Display(Name = "Media Files")] 
	public IEnumerable<Media> MediaFiles { get; init; } = [];
	
	public IEnumerable<int> MediaFilesIds { get; } = [];
	
	
}