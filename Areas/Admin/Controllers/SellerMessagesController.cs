using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,Moderator,Seller")]
    public class SellerMessagesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISellerMessagingService _messagingService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INotificationService _notificationService;

        // Quick Reply Templates
        private static readonly List<QuickReplyTemplate> QuickReplyTemplates = new()
        {
            new QuickReplyTemplate { Id = 1, Title = "Greeting", Message = "Hello! Thank you for reaching out. How can I help you today?" },
            new QuickReplyTemplate { Id = 2, Title = "Order Confirmation", Message = "Your order has been confirmed and is being prepared. You will receive updates on its status." },
            new QuickReplyTemplate { Id = 3, Title = "Shipping Update", Message = "Great news! Your order has been shipped. You can track it using the tracking number provided in your order details." },
            new QuickReplyTemplate { Id = 4, Title = "Thank You", Message = "Thank you for your purchase! We hope you enjoy your product. Please leave a review if you have a moment." },
            new QuickReplyTemplate { Id = 5, Title = "Product Inquiry", Message = "Thank you for your interest in our product. I'd be happy to answer any questions you have." },
            new QuickReplyTemplate { Id = 6, Title = "Return Policy", Message = "Our return policy allows returns within 7 days of delivery. Please ensure the product is in its original condition." },
            new QuickReplyTemplate { Id = 7, Title = "Custom Order", Message = "We can accommodate custom orders. Please provide your specific requirements and I'll get back to you with a quote." },
            new QuickReplyTemplate { Id = 8, Title = "Out of Stock", Message = "Unfortunately, this item is currently out of stock. We expect it to be available again soon. Would you like me to notify you when it's back?" },
        };

        private readonly IStringLocalizer<SharedResources> _localizer;

        public SellerMessagesController(
            UserManager<ApplicationUser> userManager,
            ISellerMessagingService messagingService,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            INotificationService notificationService,
            IStringLocalizer<SharedResources> localizer)
        {
            _userManager = userManager;
            _messagingService = messagingService;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _notificationService = notificationService;
            _localizer = localizer;
        }

        private async Task<int?> GetCurrentSellerIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);

            // If user has Seller role but no Seller entity, create one automatically
            if (seller == null && await _userManager.IsInRoleAsync(user, "Seller"))
            {
                seller = new Models.Seller
                {
                    UserId = user.Id,
                    ShopName = user.FullName ?? user.Email ?? "My Shop",
                    ShopSlug = (user.FullName ?? user.Email ?? "shop").ToLower().Replace(" ", "-").Replace("@", "-").Replace(".", "-"),
                    ShopDescription = "Welcome to my shop!",
                    BusinessEmail = user.Email,
                    BusinessPhone = user.PhoneNumber,
                    BusinessCategory = BusinessCategory.Other,
                    Status = SellerStatus.Approved,
                    IsVerified = true,
                    CommissionRate = 5m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ApprovedAt = DateTime.UtcNow
                };

                _context.Sellers.Add(seller);
                await _context.SaveChangesAsync();
            }

            return seller?.Id;
        }

        private bool IsAdminOrModerator()
        {
            return User.IsInRole("SuperAdmin") || User.IsInRole("Admin") || User.IsInRole("Moderator");
        }

        public async Task<IActionResult> Index()
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null && !IsAdminOrModerator())
            {
                TempData["error"] = _localizer["SellerRegistrationRequired"].Value;
                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }

            var user = await _userManager.GetUserAsync(User);

            List<SellerConversation> conversations;
            if (IsAdminOrModerator())
            {
                // Admin can see all conversations
                conversations = await _context.SellerConversations
                    .Include(c => c.Seller).ThenInclude(s => s!.User)
                    .Include(c => c.Buyer)
                    .Include(c => c.Order)
                    .Include(c => c.Product)
                    .Include(c => c.ClosedByUser)
                    .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                    .Where(c => !c.IsArchived)
                    .OrderByDescending(c => c.LastMessageAt)
                    .ToListAsync();
            }
            else
            {
                conversations = await _messagingService.GetConversationsForSellerAsync(sellerId!.Value);
            }

            ViewBag.UnreadCount = sellerId.HasValue
                ? await _messagingService.GetUnreadCountForSellerAsync(sellerId.Value)
                : 0;
            ViewBag.IsAdminOrModerator = IsAdminOrModerator();
            ViewBag.CurrentSellerId = sellerId;
            ViewBag.CurrentUserId = user?.Id;

            return View(conversations);
        }

        public async Task<IActionResult> Conversation(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            var conversation = await _messagingService.GetConversationAsync(id);

            if (conversation == null)
            {
                return NotFound();
            }

            // Check access
            if (!IsAdminOrModerator() && conversation.SellerId != sellerId)
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);

            // Mark messages as read for seller
            await _messagingService.MarkMessagesAsReadAsync(id, true);

            var messages = await _messagingService.GetMessagesAsync(id);

            // Get seller's products for sharing
            dynamic sellerProducts;
            if (sellerId.HasValue)
            {
                sellerProducts = await _context.Products
                    .Where(p => p.SellerId == sellerId.Value && p.Status == ProductStatus.Active)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(20)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Price,
                        p.DiscountPrice,
                        ImageUrl = p.ImageUrl ?? "/images/products/noimage.jpg"
                    })
                    .ToListAsync();
            }
            else
            {
                sellerProducts = new List<object>();
            }

            ViewBag.Conversation = conversation;
            ViewBag.CurrentUserId = user?.Id;
            ViewBag.IsSeller = true;
            ViewBag.QuickReplies = QuickReplyTemplates;
            ViewBag.SellerProducts = sellerProducts;

            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int conversationId, string message, IFormFile? attachment = null,
            int? sharedProductId = null, string messageType = "text")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(conversationId);
            if (conversation == null)
            {
                return NotFound();
            }

            var sellerId = await GetCurrentSellerIdAsync();
            if (!IsAdminOrModerator() && conversation.SellerId != sellerId)
            {
                return Forbid();
            }

            // If conversation is closed by seller, reopen it
            if (conversation.IsClosedBySeller)
            {
                await _messagingService.ReopenConversationAsync(conversationId, true);
            }

            if (string.IsNullOrWhiteSpace(message) && attachment == null && sharedProductId == null)
            {
                TempData["error"] = _localizer["MessageCannotBeEmpty"].Value;
                return RedirectToAction(nameof(Conversation), new { id = conversationId });
            }

            string? attachmentUrl = null;
            string? attachmentName = null;
            string? attachmentType = null;

            // Handle file upload
            if (attachment != null && attachment.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx" };
                var extension = Path.GetExtension(attachment.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["error"] = _localizer["InvalidFileType"].Value;
                    return RedirectToAction(nameof(Conversation), new { id = conversationId });
                }

                if (attachment.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    TempData["error"] = _localizer["FileSizeTooLarge"].Value;
                    return RedirectToAction(nameof(Conversation), new { id = conversationId });
                }

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "messages");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(stream);
                }

                attachmentUrl = $"/uploads/messages/{uniqueFileName}";
                attachmentName = attachment.FileName;
                attachmentType = extension.TrimStart('.').ToLower() switch
                {
                    "jpg" or "jpeg" or "png" or "gif" or "webp" => "image",
                    "pdf" => "pdf",
                    "doc" or "docx" => "document",
                    _ => "file"
                };
            }

            // Auto-generate message for shared content if empty
            if (string.IsNullOrWhiteSpace(message))
            {
                if (sharedProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync(sharedProductId.Value);
                    message = $"Check out this product: {product?.Name ?? "Product"}";
                    messageType = "shared_product";
                }
                else if (attachment != null)
                {
                    message = $"Sent an attachment: {attachmentName}";
                }
            }

            await _messagingService.SendMessageAsync(
                conversationId,
                user.Id,
                true,
                message?.Trim() ?? "",
                attachmentUrl,
                attachmentName,
                attachmentType,
                null, // sharedOrderId - sellers don't share orders
                sharedProductId,
                messageType
            );

            TempData["success"] = _localizer["MessageSentSuccessfully"].Value;
            return RedirectToAction(nameof(Conversation), new { id = conversationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShareProduct(int conversationId, int productId, string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(conversationId);
            if (conversation == null)
            {
                return NotFound();
            }

            var sellerId = await GetCurrentSellerIdAsync();
            if (!IsAdminOrModerator() && conversation.SellerId != sellerId)
            {
                return Forbid();
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["error"] = _localizer["ProductNotFound"].Value;
                return RedirectToAction(nameof(Conversation), new { id = conversationId });
            }

            var message = string.IsNullOrWhiteSpace(note)
                ? $"Check out this product: {product.Name}"
                : note;

            await _messagingService.SendMessageAsync(
                conversationId,
                user.Id,
                true,
                message,
                null, null, null,
                null,
                productId,
                "shared_product"
            );

            TempData["success"] = _localizer["ProductSharedSuccessfully"].Value;
            return RedirectToAction(nameof(Conversation), new { id = conversationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRating(int conversationId, int? orderId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(conversationId);
            if (conversation == null)
            {
                return NotFound();
            }

            var sellerId = await GetCurrentSellerIdAsync();
            if (!IsAdminOrModerator() && conversation.SellerId != sellerId)
            {
                return Forbid();
            }

            // Use provided orderId or conversation's orderId
            var targetOrderId = orderId ?? conversation.OrderId;

            var message = targetOrderId.HasValue
                ? $"We hope you're enjoying your purchase! Would you mind taking a moment to leave a rating for your order? Your feedback helps us improve."
                : $"We hope you're enjoying your shopping experience! Would you mind taking a moment to leave a rating? Your feedback helps us improve.";

            await _messagingService.SendMessageAsync(
                conversationId,
                user.Id,
                true,
                message,
                null, null, null,
                targetOrderId,
                null,
                "rating_request"
            );

            // Also send a notification to the buyer
            await _notificationService.CreateNotificationAsync(new UserNotification
            {
                UserId = conversation.BuyerId,
                Type = "general",
                Icon = "fa-star",
                IconColor = "text-warning",
                Title = "Rate Your Purchase",
                Message = $"{conversation.Seller?.ShopName ?? "A seller"} has requested a rating for your recent purchase.",
                Link = targetOrderId.HasValue
                    ? $"/Customer/Home/OrderDetails/{targetOrderId}"
                    : $"/Customer/Messages/Conversation/{conversationId}",
                OrderId = targetOrderId,
                ConversationId = conversationId,
                CreatedAt = DateTime.UtcNow
            });

            TempData["success"] = _localizer["RatingRequestSentSuccessfully"].Value;
            return RedirectToAction(nameof(Conversation), new { id = conversationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenConversation(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            var conversation = await _messagingService.GetConversationAsync(id);

            if (conversation == null)
            {
                return NotFound();
            }

            if (!IsAdminOrModerator() && conversation.SellerId != sellerId)
            {
                return Forbid();
            }

            await _messagingService.ReopenConversationAsync(id, true);
            TempData["success"] = _localizer["ConversationReopened"].Value;

            return RedirectToAction(nameof(Conversation), new { id });
        }

        [HttpGet]
        public IActionResult GetQuickReplies()
        {
            return Json(QuickReplyTemplates);
        }

        [HttpGet]
        public async Task<IActionResult> GetBuyerOrdersAndProducts(string buyerId)
        {
            if (string.IsNullOrEmpty(buyerId))
            {
                return Json(new { orders = new List<object>(), products = new List<object>() });
            }

            var sellerId = await GetCurrentSellerIdAsync();

            // Get orders for the buyer (optionally filter by seller if not admin)
            var ordersQuery = _context.Orders
                .Where(o => o.UserId == buyerId);

            if (!IsAdminOrModerator() && sellerId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderItems.Any(oi => oi.Product.SellerId == sellerId.Value));
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .Select(o => new
                {
                    o.Id,
                    Display = $"Order #{o.Id} - {o.OrderDate:dd MMM yyyy} - {o.TotalAmount:N0}৳"
                })
                .ToListAsync();

            // Get products from buyer's orders
            var productsQuery = _context.OrderItems
                .Where(oi => oi.Order.UserId == buyerId);

            if (!IsAdminOrModerator() && sellerId.HasValue)
            {
                productsQuery = productsQuery.Where(oi => oi.Product.SellerId == sellerId.Value);
            }

            var products = await productsQuery
                .Select(oi => new
                {
                    oi.ProductId,
                    oi.Product.Name
                })
                .Distinct()
                .Take(20)
                .ToListAsync();

            var productList = products
                .GroupBy(p => p.ProductId)
                .Select(g => new
                {
                    Id = g.Key,
                    Display = g.First().Name
                })
                .ToList();

            return Json(new { orders, products = productList });
        }

        public async Task<IActionResult> NewMessage()
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null && !IsAdminOrModerator())
            {
                TempData["error"] = _localizer["SellerRegistrationRequired"].Value;
                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }

            List<EligibleBuyer> eligibleBuyers;
            if (IsAdminOrModerator())
            {
                // Admin can message anyone
                eligibleBuyers = await _context.Users
                    .Select(u => new EligibleBuyer
                    {
                        UserId = u.Id,
                        DisplayName = u.FullName ?? u.Email ?? "Unknown",
                        Email = u.Email,
                        EligibilityReason = "Admin access",
                        InteractionDate = DateTime.UtcNow
                    })
                    .OrderBy(u => u.DisplayName)
                    .Take(100)
                    .ToListAsync();
            }
            else
            {
                eligibleBuyers = await _messagingService.GetEligibleBuyersForSellerAsync(sellerId!.Value);
            }

            ViewBag.SellerId = sellerId;
            return View(eligibleBuyers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartConversation(string buyerId, int? orderId, int? productId, string subject, string message)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null && !IsAdminOrModerator())
            {
                TempData["error"] = _localizer["SellerRegistrationRequired"].Value;
                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }

            // For admin, get or create seller ID
            if (IsAdminOrModerator() && sellerId == null)
            {
                // Use the first seller or create a system seller
                var firstSeller = await _context.Sellers.FirstOrDefaultAsync();
                if (firstSeller != null)
                {
                    sellerId = firstSeller.Id;
                }
                else
                {
                    TempData["error"] = _localizer["NoSellersExist"].Value;
                    return RedirectToAction(nameof(NewMessage));
                }
            }

            // Check eligibility for non-admin
            if (!IsAdminOrModerator())
            {
                var canMessage = await _messagingService.CanSellerMessageBuyerAsync(sellerId!.Value, buyerId);
                if (!canMessage)
                {
                    TempData["error"] = _localizer["CannotMessageBuyer"].Value;
                    return RedirectToAction(nameof(NewMessage));
                }
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["error"] = _localizer["MessageCannotBeEmpty"].Value;
                return RedirectToAction(nameof(NewMessage));
            }

            var conversation = await _messagingService.GetOrCreateConversationAsync(
                sellerId!.Value, buyerId, orderId, productId, subject);

            if (conversation == null)
            {
                TempData["error"] = _localizer["FailedToCreateConversation"].Value;
                return RedirectToAction(nameof(NewMessage));
            }

            await _messagingService.SendMessageAsync(conversation.Id, user.Id, true, message.Trim());
            TempData["success"] = _localizer["MessageSentSuccessfully"].Value;

            return RedirectToAction(nameof(Conversation), new { id = conversation.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseConversation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var sellerId = await GetCurrentSellerIdAsync();
            var conversation = await _messagingService.GetConversationAsync(id);

            if (conversation == null)
            {
                return NotFound();
            }

            if (!IsAdminOrModerator() && conversation.SellerId != sellerId)
            {
                return Forbid();
            }

            // Determine who is closing: Staff (Admin/Moderator), Seller, or Buyer
            var isStaff = IsAdminOrModerator() && (sellerId == null || sellerId != conversation.SellerId);
            var isSeller = sellerId.HasValue && sellerId == conversation.SellerId;

            await _messagingService.CloseConversationAsync(id, isSeller, user.Id, isStaff);
            TempData["success"] = _localizer["ConversationClosed"].Value;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConversation(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            var conversation = await _messagingService.GetConversationAsync(id);

            if (conversation == null)
            {
                return NotFound();
            }

            if (!IsAdminOrModerator() && conversation.SellerId != sellerId)
            {
                return Forbid();
            }

            await _messagingService.ArchiveConversationAsync(id);
            TempData["success"] = _localizer["ConversationArchived"].Value;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return Json(new { count = 0 });

            var count = await _messagingService.GetUnreadCountForSellerAsync(sellerId.Value);
            return Json(new { count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(int messageId, string newMessage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var message = await _context.SellerMessages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return Json(new { success = false, error = "Message not found." });
            }

            // Verify ownership - only sender can edit their own message
            if (message.SenderId != user.Id)
            {
                return Json(new { success = false, error = "You can only edit your own messages." });
            }

            // Can only edit within 15 minutes of sending
            if ((DateTime.UtcNow - message.CreatedAt).TotalMinutes > 15)
            {
                return Json(new { success = false, error = "Messages can only be edited within 15 minutes of sending." });
            }

            // Cannot edit if already read by recipient
            if (message.IsRead)
            {
                return Json(new { success = false, error = "Cannot edit a message that has already been read." });
            }

            if (string.IsNullOrWhiteSpace(newMessage))
            {
                return Json(new { success = false, error = "Message cannot be empty." });
            }

            message.Message = newMessage.Trim();
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Message edited successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var message = await _context.SellerMessages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return Json(new { success = false, error = "Message not found." });
            }

            // Verify ownership - only sender can delete their own message
            if (message.SenderId != user.Id)
            {
                return Json(new { success = false, error = "You can only delete your own messages." });
            }

            // Can only delete within 15 minutes of sending
            if ((DateTime.UtcNow - message.CreatedAt).TotalMinutes > 15)
            {
                return Json(new { success = false, error = "Messages can only be deleted within 15 minutes of sending." });
            }

            // Soft delete - mark as deleted
            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;
            message.Message = "This message was deleted";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Message deleted successfully." });
        }
    }

    public class QuickReplyTemplate
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
