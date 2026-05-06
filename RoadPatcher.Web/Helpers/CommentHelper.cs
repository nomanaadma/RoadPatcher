using System.Linq.Expressions;
using RoadPatcher.Application.Entities;
using RoadPatcher.Models;

namespace RoadPatcher.Helpers;

public static class CommentHelper
{
	public static Expression<Func<Comment, CommentDto>> ToDto =>
		c => new CommentDto
		{
			Id = c.Id,
			PostId = c.PostId,
			Parent = c.ParentId,
			Fullname = UserHelper.GetFullName(c.User!.UserData!),
			ProfilePictureUrl = UserHelper.GetProfileImage(c.User.UserData!.Image),
			Content = c.Content,
			Created = c.CreatedAt
		};
}