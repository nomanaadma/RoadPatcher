using RoadPatcher._keenthemes;
using RoadPatcher._keenthemes.libs;

namespace RoadPatcher.Middlewares;

public class ThemeMiddleware(RequestDelegate next, ILogger<ThemeMiddleware> logger)
{
    private readonly ILogger<ThemeMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, IKTTheme theme, IKTBootstrapBase ktBootstrapBase)
    {
        ktBootstrapBase.Init(theme);
        await next(context);
    }
}

public static class ThemeMiddlewareExtensions
{
    public static IApplicationBuilder UseThemeMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ThemeMiddleware>();
    }
}