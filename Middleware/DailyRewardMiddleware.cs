using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Middleware
{
    /// <summary>
    /// Middleware to automatically award daily login reward points
    /// when an authenticated user browses the site for the first time each day
    /// </summary>
    public class DailyRewardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DailyRewardMiddleware> _logger;

        public DailyRewardMiddleware(RequestDelegate next, ILogger<DailyRewardMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            // Only process for authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // Skip for static files and API calls to reduce overhead
                var path = context.Request.Path.Value?.ToLower() ?? "";
                if (!path.Contains("/api/") &&
                    !path.StartsWith("/css/") &&
                    !path.StartsWith("/js/") &&
                    !path.StartsWith("/images/") &&
                    !path.StartsWith("/lib/") &&
                    !path.Contains("."))
                {
                    await TryAwardDailyRewardAsync(context, serviceProvider);
                }
            }

            await _next(context);
        }

        private async Task TryAwardDailyRewardAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            try
            {
                // Check session flag to avoid repeated DB calls within the same session
                var sessionKey = "DailyRewardChecked";
                var session = context.Session;

                // Try to get session value
                if (session.TryGetValue(sessionKey, out var _))
                {
                    return; // Already checked in this session
                }

                using var scope = serviceProvider.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var rewardService = scope.ServiceProvider.GetRequiredService<IRewardService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var user = await userManager.GetUserAsync(context.User);
                if (user == null) return;

                // Get today's date in Bangladesh Standard Time (UTC+6)
                var bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
                var nowInBD = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bdTimeZone);
                var todayInBD = nowInBD.Date;

                // Quick check: if already rewarded today, mark session and skip
                if (user.LastDailyRewardDate.HasValue)
                {
                    var lastRewardDateInBD = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(user.LastDailyRewardDate.Value, DateTimeKind.Utc),
                        bdTimeZone).Date;

                    if (lastRewardDateInBD >= todayInBD)
                    {
                        // Already rewarded today, mark session
                        session.SetInt32(sessionKey, 1);
                        return;
                    }
                }

                // Award daily login points
                var result = await rewardService.AwardDailyLoginPointsAsync(user.Id);

                if (result.Awarded)
                {
                    // Create notification for the user
                    var notification = new UserNotification
                    {
                        UserId = user.Id,
                        Type = "reward",
                        Icon = "fa-gift",
                        IconColor = "text-success",
                        Title = "Daily Login Reward!",
                        Message = "You earned 10 reward points for visiting today. Come back tomorrow for more!",
                        Link = "/Identity/Account/Manage/WalletRewards",
                        CreatedAt = DateTime.UtcNow
                    };

                    db.UserNotifications.Add(notification);
                    await db.SaveChangesAsync();

                    _logger.LogInformation("Daily reward awarded and notification created for user {UserId}", user.Id);
                }

                // Mark session as checked regardless of result
                session.SetInt32(sessionKey, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DailyRewardMiddleware");
                // Don't throw - middleware should not break the request pipeline
            }
        }
    }

    /// <summary>
    /// Extension method to register DailyRewardMiddleware
    /// </summary>
    public static class DailyRewardMiddlewareExtensions
    {
        public static IApplicationBuilder UseDailyReward(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DailyRewardMiddleware>();
        }
    }
}
