using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Moderator.Controllers
{
    [Area("Moderator")]
    [Authorize(Roles = "SuperAdmin,Admin,Moderator")]
    public class MessageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISellerMessagingService _messagingService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INotificationService _notificationService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        // Quick Reply Templates
        private static readonly List<QuickReplyTemplate> QuickReplyTemplates = new()
        {
            new QuickReplyTemplate { Id = 1, Title = "Greeting", Message = "Hello! Thank you for reaching out. How can I help you today?" },
            new QuickReplyTemplate { Id = 2, Title = "Order Inquiry", Message = "I'd be happy to help with your order. Could you please provide your order number?" },
            new QuickReplyTemplate { Id = 3, Title = "Issue Resolved", Message = "Great news! Your issue has been resolved. Please let me know if you need any further assistance." },
            new QuickReplyTemplate { Id = 4, Title = "Escalation", Message = "I'm escalating this matter to our senior team for further review. You will receive an update shortly." },
            new QuickReplyTemplate { Id = 5, Title = "Refund Info", Message = "Your refund request is being processed. Refunds typically take 3-5 business days to reflect in your account." },
            new QuickReplyTemplate { Id = 6, Title = "Return Policy", Message = "Our return policy allows returns within 7 days of delivery. Please ensure the product is in its original condition." },
            new QuickReplyTemplate { Id = 7, Title = "Thank You", Message = "Thank you for contacting us. Is there anything else I can help you with?" },
            new QuickReplyTemplate { Id = 8, Title = "Follow Up", Message = "I'm following up on your previous inquiry. Has your issue been resolved?" },
        };

        public MessageController(
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

        public async Task<IActionResult> Index()
        {
            // Moderators can see all conversations
            var conversations = await _context.SellerConversations
                .Include(c => c.Seller).ThenInclude(s => s!.User)
                .Include(c => c.Buyer)
                .Include(c => c.Order)
                .Include(c => c.Product)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(c => !c.IsArchived)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            ViewBag.UnreadCount = await _context.SellerMessages
                .Where(m => !m.IsRead && !m.IsSentBySeller) // Only count unread messages from buyers
                .Select(m => m.ConversationId)
                .Distinct()
                .CountAsync();

            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Message";

            return View("~/Areas/Admin/Views/SellerMessages/Index.cshtml", conversations);
        }

        public async Task<IActionResult> Conversation(int id)
        {
            var conversation = await _messagingService.GetConversationAsync(id);

            if (conversation == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            // Mark messages as read
            await _messagingService.MarkMessagesAsReadAsync(id, true);

            var messages = await _messagingService.GetMessagesAsync(id);

            // Get all active products for sharing
            var products = await _context.Products
                .Where(p => p.Status == ProductStatus.Active)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.DiscountPrice,
                    ImageUrl = p.ImageUrl ?? "/images/products/noimage.jpg"
                })
                .ToListAsync();

            ViewBag.Conversation = conversation;
            ViewBag.CurrentUserId = user?.Id;
            ViewBag.IsSeller = true;
            ViewBag.IsModerator = true;
            ViewBag.QuickReplies = QuickReplyTemplates;
            ViewBag.SellerProducts = products;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Message";

            return View("~/Areas/Admin/Views/SellerMessages/Conversation.cshtml", messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int conversationId, string message, IFormFile? attachment = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversation = await _messagingService.GetConversationAsync(conversationId);
            if (conversation == null)
            {
                return NotFound();
            }

            // If conversation is closed, reopen it
            if (conversation.IsClosedBySeller)
            {
                await _messagingService.ReopenConversationAsync(conversationId, true);
            }

            if (string.IsNullOrWhiteSpace(message) && attachment == null)
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

                if (attachment.Length > 10 * 1024 * 1024)
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

                if (string.IsNullOrWhiteSpace(message))
                {
                    message = $"Sent an attachment: {attachmentName}";
                }
            }

            await _messagingService.SendMessageAsync(
                conversationId,
                user.Id,
                true, // isSeller (acting as moderator)
                message?.Trim() ?? "",
                attachmentUrl,
                attachmentName,
                attachmentType
            );

            TempData["success"] = _localizer["MessageSentSuccessfully"].Value;
            return RedirectToAction(nameof(Conversation), new { id = conversationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseConversation(int id)
        {
            var conversation = await _messagingService.GetConversationAsync(id);

            if (conversation == null)
            {
                return NotFound();
            }

            await _messagingService.CloseConversationAsync(id, true);
            TempData["success"] = _localizer["ConversationClosed"].Value;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenConversation(int id)
        {
            var conversation = await _messagingService.GetConversationAsync(id);

            if (conversation == null)
            {
                return NotFound();
            }

            await _messagingService.ReopenConversationAsync(id, true);
            TempData["success"] = _localizer["ConversationReopened"].Value;

            return RedirectToAction(nameof(Conversation), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveConversation(int id)
        {
            var conversation = await _messagingService.GetConversationAsync(id);

            if (conversation == null)
            {
                return NotFound();
            }

            await _messagingService.ArchiveConversationAsync(id);
            TempData["success"] = _localizer["ConversationArchived"].Value;

            return RedirectToAction(nameof(Index));
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

            // Get orders for the buyer
            var orders = await _context.Orders
                .Where(o => o.UserId == buyerId)
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .Select(o => new
                {
                    o.Id,
                    Display = $"Order #{o.Id} - {o.OrderDate:dd MMM yyyy} - {o.TotalAmount:N0}৳"
                })
                .ToListAsync();

            // Get products from buyer's orders
            var products = await _context.OrderItems
                .Where(oi => oi.Order.UserId == buyerId)
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
            // Moderators can message anyone - fetch first, then process client-side
            var users = await _context.Users
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                .Take(100)
                .ToListAsync();

            var eligibleBuyers = users
                .Select(u => new Services.EligibleBuyer
                {
                    UserId = u.Id,
                    DisplayName = !string.IsNullOrEmpty(u.FirstName) ? $"{u.FirstName} {u.LastName}".Trim() : u.Email ?? "Unknown",
                    Email = u.Email,
                    EligibilityReason = "Moderator access",
                    InteractionDate = DateTime.UtcNow
                })
                .OrderBy(u => u.DisplayName)
                .ToList();

            ViewBag.SellerId = null;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Message";
            return View("~/Areas/Admin/Views/SellerMessages/NewMessage.cshtml", eligibleBuyers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartConversation(string buyerId, int? orderId, int? productId, string subject, string message)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // For moderator, use the first seller
            var firstSeller = await _context.Sellers.FirstOrDefaultAsync();
            if (firstSeller == null)
            {
                TempData["error"] = _localizer["NoSellersExist"].Value;
                return RedirectToAction(nameof(NewMessage));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["error"] = _localizer["MessageCannotBeEmpty"].Value;
                return RedirectToAction(nameof(NewMessage));
            }

            var conversation = await _messagingService.GetOrCreateConversationAsync(
                firstSeller.Id, buyerId, orderId, productId, subject);

            if (conversation == null)
            {
                TempData["error"] = _localizer["FailedToCreateConversation"].Value;
                return RedirectToAction(nameof(NewMessage));
            }

            await _messagingService.SendMessageAsync(conversation.Id, user.Id, true, message.Trim());
            TempData["success"] = _localizer["MessageSentSuccessfully"].Value;

            return RedirectToAction(nameof(Conversation), new { id = conversation.Id });
        }
    }

    public class QuickReplyTemplate
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
