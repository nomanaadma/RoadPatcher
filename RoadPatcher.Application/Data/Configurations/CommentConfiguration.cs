using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
	public void Configure(EntityTypeBuilder<Comment> builder)
	{

		// Primary Key
		builder.HasKey(c => c.Id);

		// User Configuration (Matched style from Favourite)
		builder.Property(c => c.UserId)
			.IsRequired()
			.HasMaxLength(64);

		builder.HasOne(c => c.User)
			.WithMany()
			.HasForeignKey(c => c.UserId)
			.OnDelete(DeleteBehavior.Restrict); // Prevent user deletion if they have comments (optional)

		// Post Configuration
		builder.Property(c => c.PostId)
			.IsRequired();

		builder.HasOne(c => c.Post)
			.WithMany() // Assuming Post has a collection of Comments, usually .WithMany(p => p.Comments)
			.HasForeignKey(c => c.PostId)
			.OnDelete(DeleteBehavior.Cascade); // Usually, if a Post is deleted, comments should go with it.

		// Content Configuration
		builder.Property(c => c.Content)
			.IsRequired()
			.HasMaxLength(2000); // Good practice to set a limit for text fields

		// Date Configuration
		builder.Property(c => c.CreatedAt)
			.HasDefaultValueSql("NOW()");

		// --- Self-Referencing Relationship (Parent/Replies) ---
		builder.HasOne(c => c.Parent)
			.WithMany(c => c.Replies)
			.HasForeignKey(c => c.ParentId)
			.IsRequired(false) // ParentId is nullable
			.OnDelete(DeleteBehavior.Restrict); 
		
		// Note: DeleteBehavior.Restrict is safest here to avoid 
		// "Multiple Cascade Paths" errors in SQL Server.
		
	}

}