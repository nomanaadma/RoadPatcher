namespace RoadPatcher.Helpers;

public static class DateHelper
{
	public static string ToPrettyFormat(this DateTime date)
	{
		return date.ToString("dd-MMM-yyyy hh:mm tt");
	}
}