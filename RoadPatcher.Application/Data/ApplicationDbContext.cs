using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data.Configurations;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	: IdentityDbContext<ApplicationUser>(options)
{
	public DbSet<UserData> UserData => Set<UserData>();
	public DbSet<Post> Posts => Set<Post>();
	public DbSet<Media> Media => Set<Media>();

	protected override void OnModelCreating(ModelBuilder builder)
	{

		base.OnModelCreating(builder);

		builder.ApplyConfiguration(new UserDataConfiguration());
		builder.ApplyConfiguration(new PostConfiguration());
		builder.ApplyConfiguration(new MediaConfiguration());

	}
	
}
