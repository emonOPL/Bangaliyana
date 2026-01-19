using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Bangaliyana.Models;
using Bangaliyana.Services;
using System.Security.Claims;

namespace Bangaliyana.Areas.Customer.Controllers
{
    /// <summary>
    /// API Controller for AI Chat functionality
    /// Handles chat sessions, messaging, handoff, and analytics
    /// </summary>
    [Area("Customer")]
    [Route("api/aichat")]
    [ApiController]
    public class AIChatController : ControllerBase
    {
        private readonly IAIChatService _aiChatService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AIChatController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public AIChatController(
            IAIChatService aiChatService,
            UserManager<ApplicationUser> userManager,
            ILogger<AIChatController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _aiChatService = aiChatService;
            _userManager = userManager;
            _logger = logger;
            _localizer = localizer;
        }

        #region Helper Methods

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        private string GetGuestSessionId()
        {
            var sessionKey = "ai_chat_guest_id";
            if (!Request.Cookies.ContainsKey(sessionKey))
            {
                var guestId = Guid.NewGuid().ToString();
                Response.Cookies.Append(sessionKey, guestId, new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(30),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                });
                return guestId;
            }
            return Request.Cookies[sessionKey]!;
        }

        private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

        #endregion

        #region Session Management

        /// <summary>
        /// Start or resume an AI chat session
        /// </summary>
        [HttpPost("session/start")]
        public async Task<IActionResult> StartSession([FromBody] StartSessionRequest? request)
        {
            try
            {
                var userId = GetUserId();
                var guestSessionId = string.IsNullOrEmpty(userId) ? GetGuestSessionId() : null;

                var session = await _aiChatService.StartSessionAsync(userId, guestSessionId, request?.InitialQuery);

                return Ok(new
                {
                    success = true,
                    session = session
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting AI chat session");
                return StatusCode(500, new { success = false, message = "Failed to start chat session" });
            }
        }

        /// <summary>
        /// Get or create session for current user
        /// </summary>
        [HttpGet("session")]
        public async Task<IActionResult> GetOrCreateSession()
        {
            try
            {
                var userId = GetUserId();
                var guestSessionId = string.IsNullOrEmpty(userId) ? GetGuestSessionId() : null;

                var session = await _aiChatService.GetOrCreateSessionAsync(userId, guestSessionId);

                return Ok(new
                {
                    success = true,
                    session = session
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting/creating AI chat session");
                return StatusCode(500, new { success = false, message = "Failed to get chat session" });
            }
        }

        /// <summary>
        /// Get recent sessions for logged-in user
        /// </summary>
        [HttpGet("sessions/recent")]
        public async Task<IActionResult> GetRecentSessions([FromQuery] int count = 10)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new { success = true, sessions = new List<AIChatSessionInfo>() });
                }

                var sessions = await _aiChatService.GetRecentSessionsAsync(userId, count);
                return Ok(new { success = true, sessions = sessions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent sessions");
                return StatusCode(500, new { success = false, message = "Failed to get recent sessions" });
            }
        }

        /// <summary>
        /// End/Close a chat session
        /// </summary>
        [HttpPost("session/{sessionId}/end")]
        public async Task<IActionResult> EndSession(int sessionId, [FromBody] EndSessionRequest? request)
        {
            try
            {
                var success = await _aiChatService.EndSessionAsync(sessionId, request?.Feedback, request?.Rating);
                return Ok(new { success = success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending session {SessionId}", sessionId);
                return StatusCode(500, new { success = false, message = "Failed to end session" });
            }
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Send a message and get AI response
        /// </summary>
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { success = false, message = "Message is required" });
                }

                var userId = GetUserId();

                // Save user message
                var userMessage = await _aiChatService.SaveMessageAsync(
                    request.SessionId,
                    request.Message,
                    ChatMessageSender.User,
                    ChatMessageType.Text);

                // Get AI response
                AIChatResponse aiResponse;
                if (!string.IsNullOrEmpty(userId))
                {
                    // Use personalized response for logged-in users
                    aiResponse = await _aiChatService.GetPersonalizedResponseAsync(request.Message, request.SessionId, userId);
                }
                else
                {
                    aiResponse = await _aiChatService.GetResponseAsync(request.Message, request.SessionId, userId);
                }

                // Save AI response
                var aiMessage = await _aiChatService.SaveMessageAsync(
                    request.SessionId,
                    aiResponse.Message,
                    ChatMessageSender.AI,
                    aiResponse.ProductSuggestions?.Any() == true ? ChatMessageType.ProductCarousel : ChatMessageType.Text,
                    aiResponse.ProductSuggestions?.Any() == true ? System.Text.Json.JsonSerializer.Serialize(aiResponse.ProductSuggestions) : null,
                    aiResponse.DetectedIntent);

                return Ok(new
                {
                    success = true,
                    userMessage = userMessage,
                    aiResponse = new
                    {
                        message = aiMessage,
                        intent = aiResponse.DetectedIntent,
                        confidence = aiResponse.ConfidenceScore,
                        products = aiResponse.ProductSuggestions,
                        faqSuggestions = aiResponse.FAQSuggestions,
                        quickReplies = aiResponse.QuickReplies,
                        requiresHumanAgent = aiResponse.RequiresHumanHandoff
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message for session {SessionId}", request.SessionId);
                return StatusCode(500, new { success = false, message = "Failed to process message" });
            }
        }

        /// <summary>
        /// Get chat history for a session
        /// </summary>
        [HttpGet("session/{sessionId}/history")]
        public async Task<IActionResult> GetChatHistory(int sessionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var history = await _aiChatService.GetChatHistoryAsync(sessionId, page, pageSize);
                return Ok(new { success = true, history = history });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat history for session {SessionId}", sessionId);
                return StatusCode(500, new { success = false, message = "Failed to get chat history" });
            }
        }

        /// <summary>
        /// Submit feedback for a specific message
        /// </summary>
        [HttpPost("message/{messageId}/feedback")]
        public async Task<IActionResult> SubmitMessageFeedback(int messageId, [FromBody] MessageFeedbackRequest request)
        {
            try
            {
                var success = await _aiChatService.SubmitMessageFeedbackAsync(messageId, request.WasHelpful, request.Comment);
                return Ok(new { success = success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback for message {MessageId}", messageId);
                return StatusCode(500, new { success = false, message = "Failed to submit feedback" });
            }
        }

        #endregion

        #region Advanced Features - Order, Wishlist, Coupon, etc.

        /// <summary>
        /// Track order by order number
        /// </summary>
        [HttpGet("order/track")]
        public async Task<IActionResult> TrackOrder([FromQuery] string? orderNumber)
        {
            try
            {
                var userId = GetUserId();
                var result = await _aiChatService.TrackOrderAsync(orderNumber, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking order {OrderNumber}", orderNumber);
                return StatusCode(500, new { success = false, message = "Failed to track order" });
            }
        }

        /// <summary>
        /// Get recent orders for user
        /// </summary>
        [HttpGet("orders/recent")]
        public async Task<IActionResult> GetRecentOrders([FromQuery] int count = 5)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new AIOrderTrackingResponse
                    {
                        Found = false,
                        Message = "Please login to view your orders"
                    });
                }

                var result = await _aiChatService.GetRecentOrdersAsync(userId, count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders");
                return StatusCode(500, new { success = false, message = "Failed to get orders" });
            }
        }

        /// <summary>
        /// Add product to wishlist
        /// </summary>
        [HttpPost("wishlist/add")]
        public async Task<IActionResult> AddToWishlist([FromBody] WishlistRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new AIWishlistResponse
                    {
                        Success = false,
                        Message = "Please login to add items to wishlist"
                    });
                }

                var result = await _aiChatService.AddToWishlistAsync(request.ProductId, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to wishlist");
                return StatusCode(500, new { success = false, message = "Failed to add to wishlist" });
            }
        }

        /// <summary>
        /// Get user's wishlist
        /// </summary>
        [HttpGet("wishlist")]
        public async Task<IActionResult> GetWishlist()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new AIWishlistResponse
                    {
                        Success = false,
                        Message = "Please login to view your wishlist"
                    });
                }

                var result = await _aiChatService.GetWishlistAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wishlist");
                return StatusCode(500, new { success = false, message = "Failed to get wishlist" });
            }
        }

        /// <summary>
        /// Find available coupons
        /// </summary>
        [HttpGet("coupons")]
        public async Task<IActionResult> GetCoupons([FromQuery] decimal? cartTotal)
        {
            try
            {
                var userId = GetUserId();
                var result = await _aiChatService.FindAvailableCouponsAsync(userId, cartTotal);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coupons");
                return StatusCode(500, new { success = false, message = "Failed to get coupons" });
            }
        }

        /// <summary>
        /// Validate coupon code
        /// </summary>
        [HttpPost("coupon/validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _aiChatService.ValidateCouponAsync(request.CouponCode, userId, request.CartTotal);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating coupon");
                return StatusCode(500, new { success = false, message = "Failed to validate coupon" });
            }
        }

        /// <summary>
        /// Check return eligibility
        /// </summary>
        [HttpGet("order/{orderNumber}/return-eligibility")]
        public async Task<IActionResult> CheckReturnEligibility(string orderNumber)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new AIReturnResponse
                    {
                        IsEligible = false,
                        Message = "Please login to check return eligibility"
                    });
                }

                var result = await _aiChatService.CheckReturnEligibilityAsync(orderNumber, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking return eligibility");
                return StatusCode(500, new { success = false, message = "Failed to check return eligibility" });
            }
        }

        /// <summary>
        /// Compare products
        /// </summary>
        [HttpPost("products/compare")]
        public async Task<IActionResult> CompareProducts([FromBody] CompareProductsRequest request)
        {
            try
            {
                if (request.ProductIds == null || request.ProductIds.Count < 2)
                {
                    return BadRequest(new { success = false, message = "At least 2 products required for comparison" });
                }

                var result = await _aiChatService.CompareProductsAsync(request.ProductIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing products");
                return StatusCode(500, new { success = false, message = "Failed to compare products" });
            }
        }

        /// <summary>
        /// Answer product question
        /// </summary>
        [HttpPost("product/{productId}/question")]
        public async Task<IActionResult> AnswerProductQuestion(int productId, [FromBody] ProductQuestionRequest request)
        {
            try
            {
                var result = await _aiChatService.AnswerProductQuestionAsync(productId, request.Question);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error answering product question");
                return StatusCode(500, new { success = false, message = "Failed to answer question" });
            }
        }

        /// <summary>
        /// Add product to cart via chat
        /// </summary>
        [HttpPost("cart/add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _aiChatService.AddToCartViaChatAsync(
                    request.ProductId,
                    request.Quantity,
                    userId,
                    request.VariantId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart");
                return StatusCode(500, new { success = false, message = "Failed to add to cart" });
            }
        }

        /// <summary>
        /// Search products with links
        /// </summary>
        [HttpGet("products/search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] int maxResults = 5)
        {
            try
            {
                var result = await _aiChatService.SearchProductsWithLinksAsync(query, maxResults);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return StatusCode(500, new { success = false, message = "Failed to search products" });
            }
        }

        #endregion

        #region Human Agent Handoff

        /// <summary>
        /// Request handoff to human agent
        /// </summary>
        [HttpPost("session/{sessionId}/handoff")]
        public async Task<IActionResult> RequestHandoff(int sessionId, [FromBody] HandoffRequest? request)
        {
            try
            {
                var result = await _aiChatService.RequestHandoffAsync(sessionId, request?.Reason);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting handoff for session {SessionId}", sessionId);
                return StatusCode(500, new { success = false, message = "Failed to request handoff" });
            }
        }

        /// <summary>
        /// Get handoff queue (for agents)
        /// </summary>
        [HttpGet("handoff/queue")]
        public async Task<IActionResult> GetHandoffQueue()
        {
            try
            {
                // Check if user is an agent
                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
                {
                    return Forbid();
                }

                var queue = await _aiChatService.GetHandoffQueueAsync();
                return Ok(new { success = true, queue = queue });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting handoff queue");
                return StatusCode(500, new { success = false, message = "Failed to get handoff queue" });
            }
        }

        /// <summary>
        /// Accept handoff (for agents)
        /// </summary>
        [HttpPost("handoff/{queueId}/accept")]
        public async Task<IActionResult> AcceptHandoff(int queueId)
        {
            try
            {
                var agentId = GetUserId();
                if (string.IsNullOrEmpty(agentId))
                {
                    return Unauthorized();
                }

                // Check if user is an agent
                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin") && !User.IsInRole("Moderator"))
                {
                    return Forbid();
                }

                var success = await _aiChatService.AcceptHandoffAsync(queueId, agentId);
                return Ok(new { success = success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting handoff {QueueId}", queueId);
                return StatusCode(500, new { success = false, message = "Failed to accept handoff" });
            }
        }

        /// <summary>
        /// Resolve handoff (for agents)
        /// </summary>
        [HttpPost("handoff/{queueId}/resolve")]
        public async Task<IActionResult> ResolveHandoff(int queueId, [FromBody] ResolveHandoffRequest? request)
        {
            try
            {
                var agentId = GetUserId();
                if (string.IsNullOrEmpty(agentId))
                {
                    return Unauthorized();
                }

                var success = await _aiChatService.ResolveHandoffAsync(queueId, agentId, request?.ResolutionNotes);
                return Ok(new { success = success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving handoff {QueueId}", queueId);
                return StatusCode(500, new { success = false, message = "Failed to resolve handoff" });
            }
        }

        #endregion

        #region User Preferences

        /// <summary>
        /// Get user preferences
        /// </summary>
        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new
                    {
                        success = true,
                        preferences = new UserChatPreferences()
                    });
                }

                var preferences = await _aiChatService.GetUserPreferencesAsync(userId);
                return Ok(new { success = true, preferences = preferences });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preferences");
                return StatusCode(500, new { success = false, message = "Failed to get preferences" });
            }
        }

        /// <summary>
        /// Update user preferences
        /// </summary>
        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] UserChatPreferences preferences)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _aiChatService.UpdateUserPreferencesAsync(userId, preferences);
                return Ok(new { success = success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating preferences");
                return StatusCode(500, new { success = false, message = "Failed to update preferences" });
            }
        }

        #endregion

        #region Proactive Engagement

        /// <summary>
        /// Check proactive trigger
        /// </summary>
        [HttpPost("trigger/check")]
        public async Task<IActionResult> CheckProactiveTrigger([FromBody] ProactiveTriggerRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _aiChatService.CheckProactiveTriggerAsync(
                    userId,
                    request.PageUrl,
                    request.TimeOnPageSeconds,
                    request.DeviceType);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking proactive trigger");
                return StatusCode(500, new { success = false, message = "Failed to check trigger" });
            }
        }

        /// <summary>
        /// Record trigger interaction
        /// </summary>
        [HttpPost("trigger/{triggerId}/interaction")]
        public async Task<IActionResult> RecordTriggerInteraction(int triggerId, [FromBody] TriggerInteractionRequest request)
        {
            try
            {
                await _aiChatService.RecordTriggerInteractionAsync(triggerId, request.InteractionType);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording trigger interaction");
                return StatusCode(500, new { success = false, message = "Failed to record interaction" });
            }
        }

        #endregion

        #region Analytics (Admin only)

        /// <summary>
        /// Get daily analytics
        /// </summary>
        [HttpGet("analytics/daily")]
        public async Task<IActionResult> GetDailyAnalytics([FromQuery] DateTime? date)
        {
            try
            {
                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var targetDate = date ?? DateTime.UtcNow.Date;
                var analytics = await _aiChatService.GetDailyAnalyticsAsync(targetDate);
                return Ok(new { success = true, analytics = analytics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily analytics");
                return StatusCode(500, new { success = false, message = "Failed to get analytics" });
            }
        }

        /// <summary>
        /// Get analytics report for date range
        /// </summary>
        [HttpGet("analytics/report")]
        public async Task<IActionResult> GetAnalyticsReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var report = await _aiChatService.GetAnalyticsReportAsync(startDate, endDate);
                return Ok(new { success = true, report = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics report");
                return StatusCode(500, new { success = false, message = "Failed to get report" });
            }
        }

        /// <summary>
        /// Get top intents
        /// </summary>
        [HttpGet("analytics/intents")]
        public async Task<IActionResult> GetTopIntents([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int count = 10)
        {
            try
            {
                if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var intents = await _aiChatService.GetTopIntentsAsync(startDate, endDate, count);
                return Ok(new { success = true, intents = intents });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top intents");
                return StatusCode(500, new { success = false, message = "Failed to get intents" });
            }
        }

        #endregion

        #region Language

        /// <summary>
        /// Detect language of text
        /// </summary>
        [HttpPost("language/detect")]
        public IActionResult DetectLanguage([FromBody] DetectLanguageRequest request)
        {
            try
            {
                var language = _aiChatService.DetectLanguage(request.Text);
                return Ok(new
                {
                    success = true,
                    language = language.ToString(),
                    languageCode = language switch
                    {
                        DetectedLanguage.Bengali => "bn",
                        DetectedLanguage.English => "en",
                        DetectedLanguage.Banglish => "bn-Latn",
                        _ => "unknown"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting language");
                return StatusCode(500, new { success = false, message = "Failed to detect language" });
            }
        }

        #endregion
    }

    #region Request DTOs

    public class StartSessionRequest
    {
        public string? InitialQuery { get; set; }
    }

    public class EndSessionRequest
    {
        public string? Feedback { get; set; }
        public int? Rating { get; set; }
    }

    public class SendMessageRequest
    {
        public int SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MessageFeedbackRequest
    {
        public bool WasHelpful { get; set; }
        public string? Comment { get; set; }
    }

    public class WishlistRequest
    {
        public int ProductId { get; set; }
    }

    public class ValidateCouponRequest
    {
        public string CouponCode { get; set; } = string.Empty;
        public decimal CartTotal { get; set; }
    }

    public class CompareProductsRequest
    {
        public List<int> ProductIds { get; set; } = new();
    }

    public class ProductQuestionRequest
    {
        public string Question { get; set; } = string.Empty;
    }

    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public int? VariantId { get; set; }
    }

    public class HandoffRequest
    {
        public string? Reason { get; set; }
    }

    public class ResolveHandoffRequest
    {
        public string? ResolutionNotes { get; set; }
    }

    public class ProactiveTriggerRequest
    {
        public string PageUrl { get; set; } = string.Empty;
        public int TimeOnPageSeconds { get; set; }
        public string? DeviceType { get; set; }
    }

    public class TriggerInteractionRequest
    {
        public string InteractionType { get; set; } = string.Empty;
    }

    public class DetectLanguageRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    #endregion
}
