namespace RoadPatcher.Application.Entities;

public class Donation
{
    public int Id { get; init; }

    public int PostId { get; init; }
    public Post? Post { get; init; }

    public required string UserId { get; init; }
    public ApplicationUser? User { get; init; }

    // Amount in PKR (decimal representation stored in DB). For Stripe we send integer paisa = (long)(Amount * 100)
    public decimal Amount { get; init; }

    // Stripe PaymentIntent id for idempotency and lookup
    public required string PaymentIntentId { get; init; }

    public string? PaymentMethodId { get; init; }

    public DonationStatus Status { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? SucceededAt { get; init; }
}

