using X.PagedList;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Models;

public class PostManagePageViewModel
{
	public required IPagedList<PostManageListItemViewModel> Posts { get; init; }
	public string CurrentScope { get; init; } = "myPosts";
	public string? Search { get; init; }
	public ApprovalStatus? ReviewStatus { get; init; }
	public PostStatus? PostStatus { get; init; }
	public string? CreatorId { get; init; }
	public List<PostScopeOption> AvailableScopes { get; init; } = [];
	public List<PostCreatorOption> AvailableCreators { get; init; } = [];
}

public class PostManageListItemViewModel
{
	public int Id { get; init; }
	public string Address { get; init; } = null!;
	public string GoogleAddress { get; init; } = null!;
	public DateTime CreatedAt { get; init; }
	public PostStatus? Status { get; init; }
	public ApprovalStatus ReviewStatus { get; init; }
	public decimal Cost { get; init; }
	public decimal Raised { get; init; }
	public string CreatorId { get; init; } = null!;
	public string CreatorName { get; init; } = null!;
	public string? CreatorImage { get; init; }
	public string? ReviewerId { get; init; }
	public string? ReviewerName { get; init; }
	public string? ReviewerImage { get; init; }
}

public class PostScopeOption
{
	public string Value { get; init; } = null!;
	public string Label { get; init; } = null!;
}

public class PostCreatorOption
{
	public string Id { get; init; } = null!;
	public string Name { get; init; } = null!;
	public string? Image { get; init; }
}
