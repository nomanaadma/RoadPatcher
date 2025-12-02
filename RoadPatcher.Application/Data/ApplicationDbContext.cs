using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	: IdentityDbContext<ApplicationUser>(options)
{
	public DbSet<UserData> UserDatas => Set<UserData>();

	protected override void OnModelCreating(ModelBuilder builder)
	{

		base.OnModelCreating(builder);

		// Configure UserData table
		builder.Entity<UserData>(entity =>
		{
			entity.HasKey(e => e.UserId); // Primary Key

			// One-to-one relationship with ApplicationUser
			entity.HasOne(d => d.User)
				.WithOne(u => u.UserData)
				.HasForeignKey<UserData>(d => d.UserId)
				.IsRequired(false);
			
			entity.Property(e => e.UserId)
				.HasMaxLength(36)  // Match Identity default
				.IsRequired();

			entity.Property(e => e.FirstName)
				.HasMaxLength(50)
				.IsRequired();

			entity.Property(e => e.LastName)
				.HasMaxLength(50)
				.IsRequired();

			entity.Property(e => e.Address)
				.HasMaxLength(200);

			entity.Property(e => e.Latitude);
			entity.Property(e => e.Longitude);
			
			entity.Property(e => e.Image)
				.HasMaxLength(50);
			
		});

	}
	
}
