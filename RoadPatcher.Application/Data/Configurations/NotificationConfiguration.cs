using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        // Primary Key
        builder.HasKey(n => n.Id);

        // User Configuration
        builder.Property(n => n.UserId)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Delete notifications when user is deleted

        // Content Configuration
        builder.Property(n => n.Content)
            .IsRequired()
            .HasMaxLength(500);

        // IsRead Configuration
        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        // Date Configuration
        builder.Property(n => n.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(n => n.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes for performance
        // Index on UserId + IsRead for fast unread count queries
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("IX_Notification_UserId_IsRead");

        // Index on CreatedAt DESC for sorting latest notifications first
        builder.HasIndex(n => n.CreatedAt)
            .IsDescending()
            .HasDatabaseName("IX_Notification_CreatedAt_Desc");
    }
}
