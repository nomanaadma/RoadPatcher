using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
	public void Configure(EntityTypeBuilder<Media> builder)
	{
		builder.HasKey(m => m.Id);

		builder.Property(m => m.Name)
			.IsRequired()
			.HasMaxLength(200);

		builder.Property(m => m.PostId)
			.IsRequired(false);

		builder.Property(m => m.CreatedAt)
			.HasDefaultValueSql("NOW()");
	}
}