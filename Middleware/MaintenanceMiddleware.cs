using Bangaliyana.Services;

namespace Bangaliyana.Middleware
{
    /// <summary>
    /// Middleware to show maintenance page when site is in maintenance mode
    /// Admins can still access the site during maintenance
    /// </summary>
    public class MaintenanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MaintenanceMiddleware> _logger;

        public MaintenanceMiddleware(RequestDelegate next, ILogger<MaintenanceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISiteSettingsService siteSettingsService)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Skip maintenance check for:
            // - Static files (css, js, images, etc.)
            // - Admin area (so admins can turn off maintenance mode)
            // - Identity/Account (so admins can login)
            // - The maintenance page itself
            // - API calls for admin functionality
            if (path.StartsWith("/css/") ||
                path.StartsWith("/js/") ||
                path.StartsWith("/images/") ||
                path.StartsWith("/lib/") ||
                path.StartsWith("/admin") ||
                path.StartsWith("/identity") ||
                path.StartsWith("/maintenance") ||
                path.Contains("."))
            {
                await _next(context);
                return;
            }

            // Check if user is admin - admins can bypass maintenance mode
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var isAdmin = context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin");
                if (isAdmin)
                {
                    await _next(context);
                    return;
                }
            }

            // Check maintenance mode
            var siteSettings = await siteSettingsService.GetSiteSettingsAsync();
            if (siteSettings?.IsMaintenanceMode == true)
            {
                _logger.LogInformation("Site is in maintenance mode. Redirecting user to maintenance page.");
                context.Response.Redirect("/Maintenance");
                return;
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method to register MaintenanceMiddleware
    /// </summary>
    public static class MaintenanceMiddlewareExtensions
    {
        public static IApplicationBuilder UseMaintenance(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MaintenanceMiddleware>();
        }
    }
}
