namespace RoadPatcher.Models;

public class CreatePaymentIntentRequest
{
	public int PostId { get; set; }
	public decimal Amount { get; set; }
}

public class CreatePaymentIntentResponse
{
	public string ClientSecret { get; set; } = string.Empty;
	public string PaymentIntentId { get; set; } = string.Empty;
	public decimal Amount { get; set; } = 0;
}

public class ConfirmDonationRequest
{
	public int PostId { get; set; }
	public string PaymentIntentId { get; set; } = string.Empty;
}