using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Bangaliyana.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time notifications
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to their personal group (for user-specific notifications)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                // Add user to role-based groups for role-specific notifications
                if (Context.User?.IsInRole("SuperAdmin") == true)
                    await Groups.AddToGroupAsync(Context.ConnectionId, "role_SuperAdmin");
                if (Context.User?.IsInRole("Admin") == true)
                    await Groups.AddToGroupAsync(Context.ConnectionId, "role_Admin");
                if (Context.User?.IsInRole("Moderator") == true)
                    await Groups.AddToGroupAsync(Context.ConnectionId, "role_Moderator");
                if (Context.User?.IsInRole("Seller") == true)
                    await Groups.AddToGroupAsync(Context.ConnectionId, "role_Seller");
                if (Context.User?.IsInRole("User") == true)
                    await Groups.AddToGroupAsync(Context.ConnectionId, "role_User");

                _logger.LogInformation("User {UserId} connected to NotificationHub with ConnectionId {ConnectionId}",
                    userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} disconnected from NotificationHub. Reason: {Reason}",
                    userId, exception?.Message ?? "Normal disconnect");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Client can call this to join a seller-specific group (for seller order notifications)
        /// </summary>
        public async Task JoinSellerGroup(int sellerId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"seller_{sellerId}");
                _logger.LogDebug("User {UserId} joined seller group {SellerId}", userId, sellerId);
            }
        }

        /// <summary>
        /// Client can call this to leave a seller group
        /// </summary>
        public async Task LeaveSellerGroup(int sellerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"seller_{sellerId}");
        }

        /// <summary>
        /// Client heartbeat to maintain connection
        /// </summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }
    }
}
