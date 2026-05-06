using Microsoft.AspNetCore.Html;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Helpers;

public static class PostHelper
{
	public static decimal GetRaisedPercentage(decimal cost, decimal raised)
	{
		return Math.Round(raised / cost * 100, 2);
	}
	
	public static decimal GetRemaining(decimal cost, decimal raised)
	{
		return cost - raised;
	}
	
	public static string GetProgressClass(decimal percentage)
	{
		return percentage switch
		{
			0 => "secondary",
			<= 20 => "danger",
			<= 35 => "warning",
			<= 50 => "primary",
			<= 65 => "dark",
			<= 80 => "info",
			_ => "success"
		};
	}
	
	public static decimal GetTooltipBoxPercentage(decimal raisedPercentage)
	{
		return Math.Max(29, Math.Min(raisedPercentage, 71));
	}

	public static PostStatus InvertStatus(PostStatus status)
	{
		#pragma warning disable CS8509
		return status switch
		{
			PostStatus.Completed => PostStatus.InProcess,
			PostStatus.InProcess => PostStatus.Completed,
		};
	}
	
	public static IHtmlContent RenderBtn(PostStatus status, int postId)
	{

		var statusName = status.ToString();
		
		#pragma warning disable CS8509
		var currentStatus = InvertStatus(status).ToString();

		var btnClass = $"mark-{statusName.ToLower()}-btn";
		var btnButtonClass = $"mark-{statusName.ToLower()}-btn-button";

		var icon = status switch
		{
			PostStatus.Completed => "las la-check-double",
			PostStatus.InProcess => "las la-redo-alt",
		};

		var label = $"Mark as {statusName.Replace("_", " ")}";

		var html = $"""
            <div class='mark-btn {btnClass} text-center'>
                <button class='btn btn-lg text-uppercase mark-status-btn fw-bolder {btnButtonClass}'
                        data-post-id='{postId}' data-status='{currentStatus}'>
                    <i class='{icon}'></i> {label}
                </button>
            </div>
        """;

		return new HtmlString(html);
	}
	
}