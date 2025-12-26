namespace RoadPatcher.Application.Entities;

public enum DonationStatus
{
    Completed,
    Failed
}

public class Donation
{
    public int Id { get; init; }

    public int PostId { get; init; }
    public Post? Post { get; init; }

    public string? UserId { get; init; }
    public ApplicationUser? User { get; init; }

    // Amount in PKR (decimal representation stored in DB). For Stripe we send integer paisa = (long)(Amount * 100)
    public decimal Amount { get; init; }

    // Stripe PaymentIntent id for idempotency and lookup
    public required string PaymentIntentId { get; init; }

    public string? PaymentMethodId { get; init; }

    public DonationStatus Status { get; set; }

    public string? CardLast4 { get; init; }

    public string? ReceiptUrl { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SucceededAt { get; init; }
}

