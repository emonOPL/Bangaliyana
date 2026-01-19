using System.Security.Claims;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bangaliyana.Controllers
{
    /// <summary>
    /// API Controller for notification operations (polling fallback, mark as read, etc.)
    /// </summary>
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationApiController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IRealTimeNotificationService _realTimeService;
        private readonly ILogger<NotificationApiController> _logger;

        public NotificationApiController(
            INotificationService notificationService,
            IRealTimeNotificationService realTimeService,
            ILogger<NotificationApiController> logger)
        {
            _notificationService = notificationService;
            _realTimeService = realTimeService;
            _logger = logger;
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        /// <summary>
        /// Get recent notifications (for dropdown or polling)
        /// </summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentNotifications([FromQuery] int limit = 5)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            var recent = notifications.Take(limit).Select(n => new
            {
                n.Id,
                n.Type,
                n.Icon,
                n.IconColor,
                n.Title,
                n.Message,
                n.Link,
                n.IsRead,
                n.OrderId,
                n.CreatedAt,
                TimeAgo = GetTimeAgo(n.CreatedAt)
            });

            return Ok(recent);
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId);

            // Update unread count via SignalR
            var count = await _notificationService.GetUnreadCountAsync(userId);
            await _realTimeService.UpdateUnreadCountAsync(userId, count);

            return Ok(new { success = true });
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId);

            // Update unread count to 0 via SignalR
            await _realTimeService.UpdateUnreadCountAsync(userId, 0);

            return Ok(new { success = true });
        }

        /// <summary>
        /// Fallback polling endpoint for older browsers
        /// </summary>
        [HttpGet("poll")]
        public async Task<IActionResult> PollNotifications([FromQuery] DateTime? since = null)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);

            if (since.HasValue)
            {
                notifications = notifications.Where(n => n.CreatedAt > since.Value).ToList();
            }

            var count = await _notificationService.GetUnreadCountAsync(userId);

            return Ok(new
            {
                notifications = notifications.Take(10).Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Icon,
                    n.IconColor,
                    n.Title,
                    n.Message,
                    n.Link,
                    n.IsRead,
                    n.OrderId,
                    n.CreatedAt
                }),
                unreadCount = count
            });
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;
            if (timeSpan.TotalMinutes < 1) return "Just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}d ago";
            return dateTime.ToString("dd MMM");
        }
    }
}
