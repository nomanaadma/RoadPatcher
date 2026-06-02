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

		// Review/Approval status
		builder.Property(p => p.ReviewStatus)
			.HasConversion<int>()
			.HasDefaultValue(ApprovalStatus.InReview)
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

		builder.Property(p => p.CompletionImageId)
			.IsRequired(false);

		builder.HasOne(p => p.CompletionImage)
			.WithMany()
			.HasForeignKey(p => p.CompletionImageId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(p => p.InferenceImageId)
			.IsRequired(false);

		builder.HasOne(p => p.InferenceImage)
			.WithMany()
			.HasForeignKey(p => p.InferenceImageId)
			.OnDelete(DeleteBehavior.Restrict);
		
		builder.Property(p => p.Location)
			.HasColumnType("geography (point)");
		
		builder.HasIndex(p => p.Location)
			.HasMethod("gist");
		
		// Defaults for counters
		builder.Property(p => p.PeopleDonated).HasDefaultValue(0);
		builder.Property(p => p.Likes).HasDefaultValue(0);
		builder.Property(p => p.Discussions).HasDefaultValue(0);
		builder.Property(p => p.Shares).HasDefaultValue(0);
		builder.Property(p => p.Favourites).HasDefaultValue(0);

		// Post → MediaFiles (optional on Media side)
		builder.HasMany(p => p.MediaFiles)
			.WithOne(m => m.Post)
			.HasForeignKey(m => m.PostId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(p => p.CreatedAt)
			.HasDefaultValueSql("NOW()");

		builder.Property(p => p.UpdatedAt)
			.HasDefaultValueSql("NOW()");

		// Reviewer (nullable) - the admin who claimed/reviewing the post
		builder.Property(p => p.ReviewerId)
			.HasMaxLength(64)
			.IsRequired(false);

		builder.HasOne(p => p.Reviewer)
			.WithMany()
			.HasForeignKey(p => p.ReviewerId)
			.OnDelete(DeleteBehavior.Restrict);

	}
}