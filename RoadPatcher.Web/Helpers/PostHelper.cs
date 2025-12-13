namespace RoadPatcher.Helpers;

public static class PostHelper
{
	public static decimal GetRaisedPercentage(decimal cost, decimal raised)
	{
		return 100;
		return Math.Round((raised / cost) * 100, 2);
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
		return Math.Max(31, Math.Min(raisedPercentage, 69));
	}

	public static decimal GetTooltipProgressPercentage(decimal raisedPercentage)
	{
		return Math.Max(5, Math.Min(raisedPercentage, 95));
	}
}