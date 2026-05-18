using Newtonsoft.Json;

namespace RoadPatcher.Models;

public record CommentDto
{
	public int Id { get; init; }
	
	[JsonProperty("post_id")]
	
	public int PostId { get; init; }
	public int? Parent { get; init; }

	public string Fullname { get; init; } = null!;

	[JsonProperty("profile_picture_url")]
	public string ProfilePictureUrl { get; init; } = null!;
	
	public string Content { get; init; } = null!;
	public DateTime Created { get; init; }

	[JsonProperty("created_by_admin")]
	public bool CreatedByAdmin { get; init; }
	
	public int TotalDiscussion { get; set; }
}

public record CreateCommentRequest
{
	
	[JsonProperty("postId")]
	public int PostId { get; init; }
	
	[JsonProperty("parentId")]
	public int? ParentId { get; init; }
	
	public string Content { get; init; } = null!;
}