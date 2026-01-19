using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using System.Security.Claims;

namespace Bangaliyana.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HelpCenterController : Controller
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ISupportChatService _chatService;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HelpCenterController> _logger;
        private readonly IAIChatService _aiChatService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public HelpCenterController(
            ISiteSettingsService siteSettingsService,
            ISupportChatService chatService,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<HelpCenterController> logger,
            IAIChatService aiChatService,
            IStringLocalizer<SharedResources> localizer)
        {
            _siteSettingsService = siteSettingsService;
            _chatService = chatService;
            _emailService = emailService;
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _aiChatService = aiChatService;
            _localizer = localizer;
        }

        /// <summary>
        /// Help Center main page with all options
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var faqs = await _siteSettingsService.GetActiveFAQsAsync();
            var categories = await _siteSettingsService.GetFAQCategoriesAsync();
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();

            ViewBag.FAQs = faqs.Take(6);
            ViewBag.Categories = categories;
            ViewBag.SiteSettings = siteSettings;

            return View();
        }

        /// <summary>
        /// FAQ page with search and category filtering
        /// </summary>
        public async Task<IActionResult> FAQ(string? search, string? category)
        {
            List<FAQ> faqs;

            if (!string.IsNullOrWhiteSpace(search))
            {
                faqs = await _siteSettingsService.SearchFAQsAsync(search);
                ViewBag.SearchQuery = search;
            }
            else if (!string.IsNullOrWhiteSpace(category))
            {
                faqs = await _siteSettingsService.GetFAQsByCategoryAsync(category);
                ViewBag.SelectedCategory = category;
            }
            else
            {
                faqs = await _siteSettingsService.GetActiveFAQsAsync();
            }

            var categories = await _siteSettingsService.GetFAQCategoriesAsync();

            ViewBag.FAQs = faqs;
            ViewBag.Categories = categories;
            ViewBag.TotalCount = faqs.Count;

            return View();
        }

        /// <summary>
        /// Contact form page
        /// </summary>
        public async Task<IActionResult> Contact()
        {
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            ViewBag.SiteSettings = siteSettings;

            // Pre-fill form if user is logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.UserName = user.FullName;
                    ViewBag.UserEmail = user.Email;
                    ViewBag.UserPhone = user.PhoneNumber;
                }
            }

            return View();
        }

        /// <summary>
        /// Submit contact form
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitContact(ContactInquiry model)
        {
            if (!ModelState.IsValid)
            {
                var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
                ViewBag.SiteSettings = siteSettings;
                TempData["Error"] = _localizer["PleaseFillAllRequiredFieldsCorrectly"].Value;
                return View("Contact", model);
            }

            try
            {
                // Set additional fields
                if (User.Identity?.IsAuthenticated == true)
                {
                    model.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }

                model.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                model.UserAgent = Request.Headers["User-Agent"].ToString();

                // Save inquiry
                var inquiry = await _siteSettingsService.CreateContactInquiryAsync(model);

                // Send confirmation email to user
                try
                {
                    var emailBody = $@"
                        <h2>Thank you for contacting us!</h2>
                        <p>Dear {model.FullName},</p>
                        <p>We have received your inquiry and will get back to you as soon as possible.</p>
                        <h3>Your Message Details:</h3>
                        <p><strong>Subject:</strong> {model.Subject}</p>
                        <p><strong>Category:</strong> {model.Category}</p>
                        <p><strong>Message:</strong></p>
                        <p>{model.Message}</p>
                        <br/>
                        <p>Best regards,<br/>The Support Team</p>
                    ";

                    await _emailService.SendEmailAsync(model.Email, "We received your message - " + model.Subject, emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send confirmation email for inquiry {Id}", inquiry.Id);
                }

                TempData["Success"] = _localizer["YourMessageSentSuccessfully"].Value;
                return RedirectToAction(nameof(ContactSuccess));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting contact form");
                TempData["Error"] = _localizer["ErrorOccurredWhileSubmittingMessage"].Value;
                return View("Contact", model);
            }
        }

        /// <summary>
        /// Contact form success page
        /// </summary>
        public IActionResult ContactSuccess()
        {
            return View();
        }

        /// <summary>
        /// Live chat page - Unified AI Chat for all user types
        /// </summary>
        public async Task<IActionResult> LiveChat()
        {
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            ViewBag.SiteSettings = siteSettings;

            // Default role
            ViewBag.UserRole = "Customer";
            ViewBag.IsSeller = false;
            ViewBag.IsAdmin = false;
            ViewBag.IsModerator = false;

            // Check if user has existing active chat session
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var existingSession = await _chatService.GetActiveSessionByUserIdAsync(userId);
                    if (existingSession != null)
                    {
                        ViewBag.ExistingSession = existingSession;
                    }

                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        ViewBag.UserName = user.FullName;
                        ViewBag.UserEmail = user.Email;
                        ViewBag.UserId = user.Id;

                        // Check user roles
                        var roles = await _userManager.GetRolesAsync(user);

                        if (roles.Contains("SuperAdmin") || roles.Contains("Admin"))
                        {
                            ViewBag.UserRole = "Admin";
                            ViewBag.IsAdmin = true;
                        }
                        else if (roles.Contains("Moderator"))
                        {
                            ViewBag.UserRole = "Moderator";
                            ViewBag.IsModerator = true;
                        }
                        else if (roles.Contains("Seller"))
                        {
                            // Check if user has an active seller account
                            var seller = await _context.Sellers
                                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SellerStatus.Approved);

                            if (seller != null)
                            {
                                ViewBag.UserRole = "Seller";
                                ViewBag.IsSeller = true;
                                ViewBag.SellerId = seller.Id;
                                ViewBag.ShopName = seller.ShopName;

                                // Get seller greeting
                                var greetingMessage = await _aiChatService.GetSellerGreetingMessageAsync(
                                    user.FullName ?? "Seller",
                                    seller.ShopName);
                                ViewBag.SellerGreeting = greetingMessage;
                            }
                        }
                    }
                }
            }

            return View();
        }

        /// <summary>
        /// AI Chat page - Smart AI assistant
        /// </summary>
        public async Task<IActionResult> AIChat()
        {
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            ViewBag.SiteSettings = siteSettings;

            // Get user info if logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.UserName = user.FullName;
                    ViewBag.UserEmail = user.Email;
                    ViewBag.UserId = user.Id;
                }
            }

            return View();
        }

        /// <summary>
        /// Check live chat availability
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckChatAvailability()
        {
            // Check if there are agents online (simplified check)
            var waitingCount = await _chatService.GetWaitingSessionsCountAsync();
            var activeCount = await _chatService.GetActiveSessionsCountAsync();

            // For now, we'll assume chat is always available during business hours
            var currentHour = DateTime.Now.Hour;
            var isBusinessHours = currentHour >= 9 && currentHour < 21; // 9 AM to 9 PM

            return Json(new
            {
                available = isBusinessHours,
                waitingInQueue = waitingCount,
                estimatedWaitMinutes = waitingCount * 2 // Rough estimate
            });
        }

        /// <summary>
        /// Get FAQ by ID (for AJAX requests)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFAQ(int id)
        {
            var faq = await _siteSettingsService.GetFAQByIdAsync(id);
            if (faq == null || !faq.IsActive)
            {
                return NotFound();
            }

            return Json(new
            {
                id = faq.Id,
                question = faq.Question,
                answer = faq.Answer,
                category = faq.Category
            });
        }

        /// <summary>
        /// Submit FAQ feedback (helpful/not helpful)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitFAQFeedback(int faqId, bool isHelpful)
        {
            try
            {
                var faq = await _context.FAQs.FindAsync(faqId);
                if (faq == null || !faq.IsActive)
                {
                    return Json(new { success = false, message = "FAQ not found" });
                }

                // Get user identifier
                string? userId = null;
                string? sessionId = null;
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                if (User.Identity?.IsAuthenticated == true)
                {
                    userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }
                else
                {
                    // Use session ID for guests
                    sessionId = HttpContext.Session.Id;
                    if (string.IsNullOrEmpty(sessionId))
                    {
                        // Generate a cookie-based identifier if session is not available
                        var cookieKey = "faq_feedback_id";
                        if (!Request.Cookies.ContainsKey(cookieKey))
                        {
                            sessionId = Guid.NewGuid().ToString();
                            Response.Cookies.Append(cookieKey, sessionId, new CookieOptions
                            {
                                Expires = DateTime.UtcNow.AddYears(1),
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Lax
                            });
                        }
                        else
                        {
                            sessionId = Request.Cookies[cookieKey];
                        }
                    }
                }

                // Check if user already submitted feedback for this FAQ
                var existingFeedback = await _context.FAQFeedbacks
                    .Where(f => f.FAQId == faqId)
                    .Where(f =>
                        (userId != null && f.UserId == userId) ||
                        (sessionId != null && f.SessionId == sessionId) ||
                        (userId == null && sessionId == null && f.IpAddress == ipAddress))
                    .FirstOrDefaultAsync();

                if (existingFeedback != null)
                {
                    // User already submitted feedback
                    var totalFeedback = faq.HelpfulCount + faq.NotHelpfulCount;
                    var helpfulPercentage = totalFeedback > 0 ? (faq.HelpfulCount * 100 / totalFeedback) : 0;

                    return Json(new
                    {
                        success = false,
                        message = "You have already submitted feedback for this FAQ",
                        alreadySubmitted = true,
                        helpfulCount = faq.HelpfulCount,
                        notHelpfulCount = faq.NotHelpfulCount,
                        helpfulPercentage = helpfulPercentage
                    });
                }

                // Create new feedback
                var feedback = new FAQFeedback
                {
                    FAQId = faqId,
                    UserId = userId,
                    SessionId = sessionId,
                    IpAddress = ipAddress,
                    IsHelpful = isHelpful,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FAQFeedbacks.Add(feedback);

                // Update FAQ counts
                if (isHelpful)
                {
                    faq.HelpfulCount++;
                }
                else
                {
                    faq.NotHelpfulCount++;
                }
                faq.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var totalCount = faq.HelpfulCount + faq.NotHelpfulCount;
                var percentage = totalCount > 0 ? (faq.HelpfulCount * 100 / totalCount) : 0;

                return Json(new
                {
                    success = true,
                    message = "Thank you for your feedback!",
                    helpfulCount = faq.HelpfulCount,
                    notHelpfulCount = faq.NotHelpfulCount,
                    helpfulPercentage = percentage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting FAQ feedback for FAQ {FaqId}", faqId);
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        /// <summary>
        /// Check if user already submitted feedback for a FAQ
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckFAQFeedback(int faqId)
        {
            try
            {
                string? userId = null;
                string? sessionId = null;
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                if (User.Identity?.IsAuthenticated == true)
                {
                    userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }
                else
                {
                    var cookieKey = "faq_feedback_id";
                    if (Request.Cookies.ContainsKey(cookieKey))
                    {
                        sessionId = Request.Cookies[cookieKey];
                    }
                }

                var existingFeedback = await _context.FAQFeedbacks
                    .Where(f => f.FAQId == faqId)
                    .Where(f =>
                        (userId != null && f.UserId == userId) ||
                        (sessionId != null && f.SessionId == sessionId))
                    .FirstOrDefaultAsync();

                var faq = await _context.FAQs.FindAsync(faqId);
                var totalCount = (faq?.HelpfulCount ?? 0) + (faq?.NotHelpfulCount ?? 0);
                var percentage = totalCount > 0 ? ((faq?.HelpfulCount ?? 0) * 100 / totalCount) : 0;

                return Json(new
                {
                    hasSubmitted = existingFeedback != null,
                    isHelpful = existingFeedback?.IsHelpful,
                    helpfulCount = faq?.HelpfulCount ?? 0,
                    notHelpfulCount = faq?.NotHelpfulCount ?? 0,
                    helpfulPercentage = percentage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking FAQ feedback for FAQ {FaqId}", faqId);
                return Json(new { hasSubmitted = false });
            }
        }

        #region Seller AI Chat API Endpoints

        /// <summary>
        /// Get seller order summary via AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSellerOrderSummary()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Json(new { success = false, message = "User not found" });

                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id && s.Status == SellerStatus.Approved);
                if (seller == null) return Json(new { success = false, message = "Seller not found" });

                var summary = await _aiChatService.GetSellerOrderSummaryAsync(seller.Id.ToString());
                return Json(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller order summary");
                return Json(new { success = false, message = "Error fetching order summary" });
            }
        }

        /// <summary>
        /// Get seller sales analytics via AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSellerSalesAnalytics(string period = "today")
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Json(new { success = false, message = "User not found" });

                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id && s.Status == SellerStatus.Approved);
                if (seller == null) return Json(new { success = false, message = "Seller not found" });

                var analytics = await _aiChatService.GetSellerSalesAnalyticsAsync(seller.Id.ToString(), period);
                return Json(new { success = true, data = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller sales analytics");
                return Json(new { success = false, message = "Error fetching sales analytics" });
            }
        }

        /// <summary>
        /// Get seller payment status via AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSellerPaymentStatus()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Json(new { success = false, message = "User not found" });

                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id && s.Status == SellerStatus.Approved);
                if (seller == null) return Json(new { success = false, message = "Seller not found" });

                var paymentStatus = await _aiChatService.GetSellerPaymentStatusAsync(seller.Id.ToString());
                return Json(new { success = true, data = paymentStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller payment status");
                return Json(new { success = false, message = "Error fetching payment status" });
            }
        }

        /// <summary>
        /// Get seller product insights via AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSellerProductInsights()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Json(new { success = false, message = "User not found" });

                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id && s.Status == SellerStatus.Approved);
                if (seller == null) return Json(new { success = false, message = "Seller not found" });

                var insights = await _aiChatService.GetSellerProductInsightsAsync(seller.Id.ToString());
                return Json(new { success = true, data = insights });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller product insights");
                return Json(new { success = false, message = "Error fetching product insights" });
            }
        }

        /// <summary>
        /// Get seller improvement tips via AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSellerImprovementTips()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Json(new { success = false, message = "User not found" });

                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id && s.Status == SellerStatus.Approved);
                if (seller == null) return Json(new { success = false, message = "Seller not found" });

                var tips = await _aiChatService.GetSellerImprovementTipsAsync(seller.Id.ToString());
                return Json(new { success = true, data = tips });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller improvement tips");
                return Json(new { success = false, message = "Error fetching improvement tips" });
            }
        }

        #endregion
    }
}
