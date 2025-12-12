namespace RoadPatcher.Helpers;

public static class CurrencyHelper
{
	public static string ToPkr(this decimal amount)
	{
		return amount.ToString("#,0");
	} 
}