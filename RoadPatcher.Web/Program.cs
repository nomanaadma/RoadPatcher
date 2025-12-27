using System.Data.Common;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Npgsql;
using RoadPatcher.Middlewares;
using RoadPatcher._keenthemes;
using RoadPatcher._keenthemes.libs;
using RoadPatcher.Application.Data;
using RoadPatcher.Application.Entities;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
	options.UseNpgsql(connectionString, o => o.UseNetTopologySuite());
	options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
	.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews()
	.AddNewtonsoftJson(options =>
	{
		options.SerializerSettings.ContractResolver =
			new Newtonsoft.Json.Serialization.DefaultContractResolver
			{
				NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
			};
	});

builder.Services.AddScoped<IKTTheme, KTTheme>();
builder.Services.AddSingleton<IKTBootstrapBase, KTBootstrapBase>();

builder.Services.AddSingleton<GeometryFactory>(sp => 
    NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326));

var themeConfiguration = new ConfigurationBuilder()
	.AddJsonFile("_keenthemes/config/themesettings.json")
	.Build();

KTThemeSettings.init(themeConfiguration);

DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);

// Configure Stripe (if keys present in configuration)
var stripeSecret = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrWhiteSpace(stripeSecret))
{
	StripeConfiguration.ApiKey = stripeSecret;
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Error/500");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();


var provider = new FileExtensionContentTypeProvider
{
	Mappings =
	{
		[".mp4"] = "video/mp4"
	}
};

app.UseStaticFiles(new StaticFileOptions
{
	ContentTypeProvider = provider,
	ServeUnknownFileTypes = true
});


app.UseRouting();

// Add authentication middleware so we can check authenticated users in our custom middleware
app.UseAuthentication();

// Ensure users complete their profile before accessing other routes
app.UseProfileCompletionMiddleware();

app.UseAuthorization();

app.UseThemeMiddleware();

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.MapStaticAssets();

app.MapControllerRoute(
		name: "default",
		pattern: "{controller=Home}/{action=Index}/{id?}")
	.WithStaticAssets();

app.MapRazorPages()
	.WithStaticAssets();

app.Run();