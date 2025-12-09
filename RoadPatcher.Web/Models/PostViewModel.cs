using System.ComponentModel.DataAnnotations;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Models;

public class PostViewModel : AddressViewModel
{
	public string? Description { get; init; }

	public decimal Cost { get; init; }

	public decimal Raised { get; init; }

	public decimal Remaining { get; init; }

	[Display(Name = "Image")]
	public int ImageId { get; init; }
	public Media? Image { get; init; }
	

	[Display(Name = "Video")]
	public int VideoId { get; init; }
	public Media? Video { get; init; }
	

	public IEnumerable<Media> MediaFiles { get; init; } = [];
	
	public IEnumerable<int> MediaFilesIds { get; } = [];
	
}