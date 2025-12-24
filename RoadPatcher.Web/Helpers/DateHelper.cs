namespace RoadPatcher.Helpers;

public static class DateHelper
{
	extension(DateTime date)
	{
		public string ToPrettyFormat()
		{
			return date.ToString("dd-MMM-yyyy hh:mm tt");
		}

		public string ToAgo()
		{
			var ts = DateTime.Now - date;

			if (ts.TotalHours < 24)
				return Format(ts.TotalHours, "hour");

			return ts.TotalDays switch
			{
				< 7 => Format(ts.TotalDays, "day"),
				< 28 => Format(ts.TotalDays / 7, "week"),
				< 365 => Format(ts.TotalDays / 30, "month"),
				_ => Format(ts.TotalDays / 365, "year")
			};
		}
		
	}
	
	private static string Format(double value, string unit)
	{
		var count = (int)value;
		return $"{count} {unit}{(count == 1 ? "" : "s")} ago";
	}
}