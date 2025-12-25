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
			Fullname = c.User!.UserData!.FirstName + " " + c.User.UserData.LastName,
			ProfilePictureUrl = ProfileHelper.GetProfileImage(c.User.UserData.Image),
			Content = c.Content,
			Created = c.CreatedAt
		};
}