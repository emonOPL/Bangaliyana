using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public interface ISellerMessagingService
    {
        // Conversation Management
        Task<List<SellerConversation>> GetConversationsForBuyerAsync(string buyerId);
        Task<List<SellerConversation>> GetConversationsForSellerAsync(int sellerId);
        Task<SellerConversation?> GetConversationAsync(int conversationId);
        Task<SellerConversation?> GetOrCreateConversationAsync(int sellerId, string buyerId, int? orderId = null, int? productId = null, string? subject = null);

        // Message Management
        Task<List<SellerMessage>> GetMessagesAsync(int conversationId);
        Task<SellerMessage> SendMessageAsync(int conversationId, string senderId, bool isSeller, string message,
            string? attachmentUrl = null, string? attachmentName = null, string? attachmentType = null,
            int? sharedOrderId = null, int? sharedProductId = null, string messageType = "text");

        // Conversation Actions
        Task ReopenConversationAsync(int conversationId, bool reopenedBySeller);
        Task MarkMessagesAsReadAsync(int conversationId, bool markForSeller);

        // Unread Counts
        Task<int> GetUnreadCountForBuyerAsync(string buyerId);
        Task<int> GetUnreadCountForSellerAsync(int sellerId);

        // Eligibility Check
        Task<bool> CanSellerMessageBuyerAsync(int sellerId, string buyerId);
        Task<List<EligibleBuyer>> GetEligibleBuyersForSellerAsync(int sellerId);

        // Conversation Actions
        Task CloseConversationAsync(int conversationId, bool closedBySeller, string? closedByUserId = null, bool isStaff = false);
        Task ArchiveConversationAsync(int conversationId);

        // Start New Conversation
        Task<int> StartConversationAsync(int sellerId, string buyerId, int? productId, int? orderId, string subject, string initialMessage);
    }

    public class EligibleBuyer
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string EligibilityReason { get; set; } = string.Empty;  // "Order", "Question", "Ticket"
        public int? OrderId { get; set; }
        public int? ProductId { get; set; }
        public DateTime InteractionDate { get; set; }
    }

    public class SellerMessagingService : ISellerMessagingService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<SellerMessagingService> _logger;

        public SellerMessagingService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<SellerMessagingService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<List<SellerConversation>> GetConversationsForBuyerAsync(string buyerId)
        {
            return await _context.SellerConversations
                .Include(c => c.Seller)
                    .ThenInclude(s => s!.User)
                .Include(c => c.Order)
                .Include(c => c.Product)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => c.BuyerId == buyerId && !c.IsArchived)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        public async Task<List<SellerConversation>> GetConversationsForSellerAsync(int sellerId)
        {
            return await _context.SellerConversations
                .Include(c => c.Buyer)
                .Include(c => c.Order)
                .Include(c => c.Product)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => c.SellerId == sellerId && !c.IsArchived)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        public async Task<SellerConversation?> GetConversationAsync(int conversationId)
        {
            return await _context.SellerConversations
                .Include(c => c.Seller)
                    .ThenInclude(s => s!.User)
                .Include(c => c.Buyer)
                .Include(c => c.Order)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<SellerConversation?> GetOrCreateConversationAsync(int sellerId, string buyerId, int? orderId = null, int? productId = null, string? subject = null)
        {
            // Check for existing conversation with same context
            var existing = await _context.SellerConversations
                .FirstOrDefaultAsync(c => c.SellerId == sellerId &&
                                         c.BuyerId == buyerId &&
                                         c.OrderId == orderId &&
                                         c.ProductId == productId &&
                                         !c.IsArchived);

            if (existing != null)
            {
                return existing;
            }

            // Create new conversation
            var conversation = new SellerConversation
            {
                SellerId = sellerId,
                BuyerId = buyerId,
                OrderId = orderId,
                ProductId = productId,
                Subject = subject,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };

            _context.SellerConversations.Add(conversation);
            await _context.SaveChangesAsync();

            return conversation;
        }

        public async Task<List<SellerMessage>> GetMessagesAsync(int conversationId)
        {
            return await _context.SellerMessages
                .Include(m => m.Sender)
                .Include(m => m.SharedOrder)
                .Include(m => m.SharedProduct)
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<SellerMessage> SendMessageAsync(int conversationId, string senderId, bool isSeller, string message,
            string? attachmentUrl = null, string? attachmentName = null, string? attachmentType = null,
            int? sharedOrderId = null, int? sharedProductId = null, string messageType = "text")
        {
            var conversation = await _context.SellerConversations
                .Include(c => c.Seller)
                    .ThenInclude(s => s!.User)
                .Include(c => c.Buyer)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
            {
                throw new ArgumentException("Conversation not found");
            }

            var sellerMessage = new SellerMessage
            {
                ConversationId = conversationId,
                SenderId = senderId,
                IsSentBySeller = isSeller,
                Message = message,
                AttachmentUrl = attachmentUrl,
                AttachmentName = attachmentName,
                AttachmentType = attachmentType,
                SharedOrderId = sharedOrderId,
                SharedProductId = sharedProductId,
                MessageType = messageType,
                CreatedAt = DateTime.UtcNow
            };

            _context.SellerMessages.Add(sellerMessage);

            // Update conversation
            conversation.LastMessageAt = DateTime.UtcNow;
            if (isSeller)
            {
                conversation.UnreadCountBuyer++;
            }
            else
            {
                conversation.UnreadCountSeller++;
            }

            await _context.SaveChangesAsync();

            // Create notification for recipient
            if (isSeller)
            {
                // Notify buyer
                var shopName = conversation.Seller?.ShopName ?? "Seller";
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = conversation.BuyerId,
                    Type = "seller",
                    Icon = "fa-comment-dots",
                    IconColor = "text-info",
                    Title = $"New message from {shopName}",
                    Message = message.Length > 100 ? message.Substring(0, 97) + "..." : message,
                    Link = $"/Customer/Messages/Conversation/{conversationId}",
                    ConversationId = conversationId,
                    CreatedAt = DateTime.UtcNow
                });

                // Send email notification if enabled
                await SendMessageEmailNotificationAsync(
                    conversation.BuyerId,
                    conversation.Buyer?.Email,
                    shopName,
                    message,
                    $"/Customer/Messages/Conversation/{conversationId}",
                    false);
            }
            else
            {
                // Notify seller
                var buyerName = conversation.Buyer?.FullName ?? conversation.Buyer?.Email ?? "Buyer";
                var sellerUserId = conversation.Seller?.UserId;
                if (!string.IsNullOrEmpty(sellerUserId))
                {
                    await _notificationService.CreateNotificationAsync(new UserNotification
                    {
                        UserId = sellerUserId,
                        Type = "seller",
                        Icon = "fa-comment-dots",
                        IconColor = "text-info",
                        Title = $"New message from {buyerName}",
                        Message = message.Length > 100 ? message.Substring(0, 97) + "..." : message,
                        Link = $"/Admin/SellerMessages/Conversation/{conversationId}",
                        ConversationId = conversationId,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Send email notification if enabled
                    await SendMessageEmailNotificationAsync(
                        sellerUserId,
                        conversation.Seller?.User?.Email,
                        buyerName,
                        message,
                        $"/Admin/SellerMessages/Conversation/{conversationId}",
                        true);
                }
            }

            return sellerMessage;
        }

        public async Task MarkMessagesAsReadAsync(int conversationId, bool markForSeller)
        {
            var conversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null) return;

            // Mark messages as read
            var unreadMessages = await _context.SellerMessages
                .Where(m => m.ConversationId == conversationId &&
                           !m.IsRead &&
                           m.IsSentBySeller != markForSeller)
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.UtcNow;
            }

            // Reset unread count
            if (markForSeller)
            {
                conversation.UnreadCountSeller = 0;
            }
            else
            {
                conversation.UnreadCountBuyer = 0;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountForBuyerAsync(string buyerId)
        {
            return await _context.SellerConversations
                .Where(c => c.BuyerId == buyerId && !c.IsArchived)
                .SumAsync(c => c.UnreadCountBuyer);
        }

        public async Task<int> GetUnreadCountForSellerAsync(int sellerId)
        {
            return await _context.SellerConversations
                .Where(c => c.SellerId == sellerId && !c.IsArchived)
                .SumAsync(c => c.UnreadCountSeller);
        }

        public async Task<bool> CanSellerMessageBuyerAsync(int sellerId, string buyerId)
        {
            // Check if buyer has ordered from seller
            var hasOrdered = await _context.Orders
                .AnyAsync(o => o.UserId == buyerId &&
                              o.OrderItems.Any(oi => oi.SellerId == sellerId));

            if (hasOrdered) return true;

            // Check if buyer has asked a question on seller's product
            var hasAskedQuestion = await _context.ProductQuestions
                .AnyAsync(pq => pq.UserId == buyerId &&
                               pq.Product != null &&
                               pq.Product.SellerId == sellerId);

            if (hasAskedQuestion) return true;

            // Check if buyer has submitted a ticket related to seller's product/order
            var hasTicket = await _context.Tickets
                .AnyAsync(t => t.UserId == buyerId &&
                              t.Order != null &&
                              t.Order.OrderItems.Any(oi => oi.SellerId == sellerId));

            return hasTicket;
        }

        public async Task<List<EligibleBuyer>> GetEligibleBuyersForSellerAsync(int sellerId)
        {
            var eligibleBuyers = new List<EligibleBuyer>();

            // Get buyers from orders
            var orderBuyers = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.UserId != null &&
                           o.OrderItems.Any(oi => oi.SellerId == sellerId))
                .Select(o => new EligibleBuyer
                {
                    UserId = o.UserId!,
                    DisplayName = o.User != null ? (o.User.FullName ?? o.User.Email ?? "Unknown") : "Unknown",
                    Email = o.User != null ? o.User.Email : null,
                    PhoneNumber = o.User != null ? o.User.PhoneNumber : null,
                    EligibilityReason = "Order",
                    OrderId = o.Id,
                    InteractionDate = o.OrderDate
                })
                .ToListAsync();

            eligibleBuyers.AddRange(orderBuyers);

            // Get buyers from product questions
            var questionBuyers = await _context.ProductQuestions
                .Include(pq => pq.User)
                .Include(pq => pq.Product)
                .Where(pq => pq.Product != null &&
                            pq.Product.SellerId == sellerId)
                .Select(pq => new EligibleBuyer
                {
                    UserId = pq.UserId,
                    DisplayName = pq.User != null ? (pq.User.FullName ?? pq.User.Email ?? "Unknown") : "Unknown",
                    Email = pq.User != null ? pq.User.Email : null,
                    PhoneNumber = pq.User != null ? pq.User.PhoneNumber : null,
                    EligibilityReason = "Question",
                    ProductId = pq.ProductId,
                    InteractionDate = pq.CreatedAt
                })
                .ToListAsync();

            // Add only unique buyers (not already in list)
            foreach (var buyer in questionBuyers)
            {
                if (!eligibleBuyers.Any(b => b.UserId == buyer.UserId))
                {
                    eligibleBuyers.Add(buyer);
                }
            }

            return eligibleBuyers
                .GroupBy(b => b.UserId)
                .Select(g => g.OrderByDescending(b => b.InteractionDate).First())
                .OrderByDescending(b => b.InteractionDate)
                .ToList();
        }

        public async Task CloseConversationAsync(int conversationId, bool closedBySeller, string? closedByUserId = null, bool isStaff = false)
        {
            var conversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null) return;

            if (isStaff)
            {
                conversation.IsClosedByStaff = true;
            }
            else if (closedBySeller)
            {
                conversation.IsClosedBySeller = true;
            }
            else
            {
                conversation.IsClosedByBuyer = true;
            }

            conversation.ClosedByUserId = closedByUserId;

            await _context.SaveChangesAsync();
        }

        public async Task ReopenConversationAsync(int conversationId, bool reopenedBySeller)
        {
            var conversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null) return;

            // Reset all closed flags
            conversation.IsClosedBySeller = false;
            conversation.IsClosedByBuyer = false;
            conversation.IsClosedByStaff = false;
            conversation.ClosedByUserId = null;

            await _context.SaveChangesAsync();
        }

        public async Task ArchiveConversationAsync(int conversationId)
        {
            var conversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null) return;

            conversation.IsArchived = true;
            await _context.SaveChangesAsync();
        }

        public async Task<int> StartConversationAsync(int sellerId, string buyerId, int? productId, int? orderId, string subject, string initialMessage)
        {
            // Check if seller is suspended - block new conversations
            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sellerId);
            if (seller == null || seller.Status == SellerStatus.Suspended)
            {
                throw new InvalidOperationException("Cannot start conversation with this seller. The seller is currently unavailable.");
            }

            // Check if conversation already exists
            var existingConversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c =>
                    c.SellerId == sellerId &&
                    c.BuyerId == buyerId &&
                    c.ProductId == productId &&
                    c.OrderId == orderId &&
                    !c.IsArchived);

            if (existingConversation != null)
            {
                // Reopen if closed
                existingConversation.IsClosedByBuyer = false;
                existingConversation.IsClosedBySeller = false;

                // Send message to existing conversation
                await SendMessageAsync(existingConversation.Id, buyerId, false, initialMessage);
                return existingConversation.Id;
            }

            // Create new conversation
            var conversation = new SellerConversation
            {
                SellerId = sellerId,
                BuyerId = buyerId,
                ProductId = productId,
                OrderId = orderId,
                Subject = subject,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow,
                UnreadCountSeller = 1 // Initial message from buyer is unread by seller
            };

            _context.SellerConversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Add the initial message
            var message = new SellerMessage
            {
                ConversationId = conversation.Id,
                SenderId = buyerId,
                IsSentBySeller = false,
                Message = initialMessage,
                CreatedAt = DateTime.UtcNow
            };

            _context.SellerMessages.Add(message);
            await _context.SaveChangesAsync();

            // Notify seller (seller already loaded with User above)
            if (!string.IsNullOrEmpty(seller.UserId))
            {
                var buyer = await _context.Users.FindAsync(buyerId);
                var buyerName = buyer?.FullName ?? buyer?.Email ?? "A customer";

                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = seller.UserId,
                    Type = "seller",
                    Icon = "fa-comment-dots",
                    IconColor = "text-info",
                    Title = $"New message from {buyerName}",
                    Message = initialMessage.Length > 100 ? initialMessage.Substring(0, 97) + "..." : initialMessage,
                    Link = $"/Admin/SellerMessages/Conversation/{conversation.Id}",
                    ConversationId = conversation.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return conversation.Id;
        }

        /// <summary>
        /// Sends email notification for new messages if user has enabled it
        /// </summary>
        private async Task SendMessageEmailNotificationAsync(
            string recipientUserId,
            string? recipientEmail,
            string senderName,
            string messageContent,
            string viewLink,
            bool isRecipientSeller)
        {
            try
            {
                if (string.IsNullOrEmpty(recipientEmail))
                {
                    _logger.LogDebug("No email found for user {UserId}", recipientUserId);
                    return;
                }

                // Check user's notification preferences
                var preferences = await _context.NotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == recipientUserId);

                // Check if email notifications are enabled (both master toggle and SellerMessages)
                if (preferences != null && (!preferences.ReceiveEmails || !preferences.SellerMessages))
                {
                    _logger.LogDebug("Email notifications disabled for user {UserId}", recipientUserId);
                    return;
                }

                // Truncate message for email preview
                var truncatedMessage = messageContent.Length > 200
                    ? messageContent.Substring(0, 197) + "..."
                    : messageContent;

                var subject = $"New message from {senderName}";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0; font-size: 24px;'>New Message</h1>
    </div>
    <div style='background: #f8f9fa; padding: 20px; border: 1px solid #e9ecef;'>
        <p style='margin: 0 0 10px;'>You have received a new message from <strong>{senderName}</strong>:</p>
        <div style='background: white; padding: 15px; border-radius: 8px; border-left: 4px solid #667eea; margin: 15px 0;'>
            <p style='margin: 0; color: #555;'>{System.Net.WebUtility.HtmlEncode(truncatedMessage)}</p>
        </div>
        <p style='margin: 15px 0;'>
            <a href='{viewLink}' style='display: inline-block; background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;'>View Conversation</a>
        </p>
    </div>
    <div style='background: #f1f1f1; padding: 15px; border-radius: 0 0 10px 10px; text-align: center; font-size: 12px; color: #666;'>
        <p style='margin: 0;'>This email was sent because you have message notifications enabled.</p>
        <p style='margin: 5px 0 0;'>You can manage your notification preferences in your account settings.</p>
    </div>
</body>
</html>";

                await _emailService.SendEmailAsync(recipientEmail, subject, body);
                _logger.LogInformation("Message email notification sent to {Email}", recipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message email notification to user {UserId}", recipientUserId);
                // Don't throw - email failure shouldn't block messaging
            }
        }
    }
}
