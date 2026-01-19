using Bangaliyana.Models;
using System.Text.Json.Serialization;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Advanced AI Chat Service interface for handling automated customer support
    /// Features: Quick Replies, Human Handoff, Cross-sell, Feedback, Analytics
    /// </summary>
    public interface IAIChatService
    {
        /// <summary>
        /// Generate an AI response based on user message and context
        /// </summary>
        Task<AIChatResponse> GetResponseAsync(string userMessage, int sessionId, string? userId = null);

        /// <summary>
        /// Get a greeting message for new chat sessions
        /// </summary>
        Task<string> GetGreetingMessageAsync(string visitorName, string? initialQuery);

        /// <summary>
        /// Submit feedback for an AI response
        /// </summary>
        Task<bool> SubmitFeedbackAsync(int sessionId, string messageId, bool isHelpful, string? comment = null);

        /// <summary>
        /// Check if conversation should be handed off to human agent
        /// </summary>
        Task<HandoffDecision> ShouldHandoffToHumanAsync(int sessionId);

        /// <summary>
        /// Get proactive message based on user behavior
        /// </summary>
        Task<AIChatResponse?> GetProactiveMessageAsync(string? userId, string? currentPage, int secondsOnPage);

        /// <summary>
        /// Get conversation analytics for a session
        /// </summary>
        Task<ConversationAnalytics> GetSessionAnalyticsAsync(int sessionId);

        /// <summary>
        /// Get personalized product recommendations based on conversation
        /// </summary>
        Task<AIChatResponse> GetConversationBasedRecommendationsAsync(int sessionId, string? userId);

        /// <summary>
        /// Add product to cart via chat
        /// </summary>
        Task<AIAddToCartResponse> AddToCartViaChatAsync(int productId, int quantity, string? userId, int? variantId = null);

        /// <summary>
        /// Search and suggest products with direct links
        /// </summary>
        Task<AIProductSearchResult> SearchProductsWithLinksAsync(string query, int maxResults = 5);

        // ============ NEW ADVANCED FEATURES ============

        /// <summary>
        /// Track order by order number or get recent orders
        /// </summary>
        Task<AIOrderTrackingResponse> TrackOrderAsync(string? orderNumber, string? userId);

        /// <summary>
        /// Get user's recent orders
        /// </summary>
        Task<AIOrderTrackingResponse> GetRecentOrdersAsync(string userId, int count = 5);

        /// <summary>
        /// Add product to wishlist via chat
        /// </summary>
        Task<AIWishlistResponse> AddToWishlistAsync(int productId, string userId);

        /// <summary>
        /// Remove product from wishlist via chat
        /// </summary>
        Task<AIWishlistResponse> RemoveFromWishlistAsync(int productId, string userId);

        /// <summary>
        /// Get user's wishlist items
        /// </summary>
        Task<AIWishlistResponse> GetWishlistAsync(string userId);

        /// <summary>
        /// Move wishlist item to cart
        /// </summary>
        Task<AIWishlistResponse> MoveWishlistToCartAsync(int productId, string userId);

        /// <summary>
        /// Find available coupons for user
        /// </summary>
        Task<AICouponResponse> FindAvailableCouponsAsync(string? userId, decimal? cartTotal = null);

        /// <summary>
        /// Validate and apply coupon code
        /// </summary>
        Task<AICouponResponse> ValidateCouponAsync(string couponCode, string? userId, decimal cartTotal);

        /// <summary>
        /// Check if order is eligible for return
        /// </summary>
        Task<AIReturnResponse> CheckReturnEligibilityAsync(string orderNumber, string userId);

        /// <summary>
        /// Get return policy information
        /// </summary>
        Task<AIChatResponse> GetReturnPolicyAsync();

        /// <summary>
        /// Compare multiple products
        /// </summary>
        Task<AIComparisonResponse> CompareProductsAsync(List<int> productIds);

        /// <summary>
        /// Answer product-specific questions using AI
        /// </summary>
        Task<AIChatResponse> AnswerProductQuestionAsync(int productId, string question);

        /// <summary>
        /// Get frequently bought together products
        /// </summary>
        Task<AIChatResponse> GetFrequentlyBoughtTogetherAsync(int productId);

        /// <summary>
        /// Get reorder suggestions based on purchase history
        /// </summary>
        Task<AIChatResponse> GetReorderSuggestionsAsync(string userId);

        // ============ CHAT HISTORY & PERSISTENCE ============

        /// <summary>
        /// Start a new AI chat session
        /// </summary>
        Task<AIChatSessionInfo> StartSessionAsync(string? userId, string? guestSessionId, string? initialQuery = null);

        /// <summary>
        /// Get or create session for user
        /// </summary>
        Task<AIChatSessionInfo> GetOrCreateSessionAsync(string? userId, string? guestSessionId);

        /// <summary>
        /// Get chat history for a session
        /// </summary>
        Task<ChatHistoryResponse> GetChatHistoryAsync(int sessionId, int page = 1, int pageSize = 50);

        /// <summary>
        /// Get recent chat sessions for user
        /// </summary>
        Task<List<AIChatSessionInfo>> GetRecentSessionsAsync(string userId, int count = 10);

        /// <summary>
        /// Save message to history
        /// </summary>
        Task<AIChatMessageInfo> SaveMessageAsync(int sessionId, string content, ChatMessageSender sender,
            ChatMessageType messageType = ChatMessageType.Text, string? richContent = null, string? detectedIntent = null);

        /// <summary>
        /// End/Close a chat session
        /// </summary>
        Task<bool> EndSessionAsync(int sessionId, string? feedback = null, int? rating = null);

        // ============ HUMAN AGENT HANDOFF ============

        /// <summary>
        /// Request handoff to human agent
        /// </summary>
        Task<HandoffRequestResponse> RequestHandoffAsync(int sessionId, string? reason = null);

        /// <summary>
        /// Get handoff queue for agents
        /// </summary>
        Task<List<HandoffQueueItem>> GetHandoffQueueAsync();

        /// <summary>
        /// Accept handoff request (for agents)
        /// </summary>
        Task<bool> AcceptHandoffAsync(int queueId, string agentId);

        /// <summary>
        /// Resolve handoff request
        /// </summary>
        Task<bool> ResolveHandoffAsync(int queueId, string agentId, string? resolutionNotes = null);

        // ============ ANALYTICS & INSIGHTS ============

        /// <summary>
        /// Get daily analytics
        /// </summary>
        Task<AIChatDailyAnalytics> GetDailyAnalyticsAsync(DateTime date);

        /// <summary>
        /// Get analytics for date range
        /// </summary>
        Task<AIChatAnalyticsReport> GetAnalyticsReportAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Record analytics event
        /// </summary>
        Task RecordAnalyticsEventAsync(string eventType, int sessionId, string? additionalData = null);

        /// <summary>
        /// Get top intents
        /// </summary>
        Task<List<IntentAnalytics>> GetTopIntentsAsync(DateTime startDate, DateTime endDate, int count = 10);

        // ============ PERSONALIZATION ============

        /// <summary>
        /// Get user preferences
        /// </summary>
        Task<UserChatPreferences> GetUserPreferencesAsync(string userId);

        /// <summary>
        /// Update user preferences
        /// </summary>
        Task<bool> UpdateUserPreferencesAsync(string userId, UserChatPreferences preferences);

        /// <summary>
        /// Get personalized response based on user history
        /// </summary>
        Task<AIChatResponse> GetPersonalizedResponseAsync(string userMessage, int sessionId, string userId);

        // ============ MULTI-LANGUAGE ============

        /// <summary>
        /// Detect language of message
        /// </summary>
        DetectedLanguage DetectLanguage(string message);

        /// <summary>
        /// Format response in detected language
        /// </summary>
        string FormatResponseInLanguage(string message, DetectedLanguage language);

        // ============ PROACTIVE ENGAGEMENT ============

        /// <summary>
        /// Get active proactive triggers
        /// </summary>
        Task<List<ProactiveTriggerInfo>> GetActiveTriggersAsync();

        /// <summary>
        /// Check if trigger should fire
        /// </summary>
        Task<ProactiveMessageResult?> CheckProactiveTriggerAsync(string? userId, string pageUrl, int timeOnPage, string? deviceType);

        /// <summary>
        /// Record trigger interaction
        /// </summary>
        Task RecordTriggerInteractionAsync(int triggerId, string interactionType);

        // ============ MESSAGE FEEDBACK ============

        /// <summary>
        /// Submit feedback for specific message
        /// </summary>
        Task<bool> SubmitMessageFeedbackAsync(int messageId, bool wasHelpful, string? comment = null);

        /// <summary>
        /// Get feedback summary for session
        /// </summary>
        Task<FeedbackSummary> GetSessionFeedbackSummaryAsync(int sessionId);

        // ============ SELLER SUPPORT FEATURES ============

        /// <summary>
        /// Get AI response for seller queries
        /// </summary>
        Task<AIChatResponse> GetSellerResponseAsync(string userMessage, int sessionId, string sellerId);

        /// <summary>
        /// Get seller's order summary (today's orders, pending, etc.)
        /// </summary>
        Task<SellerOrderSummaryResponse> GetSellerOrderSummaryAsync(string sellerId);

        /// <summary>
        /// Get seller's recent orders with filters
        /// </summary>
        Task<SellerOrdersResponse> GetSellerOrdersAsync(string sellerId, string? status = null, int count = 10);

        /// <summary>
        /// Get seller's sales analytics (daily/weekly/monthly)
        /// </summary>
        Task<SellerSalesAnalyticsResponse> GetSellerSalesAnalyticsAsync(string sellerId, string period = "today");

        /// <summary>
        /// Get seller's payment status (pending/completed)
        /// </summary>
        Task<SellerPaymentStatusResponse> GetSellerPaymentStatusAsync(string sellerId);

        /// <summary>
        /// Get seller's product insights (low stock, best selling, etc.)
        /// </summary>
        Task<SellerProductInsightsResponse> GetSellerProductInsightsAsync(string sellerId);

        /// <summary>
        /// Get improvement tips for seller's shop
        /// </summary>
        Task<SellerImprovementTipsResponse> GetSellerImprovementTipsAsync(string sellerId);

        /// <summary>
        /// Get seller's message/ticket summary
        /// </summary>
        Task<SellerMessageSummaryResponse> GetSellerMessageSummaryAsync(string sellerId);

        /// <summary>
        /// Check if user is a seller
        /// </summary>
        Task<bool> IsSellerAsync(string userId);

        /// <summary>
        /// Get seller greeting message
        /// </summary>
        Task<string> GetSellerGreetingMessageAsync(string sellerName, string? shopName);
    }

    /// <summary>
    /// Response for Add to Cart via Chat
    /// </summary>
    public class AIAddToCartResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? CartUrl { get; set; } = "/Customer/Home/Cart";
        public string? CheckoutUrl { get; set; } = "/Customer/Home/Checkout";
        public bool ShopConflict { get; set; }
        public bool RequiresVariant { get; set; }
        public string? ConflictMessage { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Product search result with links
    /// </summary>
    public class AIProductSearchResult
    {
        public bool Found { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AIProductWithLink> Products { get; set; } = new();
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Product with direct link for chat
    /// </summary>
    public class AIProductWithLink
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string ProductUrl { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public int Stock { get; set; }
        public bool InStock => Stock > 0;
        public string? SellerName { get; set; }
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool HasVariants { get; set; }
        public List<AIProductVariant>? Variants { get; set; }
    }

    /// <summary>
    /// Product variant info
    /// </summary>
    public class AIProductVariant
    {
        public int Id { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public int Stock { get; set; }
        public decimal? PriceAdjustment { get; set; }
    }

    /// <summary>
    /// Enhanced AI Chat Response model with Quick Replies, Cross-sell, and more
    /// </summary>
    public class AIChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; } = true;
        public string? SuggestedAction { get; set; }
        public string? MessageId { get; set; } = Guid.NewGuid().ToString("N")[..8];

        // Product suggestions
        public List<AIProductSuggestion>? ProductSuggestions { get; set; }
        public List<AIFAQSuggestion>? FAQSuggestions { get; set; }

        // Quick Reply Buttons
        public List<QuickReplyButton>? QuickReplies { get; set; }

        // Cross-sell/Upsell Products
        public List<AICrossSellProduct>? CrossSellProducts { get; set; }

        // Urgency/Scarcity Message
        public UrgencyMessage? Urgency { get; set; }

        // Human Handoff
        public bool RequiresHumanHandoff { get; set; } = false;
        public string? HandoffReason { get; set; }

        // Typing Indicator
        public int TypingDelayMs { get; set; } = 0; // Simulated typing delay

        // Confidence Score
        public double ConfidenceScore { get; set; } = 1.0;

        // Response Metadata
        public string? DetectedIntent { get; set; }
        public string? DetectedSentiment { get; set; }
        public bool IsProactive { get; set; } = false;

        // Navigation URL (for navigation intents)
        public string? NavigationUrl { get; set; }
    }

    /// <summary>
    /// Quick Reply Button for easy user interaction
    /// </summary>
    public class QuickReplyButton
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..6];

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; } // send_message, open_url, open_page

        [JsonPropertyName("payload")]
        public string? Payload { get; set; } // Message to send or URL to open

        [JsonPropertyName("style")]
        public string? Style { get; set; } = "default"; // default, primary, success, warning, danger
    }

    /// <summary>
    /// Cross-sell/Upsell Product suggestion
    /// </summary>
    public class AICrossSellProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? Slug { get; set; }
        public string Reason { get; set; } = string.Empty; // "এটাও পছন্দ হতে পারে", "একসাথে কিনলে ভালো"
        public string? Badge { get; set; } // "Best Seller", "New", "Limited"
    }

    /// <summary>
    /// Urgency/Scarcity message for FOMO
    /// </summary>
    public class UrgencyMessage
    {
        public string Type { get; set; } = "stock"; // stock, time, popularity
        public string Message { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int? RemainingCount { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Human handoff decision
    /// </summary>
    public class HandoffDecision
    {
        public bool ShouldHandoff { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Priority { get; set; } = "normal"; // low, normal, high, urgent
        public string? Summary { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Conversation analytics
    /// </summary>
    public class ConversationAnalytics
    {
        public int SessionId { get; set; }
        public int TotalMessages { get; set; }
        public int AIResponses { get; set; }
        public double AverageConfidence { get; set; }
        public string? PrimarySentiment { get; set; }
        public List<string> DetectedIntents { get; set; } = new();
        public List<string> MentionedProducts { get; set; } = new();
        public int UnresolvedQueries { get; set; }
        public bool WasHandedOff { get; set; }
        public TimeSpan Duration { get; set; }
        public int PositiveFeedback { get; set; }
        public int NegativeFeedback { get; set; }
    }

    /// <summary>
    /// AI Feedback record
    /// </summary>
    public class AIFeedback
    {
        public string MessageId { get; set; } = string.Empty;
        public int SessionId { get; set; }
        public bool IsHelpful { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AIProductSuggestion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? Slug { get; set; }
    }

    public class AIFAQSuggestion
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    // ============ NEW RESPONSE CLASSES FOR ADVANCED FEATURES ============

    /// <summary>
    /// Order tracking response
    /// </summary>
    public class AIOrderTrackingResponse
    {
        public bool Found { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AIOrderInfo>? Orders { get; set; }
        public AIOrderInfo? Order { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Order information for chat
    /// </summary>
    public class AIOrderInfo
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusBangla { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CourierName { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public List<AIOrderItemInfo>? Items { get; set; }
        public string OrderUrl { get; set; } = string.Empty;
        public bool CanReturn { get; set; }
        public bool CanCancel { get; set; }
    }

    /// <summary>
    /// Order item info
    /// </summary>
    public class AIOrderItemInfo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? VariantInfo { get; set; }
    }

    /// <summary>
    /// Wishlist response
    /// </summary>
    public class AIWishlistResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public List<AIWishlistItem>? Items { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Wishlist item for chat
    /// </summary>
    public class AIWishlistItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal PriceWhenAdded { get; set; }
        public bool HasPriceDropped { get; set; }
        public decimal PriceDropPercent { get; set; }
        public string? ImageUrl { get; set; }
        public string ProductUrl { get; set; } = string.Empty;
        public bool InStock { get; set; }
        public DateTime AddedAt { get; set; }
    }

    /// <summary>
    /// Coupon response
    /// </summary>
    public class AICouponResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AICouponInfo>? AvailableCoupons { get; set; }
        public AICouponInfo? AppliedCoupon { get; set; }
        public decimal? DiscountAmount { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Coupon info for chat
    /// </summary>
    public class AICouponInfo
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountText { get; set; } = string.Empty;
        public decimal? MinimumOrder { get; set; }
        public decimal? MaxDiscount { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsFirstOrderOnly { get; set; }
        public string? CategoryRestriction { get; set; }
        public bool IsApplicable { get; set; }
        public string? NotApplicableReason { get; set; }
    }

    /// <summary>
    /// Return/Refund response
    /// </summary>
    public class AIReturnResponse
    {
        public bool IsEligible { get; set; }
        public string Message { get; set; } = string.Empty;
        public AIOrderInfo? Order { get; set; }
        public int? DaysRemaining { get; set; }
        public string? ReturnPolicy { get; set; }
        public string? ReturnUrl { get; set; }
        public string? RefundStatus { get; set; }
        public decimal? RefundAmount { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Product comparison response
    /// </summary>
    public class AIComparisonResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AIComparisonProduct>? Products { get; set; }
        public string? Recommendation { get; set; }
        public string? RecommendedProductId { get; set; }
        public List<AIComparisonAttribute>? Attributes { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Product for comparison
    /// </summary>
    public class AIComparisonProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string ProductUrl { get; set; } = string.Empty;
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool InStock { get; set; }
        public string? Brand { get; set; }
        public string? Seller { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new();
    }

    /// <summary>
    /// Comparison attribute row
    /// </summary>
    public class AIComparisonAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string NameBangla { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new();
        public int? BestValueIndex { get; set; }
    }

    // ============ NEW CLASSES FOR ADVANCED FEATURES ============

    /// <summary>
    /// AI Chat Session Info (DTO)
    /// </summary>
    public class AIChatSessionInfo
    {
        public int Id { get; set; }
        public string SessionCode { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? GuestSessionId { get; set; }
        public string? UserName { get; set; }
        public AIChatSessionStatus Status { get; set; }
        public DetectedLanguage DetectedLanguage { get; set; }
        public int TotalMessages { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public string? LastMessage { get; set; }
        public bool HasUnreadMessages { get; set; }
        public bool IsHandoffRequested { get; set; }
        public string? AssignedAgentName { get; set; }
    }

    /// <summary>
    /// AI Chat Message Info (DTO)
    /// </summary>
    public class AIChatMessageInfo
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public ChatMessageSender Sender { get; set; }
        public ChatMessageType MessageType { get; set; }
        public string? RichContent { get; set; }
        public string? QuickReplies { get; set; }
        public string? DetectedIntent { get; set; }
        public double? IntentConfidence { get; set; }
        public string? Sentiment { get; set; }
        public DetectedLanguage Language { get; set; }
        public int? ResponseTimeMs { get; set; }
        public bool? WasHelpful { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Chat history response
    /// </summary>
    public class ChatHistoryResponse
    {
        public int SessionId { get; set; }
        public List<AIChatMessageInfo> Messages { get; set; } = new();
        public int TotalMessages { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasMore { get; set; }
    }

    /// <summary>
    /// Handoff request response
    /// </summary>
    public class HandoffRequestResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? QueueId { get; set; }
        public int? QueuePosition { get; set; }
        public int? EstimatedWaitMinutes { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Handoff queue item for agents
    /// </summary>
    public class HandoffQueueItem
    {
        public int QueueId { get; set; }
        public int SessionId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? Reason { get; set; }
        public string? Category { get; set; }
        public int Priority { get; set; }
        public DateTime RequestedAt { get; set; }
        public int WaitTimeMinutes { get; set; }
        public string? LastMessage { get; set; }
        public int TotalMessages { get; set; }
        public List<string>? DetectedIntents { get; set; }
    }

    /// <summary>
    /// Daily analytics
    /// </summary>
    public class AIChatDailyAnalytics
    {
        public DateTime Date { get; set; }
        public int TotalSessions { get; set; }
        public int TotalMessages { get; set; }
        public int SessionsResolved { get; set; }
        public int SessionsHandedOff { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public Dictionary<string, int> TopIntents { get; set; } = new();
        public Dictionary<string, int> LanguageBreakdown { get; set; } = new();
        public int CartAdditions { get; set; }
        public int WishlistAdditions { get; set; }
    }

    /// <summary>
    /// Analytics report for date range
    /// </summary>
    public class AIChatAnalyticsReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalSessions { get; set; }
        public int TotalMessages { get; set; }
        public int UniqueUsers { get; set; }
        public double ResolutionRate { get; set; }
        public double HandoffRate { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double AverageSessionDuration { get; set; }
        public double AverageRating { get; set; }
        public double SatisfactionScore { get; set; }
        public List<AIChatDailyAnalytics> DailyBreakdown { get; set; } = new();
        public List<IntentAnalytics> TopIntents { get; set; } = new();
        public Dictionary<string, int> TopicsBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Intent analytics
    /// </summary>
    public class IntentAnalytics
    {
        public string Intent { get; set; } = string.Empty;
        public string IntentDisplayName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public double AverageConfidence { get; set; }
        public int ResolvedCount { get; set; }
        public int HandoffCount { get; set; }
    }

    /// <summary>
    /// User chat preferences
    /// </summary>
    public class UserChatPreferences
    {
        public string PreferredLanguage { get; set; } = "banglish";
        public bool AutoDetectLanguage { get; set; } = true;
        public bool EnableNotifications { get; set; } = true;
        public bool EnableSoundNotifications { get; set; } = true;
        public bool EnablePersonalization { get; set; } = true;
        public bool RememberHistory { get; set; } = true;
        public bool ShowRecommendations { get; set; } = true;
        public List<string>? InterestedCategories { get; set; }
    }

    /// <summary>
    /// Proactive trigger info
    /// </summary>
    public class ProactiveTriggerInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TriggerType { get; set; } = string.Empty;
        public int? TriggerValue { get; set; }
        public string? PageUrlPattern { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? MessageBengali { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
        public int DelaySeconds { get; set; }
    }

    /// <summary>
    /// Proactive message result
    /// </summary>
    public class ProactiveMessageResult
    {
        public bool ShouldShow { get; set; }
        public int? TriggerId { get; set; }
        public string? Message { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
        public int DelayMs { get; set; }
    }

    /// <summary>
    /// Feedback summary
    /// </summary>
    public class FeedbackSummary
    {
        public int SessionId { get; set; }
        public int TotalFeedback { get; set; }
        public int PositiveCount { get; set; }
        public int NegativeCount { get; set; }
        public double SatisfactionRate { get; set; }
        public List<string>? CommonIssues { get; set; }
    }

    // ============ SELLER SUPPORT RESPONSE CLASSES ============

    /// <summary>
    /// Seller order summary response
    /// </summary>
    public class SellerOrderSummaryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TodayOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int ReturnRequests { get; set; }
        public decimal TodayRevenue { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Seller orders list response
    /// </summary>
    public class SellerOrdersResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SellerOrderInfo>? Orders { get; set; }
        public int TotalCount { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Seller order info
    /// </summary>
    public class SellerOrderInfo
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBangla { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int ItemCount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? ShippingAddress { get; set; }
        public bool NeedsAction { get; set; }
        public string? ActionRequired { get; set; }
        public string OrderUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Seller sales analytics response
    /// </summary>
    public class SellerSalesAnalyticsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal ComparisonPercentage { get; set; }
        public bool IsGrowth { get; set; }
        public List<SellerDailySales>? DailyBreakdown { get; set; }
        public List<SellerTopProduct>? TopProducts { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Daily sales breakdown
    /// </summary>
    public class SellerDailySales
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    /// <summary>
    /// Top selling product
    /// </summary>
    public class SellerTopProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// Seller payment status response
    /// </summary>
    public class SellerPaymentStatusResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal TotalEarnings { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal AvailableForWithdraw { get; set; }
        public decimal WithdrawnAmount { get; set; }
        public decimal HoldAmount { get; set; }
        public DateTime? NextPayoutDate { get; set; }
        public List<SellerPaymentInfo>? RecentPayments { get; set; }
        public List<SellerPendingPaymentInfo>? PendingPayments { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Seller payment info
    /// </summary>
    public class SellerPaymentInfo
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? TransactionId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Pending payment info
    /// </summary>
    public class SellerPendingPaymentInfo
    {
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ExpectedDate { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Seller product insights response
    /// </summary>
    public class SellerProductInsightsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int DraftProducts { get; set; }
        public List<SellerLowStockProduct>? LowStockItems { get; set; }
        public List<SellerTopProduct>? BestSellers { get; set; }
        public List<SellerSlowMovingProduct>? SlowMoving { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Low stock product alert
    /// </summary>
    public class SellerLowStockProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public string? ImageUrl { get; set; }
        public string ProductUrl { get; set; } = string.Empty;
        public bool IsOutOfStock { get; set; }
    }

    /// <summary>
    /// Slow moving product
    /// </summary>
    public class SellerSlowMovingProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Stock { get; set; }
        public int SoldInLast30Days { get; set; }
        public int DaysSinceLastSale { get; set; }
        public string? SuggestedAction { get; set; }
    }

    /// <summary>
    /// Seller improvement tips response
    /// </summary>
    public class SellerImprovementTipsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal ShopRating { get; set; }
        public int TotalReviews { get; set; }
        public decimal ResponseRate { get; set; }
        public decimal ShipOnTimeRate { get; set; }
        public decimal CancellationRate { get; set; }
        public decimal ReturnRate { get; set; }
        public List<SellerImprovementTip>? Tips { get; set; }
        public List<SellerAchievement>? Achievements { get; set; }
        public List<SellerGoal>? Goals { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Improvement tip
    /// </summary>
    public class SellerImprovementTip
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "medium"; // low, medium, high
        public string Category { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public string? Icon { get; set; }
    }

    /// <summary>
    /// Seller achievement
    /// </summary>
    public class SellerAchievement
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
    }

    /// <summary>
    /// Seller goal
    /// </summary>
    public class SellerGoal
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal TargetValue { get; set; }
        public decimal ProgressPercent { get; set; }
        public string? Reward { get; set; }
    }

    /// <summary>
    /// Seller message summary response
    /// </summary>
    public class SellerMessageSummaryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UnreadMessages { get; set; }
        public int TotalConversations { get; set; }
        public int PendingInquiries { get; set; }
        public int OpenTickets { get; set; }
        public int UrgentTickets { get; set; }
        public double AverageResponseTime { get; set; }
        public List<SellerRecentMessage>? RecentMessages { get; set; }
        public List<QuickReplyButton>? QuickReplies { get; set; }
    }

    /// <summary>
    /// Recent message info
    /// </summary>
    public class SellerRecentMessage
    {
        public int ConversationId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime MessageTime { get; set; }
        public bool IsUnread { get; set; }
        public string? RelatedOrderNumber { get; set; }
        public string ConversationUrl { get; set; } = string.Empty;
    }
}
