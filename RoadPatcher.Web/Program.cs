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
	});

	// authBuilder.AddOAuth("X", options =>
	// {
	// 	options.SignInScheme = IdentityConstants.ExternalScheme;
	// 	options.ClientId = xClientId;
	// 	options.ClientSecret = xClientSecret;
	// 	options.CallbackPath = "/signin-x";
	// 	options.AuthorizationEndpoint = "https://x.com/i/oauth2/authorize";
	// 	options.TokenEndpoint = "https://api.x.com/2/oauth2/token";
	// 	options.UserInformationEndpoint = "https://api.x.com/2/users/me?user.fields=id,name,username";
	// 	options.Backchannel = new HttpClient(new XTokenAuthorizationHandler(xClientId, xClientSecret))
	// 	{
	// 		Timeout = TimeSpan.FromSeconds(60)
	// 	};
	// 	options.UsePkce = true;
	// 	options.SaveTokens = true;
	// 	options.Scope.Add("users.read");
	// 	options.Scope.Add("tweet.read");

	// 	options.ClaimActions.Clear();

	// 	options.Events = new OAuthEvents
	// 	{
	// 		OnCreatingTicket = async context =>
	// 		{
	// 			using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
	// 			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

	// 			using var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
	// 				context.HttpContext.RequestAborted);
	// 			response.EnsureSuccessStatusCode();

	// 			using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
	// 			var data = payload.RootElement.GetProperty("data");

	// 			if (data.TryGetProperty("id", out var idElement))
	// 			{
	// 				var id = idElement.GetString();
	// 				if (!string.IsNullOrWhiteSpace(id))
	// 				{
	// 					context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));
	// 				}
	// 			}

	// 			if (data.TryGetProperty("name", out var nameElement))
	// 			{
	// 				var name = nameElement.GetString();
	// 				if (!string.IsNullOrWhiteSpace(name))
	// 				{
	// 					context.Identity?.AddClaim(new Claim(ClaimTypes.Name, name));
	// 				}
	// 			}

	// 			if (data.TryGetProperty("username", out var usernameElement))
	// 			{
	// 				var username = usernameElement.GetString();
	// 				if (!string.IsNullOrWhiteSpace(username))
	// 				{
	// 					context.Identity?.AddClaim(new Claim("urn:x:username", username));
	// 				}
	// 			}
	// 		}
	// 	};
	// });
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