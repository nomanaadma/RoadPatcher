using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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
using RoadPatcher.Services;
using RoadPatcher.Hubs;
using Stripe;
using CSnakes.Runtime;

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
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultTokenProviders();

var authBuilder = builder.Services.AddAuthentication();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
	authBuilder.AddGoogle("Google", options =>
	{
		options.SignInScheme = IdentityConstants.ExternalScheme;
		options.ClientId = googleClientId;
		options.ClientSecret = googleClientSecret;
		options.CallbackPath = "/signin-google";
		options.Scope.Add("email");
		options.Scope.Add("profile");
		options.SaveTokens = true;
		options.Events.OnRemoteFailure = context =>
		{
			var message = context.Failure?.Message ?? "Google sign-in was canceled or denied.";
			context.Response.Redirect($"/Identity/Account/ExternalLogin?handler=Callback&remoteError={Uri.EscapeDataString(message)}");
			context.HandleResponse();
			return Task.CompletedTask;
		};
	});
}

var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
	authBuilder.AddFacebook("Facebook", options =>
	{
		options.SignInScheme = IdentityConstants.ExternalScheme;
		options.AppId = facebookAppId;
		options.AppSecret = facebookAppSecret;
		options.CallbackPath = "/signin-facebook";
		options.Fields.Add("name");
		options.Fields.Add("email");
		options.Fields.Add("first_name");
		options.Fields.Add("last_name");
		options.Scope.Add("email");
		options.SaveTokens = true;
		options.Events.OnRemoteFailure = context =>
		{
			var message = context.Failure?.Message ?? "Facebook sign-in was canceled or denied.";
			context.Response.Redirect($"/Identity/Account/ExternalLogin?handler=Callback&remoteError={Uri.EscapeDataString(message)}");
			context.HandleResponse();
			return Task.CompletedTask;
		};
	});
}

var xClientId = builder.Configuration["Authentication:X:ClientId"];
var xClientSecret = builder.Configuration["Authentication:X:ClientSecret"];
if (!string.IsNullOrWhiteSpace(xClientId) && !string.IsNullOrWhiteSpace(xClientSecret))
{
	authBuilder.AddTwitter(twitterOptions =>
	{
		twitterOptions.SignInScheme = IdentityConstants.ExternalScheme;
		twitterOptions.ConsumerKey = xClientId;
		twitterOptions.ConsumerSecret = xClientSecret;
		twitterOptions.RetrieveUserDetails = true;
		twitterOptions.SaveTokens = true;
		twitterOptions.ClaimActions.Add(new JsonKeyClaimAction("urn:twitter:profilepicture", ClaimValueTypes.String, "profile_image_url_https"));
		twitterOptions.ClaimActions.Add(new JsonKeyClaimAction("urn:twitter:profilepicture", ClaimValueTypes.String, "profile_image_url"));
		twitterOptions.Events.OnRemoteFailure = context =>
		{
			var message = context.Failure?.Message ?? "Twitter sign-in was canceled or denied.";
			context.Response.Redirect($"/Identity/Account/ExternalLogin?handler=Callback&remoteError={Uri.EscapeDataString(message)}");
			context.HandleResponse();
			return Task.CompletedTask;
		};
	});

}


builder.Services.AddControllersWithViews()
	.AddNewtonsoftJson(options =>
	{
		options.SerializerSettings.ContractResolver =
			new Newtonsoft.Json.Serialization.DefaultContractResolver
			{
				NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
			};
	});

builder.Services.AddSignalR();

// Notification service
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddScoped<IKTTheme, KTTheme>();
builder.Services.AddSingleton<IKTBootstrapBase, KTBootstrapBase>();

builder.Services.AddTransient<IEmailSender, EmailSender>();

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


builder.Services
	.WithPython()
	.WithHome(Path.Join(Environment.CurrentDirectory, "RoadPatcher.AIML"))
	.FromRedistributable();

var app = builder.Build();

// var env = app.Services.GetRequiredService<IPythonEnvironment>();

// var module = env.Hello();

// var greeting = module.AddNumbers(5, 7);
// Console.WriteLine(greeting); // Output: Hello, Alice!


using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = [ "SuperAdmin", "Admin", "User" ];

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}


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

// Map SignalR hubs
app.MapHub<NotificationHub>("/hubs/notification");

// Map area routes first (more specific)
app.MapControllerRoute(
	name: "admin",
	pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
	.WithStaticAssets();

// Default route
app.MapControllerRoute(
		name: "default",
		pattern: "{controller=Home}/{action=Index}/{id?}")
	.WithStaticAssets();

app.MapRazorPages()
	.WithStaticAssets();

app.Run();