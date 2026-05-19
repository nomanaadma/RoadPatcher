using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data.Configurations;

public class PointsTransactionConfiguration : IEntityTypeConfiguration<PointsTransaction>
{
    public void Configure(EntityTypeBuilder<PointsTransaction> builder)
    {
        builder.ToTable("PointsTransactions");

        builder.HasKey(pt => pt.Id);

        builder.HasOne(pt => pt.User)
            .WithMany(u => u.PointsTransactions)
            .HasForeignKey(pt => pt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(pt => pt.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pt => pt.Points).IsRequired();

        builder.Property(pt => pt.CreatedAt).IsRequired();
    }
}
