namespace RoadPatcher.Helpers;

public static class DateHelper
{
	private static readonly TimeZoneInfo PakistanTimeZone = GetPakistanTimeZone();

	private static TimeZoneInfo GetPakistanTimeZone()
	{
		// Try common Windows and Linux timezone identifiers, fall back to UTC
		try
		{
			return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
		}
		catch
		{
			try
			{
				return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");
			}
			catch
			{
				return TimeZoneInfo.Utc;
			}
		}
	}

	private static DateTime ConvertToPakistanTime(DateTime date)
	{
		if (date.Kind == DateTimeKind.Unspecified)
		{
			date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
		}

		if (date.Kind == DateTimeKind.Local)
		{
			date = date.ToUniversalTime();
		}

		return TimeZoneInfo.ConvertTimeFromUtc(date, PakistanTimeZone);
	}

	public static string ToPrettyFormat(this DateTime date)
	{
		return ConvertToPakistanTime(date).ToString("dd-MMM-yyyy hh:mm tt");
	}

	public static string ToAgo(this DateTime date)
	{
		var pakistanNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PakistanTimeZone);
		var pakistanDate = ConvertToPakistanTime(date);
		var ts = pakistanNow - pakistanDate;

		if (ts.TotalMinutes < 1)
			return "Just now";

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

	// Non-extension overloads for callers using DateHelper.ToPrettyFormat(date)

	private static string Format(double value, string unit)
	{
		var count = (int)value;
		return $"{count} {unit}{(count == 1 ? "" : "s")} ago";
	}
}