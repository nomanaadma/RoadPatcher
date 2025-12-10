using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
	public void Configure(EntityTypeBuilder<Post> builder)
	{

		builder.HasKey(p => p.Id);

		builder.Property(p => p.UserId)
			.IsRequired()
			.HasMaxLength(64);

		builder.Property(p => p.Address)
			.HasMaxLength(255)
			.IsRequired();

		builder.Property(p => p.GoogleAddress)
			.HasMaxLength(255)
			.IsRequired();

		builder.Property(p => p.Description)
			.HasMaxLength(2000)
			.HasColumnType("text");

		builder.Property(p => p.Status)
			.HasConversion<int>()
			.HasDefaultValue(PostStatus.Pending)
			.IsRequired();

		// Image (Required)
		builder.Property(p => p.ImageId)
			.IsRequired();

		builder.HasOne(p => p.Image)
			.WithMany()
			.HasForeignKey(p => p.ImageId)
			.OnDelete(DeleteBehavior.Restrict);

		// Video (Required)
		builder.Property(p => p.VideoId)
			.IsRequired();

		builder.HasOne(p => p.Video)
			.WithMany()
			.HasForeignKey(p => p.VideoId)
			.OnDelete(DeleteBehavior.Restrict);
		
		// Defaults for counters
		builder.Property(p => p.PeopleDonated).HasDefaultValue(0);
		builder.Property(p => p.Likes).HasDefaultValue(0);
		builder.Property(p => p.Discussions).HasDefaultValue(0);
		builder.Property(p => p.Shares).HasDefaultValue(0);
		builder.Property(p => p.Saves).HasDefaultValue(0);
		
		builder.Property(p => p.AllowEdit)
			.HasDefaultValue(true);

		// Post → MediaFiles (optional on Media side)
		builder.HasMany(p => p.MediaFiles)
			.WithOne(m => m.Post)
			.HasForeignKey(m => m.PostId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(p => p.CreatedAt)
			.HasDefaultValueSql("NOW()");

		builder.Property(p => p.UpdatedAt)
			.HasDefaultValueSql("NOW()");
	}
}