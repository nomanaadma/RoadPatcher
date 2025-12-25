using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Entities;

namespace RoadPatcher.Application.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
	: IdentityDbContext<ApplicationUser>(options)
{
	public DbSet<UserData> UserData => Set<UserData>();
	public DbSet<Post> Posts => Set<Post>();
	public DbSet<Media> Media => Set<Media>();
	public DbSet<Like> Likes => Set<Like>();

	public DbSet<Favourite> Favourites => Set<Favourite>();

	public DbSet<Comment> Comments => Set<Comment>();
	

	protected override void OnModelCreating(ModelBuilder builder)
	{

		base.OnModelCreating(builder);

		builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

	}
	
}
