using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Data Transfer Object for real-time notifications
    /// </summary>
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = "general";
        public string Icon { get; set; } = "fa-bell";
        public string IconColor { get; set; } = "text-primary";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Link { get; set; }
        public int? OrderId { get; set; }
        public int? TicketId { get; set; }
        public string? PromoCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Interface for real-time notification service using SignalR
    /// </summary>
    public interface IRealTimeNotificationService
    {
        /// <summary>
        /// Send notification to a specific user
        /// </summary>
        Task SendToUserAsync(string userId, NotificationDto notification);

        /// <summary>
        /// Send notification to all users with a specific role
        /// </summary>
        Task SendToRoleAsync(string roleName, NotificationDto notification);

        /// <summary>
        /// Send notification to multiple roles
        /// </summary>
        Task SendToRolesAsync(string[] roleNames, NotificationDto notification);

        /// <summary>
        /// Send notification to a specific seller
        /// </summary>
        Task SendToSellerAsync(int sellerId, NotificationDto notification);

        /// <summary>
        /// Update unread count for a user
        /// </summary>
        Task UpdateUnreadCountAsync(string userId, int count);

        /// <summary>
        /// Convert UserNotification to NotificationDto
        /// </summary>
        NotificationDto ToDto(UserNotification notification);
    }
}
