using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "SuperAdmin,Seller,Admin")]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public MessagesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _localizer = localizer;
        }

        private async Task<int?> GetCurrentSellerIdAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            return seller?.Id;
        }

        // GET: /Seller/Messages
        public async Task<IActionResult> Index()
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var conversations = await _context.SellerConversations
                .Include(c => c.Buyer)
                .Include(c => c.Product)
                .Include(c => c.Messages)
                .Where(c => c.SellerId == sellerId && !c.IsArchived)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            return View(conversations);
        }

        // GET: /Seller/Messages/Conversation/5
        public async Task<IActionResult> Conversation(int? id)
        {
            if (id == null) return NotFound();

            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var conversation = await _context.SellerConversations
                .Include(c => c.Buyer)
                .Include(c => c.Seller)
                .Include(c => c.Product)
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == id && c.SellerId == sellerId);

            if (conversation == null)
            {
                TempData["Error"] = _localizer["ConversationNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Mark messages as read
            var unreadMessages = conversation.Messages
                .Where(m => !m.IsRead && m.SenderId != _userManager.GetUserId(User));

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            conversation.UnreadCountSeller = 0;
            await _context.SaveChangesAsync();

            return View(conversation);
        }

        // POST: /Seller/Messages/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int conversationId, string? content, IFormFile? attachment = null)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return Json(new { success = false, message = "Not authorized" });

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false, message = "Not logged in" });

            var conversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.SellerId == sellerId);

            if (conversation == null)
            {
                return Json(new { success = false, message = "Conversation not found" });
            }

            // If conversation was closed by seller, reopen it
            if (conversation.IsClosedBySeller)
            {
                conversation.IsClosedBySeller = false;
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
                    return Json(new { success = false, message = "Invalid file type. Only images and documents are allowed." });
                }

                if (attachment.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    return Json(new { success = false, message = "File size must be less than 10MB." });
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

            // Auto-generate message if only attachment
            var messageContent = content?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(messageContent) && attachment != null)
            {
                messageContent = $"Sent an attachment: {attachmentName}";
            }

            if (string.IsNullOrWhiteSpace(messageContent) && attachment == null)
            {
                return Json(new { success = false, message = "Message cannot be empty" });
            }

            var message = new SellerMessage
            {
                ConversationId = conversationId,
                SenderId = userId,
                Message = messageContent,
                IsSentBySeller = true,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                AttachmentUrl = attachmentUrl,
                AttachmentName = attachmentName,
                AttachmentType = attachmentType
            };

            _context.SellerMessages.Add(message);

            conversation.LastMessageAt = DateTime.UtcNow;
            conversation.UnreadCountBuyer++;

            await _context.SaveChangesAsync();

            return Json(new { success = true, messageId = message.Id });
        }

        // POST: /Seller/Messages/EditMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(int messageId, string newMessage)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false, error = "Not logged in" });

            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return Json(new { success = false, error = "Not authorized" });

            var message = await _context.SellerMessages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return Json(new { success = false, error = "Message not found." });
            }

            // Verify ownership - only sender can edit their own message
            if (message.SenderId != userId)
            {
                return Json(new { success = false, error = "You can only edit your own messages." });
            }

            // Verify this is seller's conversation
            if (message.Conversation?.SellerId != sellerId)
            {
                return Json(new { success = false, error = "Unauthorized." });
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

        // POST: /Seller/Messages/DeleteMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false, error = "Not logged in" });

            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return Json(new { success = false, error = "Not authorized" });

            var message = await _context.SellerMessages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return Json(new { success = false, error = "Message not found." });
            }

            // Verify ownership - only sender can delete their own message
            if (message.SenderId != userId)
            {
                return Json(new { success = false, error = "You can only delete your own messages." });
            }

            // Verify this is seller's conversation
            if (message.Conversation?.SellerId != sellerId)
            {
                return Json(new { success = false, error = "Unauthorized." });
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

        // POST: /Seller/Messages/CloseConversation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseConversation(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var conversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c => c.Id == id && c.SellerId == sellerId);

            if (conversation != null)
            {
                conversation.IsClosedBySeller = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = _localizer["ConversationClosed"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Seller/Messages/ReopenConversation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenConversation(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var conversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c => c.Id == id && c.SellerId == sellerId);

            if (conversation != null)
            {
                conversation.IsClosedBySeller = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = _localizer["ConversationReopened"].Value;
            }

            return RedirectToAction(nameof(Conversation), new { id });
        }

        // POST: /Seller/Messages/Archive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var conversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c => c.Id == id && c.SellerId == sellerId);

            if (conversation != null)
            {
                conversation.IsArchived = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = _localizer["ConversationArchived"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Seller/Messages/Archived
        public async Task<IActionResult> Archived()
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var conversations = await _context.SellerConversations
                .Include(c => c.Buyer)
                .Include(c => c.Product)
                .Include(c => c.Messages)
                .Where(c => c.SellerId == sellerId && c.IsArchived)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            return View("Index", conversations);
        }

        // POST: /Seller/Messages/Unarchive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var conversation = await _context.SellerConversations
                .FirstOrDefaultAsync(c => c.Id == id && c.SellerId == sellerId);

            if (conversation != null)
            {
                conversation.IsArchived = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = _localizer["ConversationRestored"].Value;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
