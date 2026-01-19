using Microsoft.AspNetCore.Identity;
using Bangaliyana.Models;

namespace Bangaliyana.Middleware
{
    /// <summary>
    /// Middleware that ensures SuperAdmin users always have all roles assigned.
    /// This runs once per session to verify and fix role assignments.
    /// </summary>
    public class SuperAdminRoleMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string[] _superAdminEmails;

        // Default fallback SuperAdmin emails if configuration is not set
        private static readonly string[] DefaultSuperAdminEmails = new[]
        {
            "admin@bangaliyana.com"
        };

        public SuperAdminRoleMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _superAdminEmails = configuration.GetSection("SuperAdminSettings:SuperAdminEmails").Get<string[]>() ?? DefaultSuperAdminEmails;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userEmail = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? context.User.FindFirst("email")?.Value;

                // Check if user is a SuperAdmin by email
                if (!string.IsNullOrEmpty(userEmail) && _superAdminEmails.Contains(userEmail, StringComparer.OrdinalIgnoreCase))
                {
                    // Check session flag to avoid repeated DB calls
                    var sessionKey = $"SuperAdminRolesVerified_{userEmail}";
                    if (context.Session.GetString(sessionKey) != "true")
                    {
                        var user = await userManager.FindByEmailAsync(userEmail);
                        if (user != null)
                        {
                            await EnsureSuperAdminRolesAsync(userManager, user);
                            context.Session.SetString(sessionKey, "true");
                        }
                    }
                }
            }

            await _next(context);
        }

        private static async Task EnsureSuperAdminRolesAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
        {
            var allRoles = new[] { "SuperAdmin", "Admin", "Moderator", "Seller", "User" };
            foreach (var role in allRoles)
            {
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }

    public static class SuperAdminRoleMiddlewareExtensions
    {
        public static IApplicationBuilder UseSuperAdminRoleMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SuperAdminRoleMiddleware>();
        }
    }
}
