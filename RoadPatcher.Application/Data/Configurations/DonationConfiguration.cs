using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data.Configurations;

public class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
	public void Configure(EntityTypeBuilder<Donation> builder)
	{
		// Primary Key
		builder.HasKey(d => d.Id);

		// UserId
		builder.Property(p => p.UserId)
			.IsRequired()
			.HasMaxLength(64);

		// PostId
		builder.Property(d => d.PostId)
			.IsRequired();
		

		// PaymentIntentId
		builder.Property(d => d.PaymentIntentId)
			.IsRequired()
			.HasMaxLength(255);

		// PaymentMethodId (optional)
		builder.Property(d => d.PaymentMethodId)
			.HasMaxLength(255);

		// Status
		builder.Property(d => d.Status)
			.HasConversion<int>()
			.HasDefaultValue(DonationStatus.Pending)
			.IsRequired();

		// Timestamps
		builder.Property(d => d.CreatedAt)
			.HasDefaultValueSql("NOW()")
			.IsRequired();
		
		// Relationships
		builder.HasOne(d => d.User)
			.WithMany() // If ApplicationUser has a collection of Donations, replace with .WithMany(u => u.Donations)
			.HasForeignKey(d => d.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(d => d.Post)
			.WithMany() // If Post has a collection of Donations, replace with .WithMany(p => p.Donations)
			.HasForeignKey(d => d.PostId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}