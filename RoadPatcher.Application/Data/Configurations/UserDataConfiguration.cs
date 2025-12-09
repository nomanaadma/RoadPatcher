using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data.Configurations;

public class UserDataConfiguration : IEntityTypeConfiguration<UserData>
{
	public void Configure(EntityTypeBuilder<UserData> builder)
	{
		builder.HasKey(e => e.UserId); // Primary Key

		// One-to-one relationship with ApplicationUser
		builder.HasOne(d => d.User)
			.WithOne(u => u.UserData)
			.HasForeignKey<UserData>(d => d.UserId)
			.IsRequired(false);
			
		builder.Property(e => e.UserId)
			.HasMaxLength(36)  // Match Identity default
			.IsRequired();

		builder.Property(e => e.FirstName)
			.HasMaxLength(50)
			.IsRequired();

		builder.Property(e => e.LastName)
			.HasMaxLength(50)
			.IsRequired();

		builder.Property(e => e.Address)
			.HasMaxLength(200);
		
		builder.Property(e => e.GoogleAddress)
			.HasMaxLength(200);

		builder.Property(e => e.Latitude);
		builder.Property(e => e.Longitude);
			
		builder.Property(e => e.Image)
			.HasMaxLength(50);
	}
}

