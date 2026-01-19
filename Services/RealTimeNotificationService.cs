using Bangaliyana.Hubs;
using Bangaliyana.Models;
using Microsoft.AspNetCore.SignalR;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Real-time notification service implementation using SignalR
    /// </summary>
    public class RealTimeNotificationService : IRealTimeNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<RealTimeNotificationService> _logger;

        public RealTimeNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<RealTimeNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendToUserAsync(string userId, NotificationDto notification)
        {
            try
            {
                await _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("ReceiveNotification", notification);

                _logger.LogDebug("Sent notification to user {UserId}: {Title}", userId, notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
            }
        }

        public async Task SendToRoleAsync(string roleName, NotificationDto notification)
        {
            try
            {
                await _hubContext.Clients.Group($"role_{roleName}")
                    .SendAsync("ReceiveNotification", notification);

                _logger.LogDebug("Sent notification to role {RoleName}: {Title}", roleName, notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to role {RoleName}", roleName);
            }
        }

        public async Task SendToRolesAsync(string[] roleNames, NotificationDto notification)
        {
            var tasks = roleNames.Select(role => SendToRoleAsync(role, notification));
            await Task.WhenAll(tasks);
        }

        public async Task SendToSellerAsync(int sellerId, NotificationDto notification)
        {
            try
            {
                await _hubContext.Clients.Group($"seller_{sellerId}")
                    .SendAsync("ReceiveNotification", notification);

                _logger.LogDebug("Sent notification to seller {SellerId}: {Title}", sellerId, notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to seller {SellerId}", sellerId);
            }
        }

        public async Task UpdateUnreadCountAsync(string userId, int count)
        {
            try
            {
                await _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("UpdateUnreadCount", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update unread count for user {UserId}", userId);
            }
        }

        public NotificationDto ToDto(UserNotification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Icon = notification.Icon,
                IconColor = notification.IconColor,
                Title = notification.Title,
                Message = notification.Message,
                Link = notification.Link,
                OrderId = notification.OrderId,
                TicketId = notification.TicketId,
                PromoCode = notification.PromoCode,
                CreatedAt = notification.CreatedAt
            };
        }
    }
}
