using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data.Configurations;

public class PostNotificationSubscriptionConfiguration : IEntityTypeConfiguration<PostNotificationSubscription>
{
    public void Configure(EntityTypeBuilder<PostNotificationSubscription> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PostId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Post)
            .WithMany()
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // A user should only be subscribed once per post.
        builder.HasIndex(x => new { x.PostId, x.UserId })
            .IsUnique()
            .HasDatabaseName("IX_PostNotificationSubscription_PostId_UserId");

        builder.HasIndex(x => x.PostId)
            .HasDatabaseName("IX_PostNotificationSubscription_PostId");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_PostNotificationSubscription_UserId");
    }
}
