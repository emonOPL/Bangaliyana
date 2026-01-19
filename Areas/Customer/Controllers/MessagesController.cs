using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISellerMessagingService _messagingService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INotificationService _notificationService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public MessagesController(
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

        public async Task<IActionResult> Index(string? filter = "all", string? search = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var query = _context.SellerConversations
                .Include(c => c.Seller).ThenInclude(s => s!.User)
                .Include(c => c.Buyer)
                .Include(c => c.Order)
                .Include(c => c.Product)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => c.BuyerId == user.Id);

            // Apply filter
            switch (filter?.ToLower())
            {
                case "unread":
                    query = query.Where(c => c.UnreadCountBuyer > 0 && !c.IsArchived);
                    break;
                case "archived":
                    query = query.Where(c => c.IsArchived);
                    break;
                default: // "all"
                    query = query.Where(c => !c.IsArchived);
                    break;
            }

            // Apply search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    (c.Subject != null && c.Subject.ToLower().Contains(search)) ||
                    (c.Seller != null && c.Seller.ShopName != null && c.Seller.ShopName.ToLower().Contains(search)) ||
                    (c.Product != null && c.Product.Name.ToLower().Contains(search)) ||
                    c.Messages.Any(m => m.Message.ToLower().Contains(search))
                );
            }

            var conversations = await query
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            ViewBag.UnreadCount = await _messagingService.GetUnreadCountForBuyerAsync(user.Id);
            ViewBag.CurrentFilter = filter ?? "all";
            ViewBag.SearchQuery = search;
            ViewBag.TotalCount = await _context.SellerConversations.CountAsync(c => c.BuyerId == user.Id && !c.IsArchived);
            ViewBag.ArchivedCount = await _context.SellerConversations.CountAsync(c => c.BuyerId == user.Id && c.IsArchived);

            return View(conversations);
        }

        public async Task<IActionResult> Conversation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(id);
            if (conversation == null || conversation.BuyerId != user.Id)
            {
                return NotFound();
            }

            // Mark messages as read
            await _messagingService.MarkMessagesAsReadAsync(id, false);

            var messages = await _messagingService.GetMessagesAsync(id);

            // Get user's orders for Share Order modal
            var userOrders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status
                })
                .ToListAsync();

            ViewBag.Conversation = conversation;
            ViewBag.CurrentUserId = user.Id;
            ViewBag.UserOrders = userOrders;
            ViewBag.UserAvatarUrl = user.AvatarUrl;
            ViewBag.UserFullName = user.FullName;

            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int conversationId, string message, IFormFile? attachment = null,
            int? sharedOrderId = null, int? sharedProductId = null, string messageType = "text")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(conversationId);
            if (conversation == null || conversation.BuyerId != user.Id)
            {
                return NotFound();
            }

            // If conversation is closed, reopen it
            if (conversation.IsClosedByBuyer)
            {
                await _messagingService.ReopenConversationAsync(conversationId, false);
            }

            // Validate message or shared content
            if (string.IsNullOrWhiteSpace(message) && attachment == null && sharedOrderId == null && sharedProductId == null)
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
                    TempData["error"] = _localizer["InvalidFileTypeOnlyImagesAndDocuments"].Value;
                    return RedirectToAction(nameof(Conversation), new { id = conversationId });
                }

                if (attachment.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    TempData["error"] = _localizer["FileSizeMustBeLessThan10MB"].Value;
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
                if (sharedOrderId.HasValue)
                {
                    var order = await _context.Orders.FindAsync(sharedOrderId.Value);
                    message = $"Shared order #{order?.OrderNumber ?? sharedOrderId.Value.ToString()}";
                    messageType = "shared_order";
                }
                else if (sharedProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync(sharedProductId.Value);
                    message = $"Shared product: {product?.Name ?? "Product"}";
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
                false,
                message.Trim(),
                attachmentUrl,
                attachmentName,
                attachmentType,
                sharedOrderId,
                sharedProductId,
                messageType
            );

            TempData["success"] = _localizer["MessageSentSuccessfully"].Value;
            return RedirectToAction(nameof(Conversation), new { id = conversationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShareOrder(int conversationId, int orderId, string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(conversationId);
            if (conversation == null || conversation.BuyerId != user.Id)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.UserId != user.Id)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction(nameof(Conversation), new { id = conversationId });
            }

            var message = string.IsNullOrWhiteSpace(note)
                ? $"Shared order #{order.OrderNumber}"
                : note;

            await _messagingService.SendMessageAsync(
                conversationId,
                user.Id,
                false,
                message,
                null, null, null,
                orderId,
                null,
                "shared_order"
            );

            TempData["success"] = _localizer["OrderSharedSuccessfully"].Value;
            return RedirectToAction(nameof(Conversation), new { id = conversationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShareProduct(int conversationId, int productId, string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(conversationId);
            if (conversation == null || conversation.BuyerId != user.Id)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["error"] = _localizer["ProductNotFound"].Value;
                return RedirectToAction(nameof(Conversation), new { id = conversationId });
            }

            var message = string.IsNullOrWhiteSpace(note)
                ? $"Shared product: {product.Name}"
                : note;

            await _messagingService.SendMessageAsync(
                conversationId,
                user.Id,
                false,
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
        public async Task<IActionResult> EditMessage(int messageId, string newMessage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var message = await _context.SellerMessages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return Json(new { success = false, error = _localizer["MessageNotFound"].Value });
            }

            // Verify ownership - only sender can edit their own message
            if (message.SenderId != user.Id)
            {
                return Json(new { success = false, error = _localizer["YouCanOnlyEditYourOwnMessages"].Value });
            }

            // Can only edit within 15 minutes of sending
            if ((DateTime.UtcNow - message.CreatedAt).TotalMinutes > 15)
            {
                return Json(new { success = false, error = _localizer["MessagesCanOnlyBeEditedWithin15Minutes"].Value });
            }

            // Cannot edit if already read by recipient
            if (message.IsRead)
            {
                return Json(new { success = false, error = _localizer["CannotEditAlreadyReadMessage"].Value });
            }

            if (string.IsNullOrWhiteSpace(newMessage))
            {
                return Json(new { success = false, error = _localizer["MessageCannotBeEmpty"].Value });
            }

            message.Message = newMessage.Trim();
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = _localizer["MessageEditedSuccessfully"].Value });
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
                return Json(new { success = false, error = _localizer["MessageNotFound"].Value });
            }

            // Verify ownership - only sender can delete their own message
            if (message.SenderId != user.Id)
            {
                return Json(new { success = false, error = _localizer["YouCanOnlyDeleteYourOwnMessages"].Value });
            }

            // Can only delete within 15 minutes of sending
            if ((DateTime.UtcNow - message.CreatedAt).TotalMinutes > 15)
            {
                return Json(new { success = false, error = _localizer["MessagesCanOnlyBeDeletedWithin15Minutes"].Value });
            }

            // Soft delete - mark as deleted
            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;
            message.Message = _localizer["ThisMessageWasDeleted"].Value;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = _localizer["MessageDeletedSuccessfully"].Value });
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(int sellerId, string? query = null)
        {
            // Filter products by seller
            var productsQuery = _context.Products
                .Where(p => p.Status == Models.ProductStatus.Active && p.SellerId == sellerId);

            // Apply search filter if query is provided
            if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(query) || p.Description.Contains(query));
            }

            var products = await productsQuery
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.DiscountPrice,
                    ImageUrl = p.ImageUrl != null
                        ? (p.ImageUrl.StartsWith("/") ? p.ImageUrl : "/" + p.ImageUrl)
                        : "/images/products/noimage.jpg"
                })
                .ToListAsync();

            return Json(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EscalateToAdmin(int conversationId, string reason, string description)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(conversationId);
            if (conversation == null || conversation.BuyerId != user.Id)
            {
                return NotFound();
            }

            // Create a support ticket for admin
            var sellerName = conversation.Seller?.ShopName ?? "Unknown";
            var descriptionText = string.IsNullOrWhiteSpace(description)
                ? $"Escalated from seller conversation #{conversationId}; Seller: {sellerName}; Reason: {reason}"
                : $"Escalated from seller conversation #{conversationId}; Seller: {sellerName}; Reason: {reason}; Details: {description}";

            var ticket = new Ticket
            {
                UserId = user.Id,
                Subject = $"Escalation: {reason}",
                Description = descriptionText,
                Status = TicketStatus.Open,
                Priority = TicketPriority.High,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Add a message to the conversation about the escalation
            await _messagingService.SendMessageAsync(
                conversationId,
                user.Id,
                false,
                $"This conversation has been escalated to admin. Reason: {reason}",
                null, null, null,
                null, null,
                "escalation"
            );

            // Notify admin
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = admin.Id,
                    Type = "support",
                    Icon = "fa-exclamation-triangle",
                    IconColor = "text-danger",
                    Title = "Conversation Escalated",
                    Message = $"{user.FullName ?? user.Email} escalated a conversation with {conversation.Seller?.ShopName ?? "a seller"}",
                    Link = $"/Admin/Support/Details/{ticket.Id}",
                    TicketId = ticket.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            TempData["success"] = _localizer["IssueEscalatedToAdminSuccessfully"].Value;
            return RedirectToAction(nameof(Conversation), new { id = conversationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenConversation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(id);
            if (conversation == null || conversation.BuyerId != user.Id)
            {
                return NotFound();
            }

            await _messagingService.ReopenConversationAsync(id, false);
            TempData["success"] = _localizer["ConversationReopened"].Value;

            return RedirectToAction(nameof(Conversation), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseConversation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(id);
            if (conversation == null || conversation.BuyerId != user.Id)
            {
                return NotFound();
            }

            await _messagingService.CloseConversationAsync(id, false);
            TempData["success"] = _localizer["ConversationClosed"].Value;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConversation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(id);
            if (conversation == null || conversation.BuyerId != user.Id)
            {
                return NotFound();
            }

            await _messagingService.ArchiveConversationAsync(id);
            TempData["success"] = _localizer["ConversationArchived"].Value;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveConversation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _context.SellerConversations.FindAsync(id);
            if (conversation == null || conversation.BuyerId != user.Id)
            {
                return NotFound();
            }

            conversation.IsArchived = false;
            await _context.SaveChangesAsync();
            TempData["success"] = _localizer["ConversationRestored"].Value;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { count = 0 });

            var count = await _messagingService.GetUnreadCountForBuyerAsync(user.Id);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> StartConversation(int sellerId, int? productId = null, int? orderId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Get the seller
            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller == null)
            {
                TempData["error"] = _localizer["SellerNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Block starting new conversations with suspended sellers
            if (seller.Status == SellerStatus.Suspended)
            {
                TempData["error"] = _localizer["SellerUnavailableCannotReceiveMessages"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Check if there's an existing conversation with this seller for this product/order
            var existingConversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c =>
                    c.SellerId == sellerId &&
                    c.BuyerId == user.Id &&
                    (productId == null || c.ProductId == productId) &&
                    (orderId == null || c.OrderId == orderId) &&
                    !c.IsClosedByBuyer);

            if (existingConversation != null)
            {
                return RedirectToAction(nameof(Conversation), new { id = existingConversation.Id });
            }

            // Get product/order details for context
            Products? product = null;
            if (productId.HasValue)
            {
                product = await _context.Products.FindAsync(productId.Value);
            }

            Order? order = null;
            if (orderId.HasValue)
            {
                order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId.Value && o.UserId == user.Id);
            }

            ViewBag.Seller = seller;
            ViewBag.Product = product;
            ViewBag.Order = order;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartConversation(int sellerId, int? productId, int? orderId, string subject, string message)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                TempData["error"] = _localizer["SubjectAndMessageRequired"].Value;
                return RedirectToAction(nameof(StartConversation), new { sellerId, productId, orderId });
            }

            // Check eligibility - user must have had some interaction with the seller
            // (ordered from them, asked a question about their product, or has a support ticket)
            var hasInteraction = await CheckBuyerEligibilityAsync(user.Id, sellerId, productId, orderId);

            // For now, allow contact from product page as that's the main entry point
            // The seller can still choose not to respond

            // Create conversation
            try
            {
                var conversationId = await _messagingService.StartConversationAsync(
                    sellerId,
                    user.Id,
                    productId,
                    orderId,
                    subject.Trim(),
                    message.Trim()
                );

                TempData["success"] = _localizer["MessageSentToSellerSuccessfully"].Value;
                return RedirectToAction(nameof(Conversation), new { id = conversationId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<bool> CheckBuyerEligibilityAsync(string buyerId, int sellerId, int? productId, int? orderId)
        {
            // Check if user has ordered from this seller
            var hasOrder = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AnyAsync(o => o.UserId == buyerId &&
                              o.OrderItems.Any(oi => oi.Product.SellerId == sellerId));

            if (hasOrder) return true;

            // Check if user asked a question about seller's product
            var hasQuestion = await _context.ProductQuestions
                .Include(q => q.Product)
                .AnyAsync(q => q.UserId == buyerId &&
                              q.Product.SellerId == sellerId);

            if (hasQuestion) return true;

            // If contacting about a specific product, allow it (product inquiry)
            if (productId.HasValue)
            {
                var product = await _context.Products.FindAsync(productId.Value);
                if (product != null && product.SellerId == sellerId)
                    return true;
            }

            return false;
        }
    }
}
