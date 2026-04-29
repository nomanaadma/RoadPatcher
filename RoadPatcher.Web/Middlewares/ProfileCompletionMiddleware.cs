using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RoadPatcher.Application.Data;

namespace RoadPatcher.Middlewares;

public class ProfileCompletionMiddleware(RequestDelegate next, ILogger<ProfileCompletionMiddleware> logger)
{
	public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        try
        {
            var user = context.User;
            // context.User and Identity are non-nullable; check IsAuthenticated directly
            if (user.Identity?.IsAuthenticated != true)
            {
                await next(context);
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;

            // Allow static assets, identity endpoints and some common public paths
            string[] allowedPrefixes =
            [
	            "/css",
                "/js",
                "/lib",
                "/images",
                "/img",
                "/uploads",
                "/favicon.ico",
                "/robots.txt",
                "/_framework",
                "/_content",
                "/Identity",
                "/Home/Error",
                "/Account",
                "/Admin"
            ];

            if (allowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await next(context);
                return;
            }

            // Allow the EditProfile page (GET and POST)
            if (path.StartsWith("/Settings/EditProfile", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            // Get the user id from claims
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                await next(context);
                return;
            }

            // Check whether user has saved profile data.
            // Assumption: having a UserData record means profile was completed/saved.
            var hasProfile = await db.UserData.AnyAsync(u => u.UserId == userId);

            if (!hasProfile)
            {
                // Redirect to EditProfile. Preserve original requested URL in returnUrl query.
                const string redirectUrl = "/Settings/EditProfile";
                
                context.Response.Redirect(redirectUrl);
                return;
            }

            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ProfileCompletionMiddleware");
            await next(context);
        }
    }
}

public static class ProfileCompletionMiddlewareExtensions
{
    public static IApplicationBuilder UseProfileCompletionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ProfileCompletionMiddleware>();
    }
}
