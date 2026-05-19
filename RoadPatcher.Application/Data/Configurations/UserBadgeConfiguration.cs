using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data.Configurations;

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("UserBadges");

        builder.HasKey(ub => ub.Id);

        builder.HasOne(ub => ub.User)
            .WithMany(u => u.UserBadges)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ub => ub.Badge)
            .WithMany()
            .HasForeignKey(ub => ub.BadgeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ub => ub.AwardedAt).IsRequired();
    }
}
