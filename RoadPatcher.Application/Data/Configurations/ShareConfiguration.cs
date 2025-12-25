using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data.Configurations;


public class ShareConfiguration : IEntityTypeConfiguration<Share>
{
	public void Configure(EntityTypeBuilder<Share> builder)
	{

		builder.HasKey(p => p.Id);

		builder.Property(p => p.UserId)
			.IsRequired()
			.HasMaxLength(64);
		
		builder.Property(p => p.PostId)
			.IsRequired();
		
		builder.HasOne(p => p.Post)
			.WithMany()
			.HasForeignKey(p => p.PostId)
			.OnDelete(DeleteBehavior.Restrict);
		
		builder.Property(p => p.CreatedAt)
			.HasDefaultValueSql("NOW()");
		
	}
	
}