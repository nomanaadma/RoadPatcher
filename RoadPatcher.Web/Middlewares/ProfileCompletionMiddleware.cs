using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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


            // Get the user id from claims
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                await next(context);
                return;
            }


            var isDisabled = await db.Users
                .AnyAsync(u => u.Id == userId && u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow);

            if (isDisabled)
            {
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                context.Response.Redirect("/Identity/Account/Login?error=disabled");
                return;
            }



            var path = context.Request.Path.Value ?? string.Empty;

            // Define the ONLY paths that require a completed profile
            string[] restrictedPrefixes =
            [
                "/Posts/Pending",
                "/Posts/InProcess",
                "/Posts/Completed",
                "/Posts/Favourited",
                "/Posts/Donated",
                "/Posts/Search",
            ];


            // If the path DOES NOT match our restricted list, allow it through immediately
            if (!restrictedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await next(context);
                return;
            }


            // --- Restricted Route Logic ---
            // The user is trying to access one of the restricted /Posts/ endpoints.
            // Check whether user has saved profile data.
            var hasProfile = await db.UserData.AnyAsync(u => u.UserId == userId);

            if (!hasProfile)
            {
                // Redirect to EditProfile with required flag
                const string redirectUrl = "/Settings/EditProfile?required=true";
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
