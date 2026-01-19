using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// AI Chat session status
    /// </summary>
    public enum AIChatSessionStatus
    {
        Active,             // Active AI chat
        HandoffRequested,   // User requested human agent
        HandedOff,          // Transferred to human agent
        Resolved,           // Session resolved by AI
        Closed,             // Session closed
        Abandoned           // User left without resolution
    }

    /// <summary>
    /// Message sender type
    /// </summary>
    public enum ChatMessageSender
    {
        User,
        AI,
        System,
        Agent
    }

    /// <summary>
    /// Message content type for rich media
    /// </summary>
    public enum ChatMessageType
    {
        Text,
        ProductCard,
        ProductCarousel,
        OrderCard,
        QuickReplies,
        Image,
        File,
        Location,
        Rating,
        Feedback
    }

    /// <summary>
    /// Detected language
    /// </summary>
    public enum DetectedLanguage
    {
        Bengali,
        English,
        Banglish,    // Mixed Bengali-English
        Unknown
    }

    /// <summary>
    /// AI Chat Session - Persistent conversation history
    /// </summary>
    public class AIChatSession
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SessionCode { get; set; } = string.Empty;

        // User info (can be guest with session ID or logged-in user)
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [StringLength(100)]
        public string? GuestSessionId { get; set; }

        [StringLength(100)]
        public string? GuestName { get; set; }

        // Session context
        [StringLength(500)]
        public string? InitialQuery { get; set; }

        public AIChatSessionStatus Status { get; set; } = AIChatSessionStatus.Active;

        // Language preference
        public DetectedLanguage DetectedLanguage { get; set; } = DetectedLanguage.Banglish;

        [StringLength(20)]
        public string PreferredLanguage { get; set; } = "banglish";

        // Handoff info (if transferred to human)
        public string? AssignedAgentId { get; set; }

        [ForeignKey("AssignedAgentId")]
        public virtual ApplicationUser? AssignedAgent { get; set; }

        public DateTime? HandoffRequestedAt { get; set; }
        public DateTime? HandoffAcceptedAt { get; set; }

        [StringLength(500)]
        public string? HandoffReason { get; set; }

        // Metrics
        public int TotalMessages { get; set; } = 0;
        public int AIResponses { get; set; } = 0;
        public int UserMessages { get; set; } = 0;
        public double AverageResponseTime { get; set; } = 0;

        // User satisfaction
        [Range(1, 5)]
        public int? Rating { get; set; }

        [StringLength(1000)]
        public string? Feedback { get; set; }

        public bool WasHelpful { get; set; } = true;

        // Tracking
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(500)]
        public string? PageUrl { get; set; }

        [StringLength(100)]
        public string? DeviceType { get; set; }

        // Context data (JSON)
        public string? ContextData { get; set; }

        // Topics discussed (JSON array)
        public string? Topics { get; set; }

        // Intents detected (JSON array)
        public string? DetectedIntents { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        // Navigation
        public virtual ICollection<AIChatMessage> Messages { get; set; } = new List<AIChatMessage>();
    }

    /// <summary>
    /// Individual AI chat message
    /// </summary>
    public class AIChatMessage
    {
        public int Id { get; set; }

        public int SessionId { get; set; }

        [ForeignKey("SessionId")]
        public virtual AIChatSession Session { get; set; } = null!;

        // Message content
        [Required]
        public string Content { get; set; } = string.Empty;

        public ChatMessageSender Sender { get; set; }

        public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;

        // Rich content (JSON for product cards, carousels, etc.)
        public string? RichContent { get; set; }

        // Quick replies (JSON)
        public string? QuickReplies { get; set; }

        // Intent detection
        [StringLength(100)]
        public string? DetectedIntent { get; set; }

        [Range(0, 1)]
        public double? IntentConfidence { get; set; }

        // Sentiment
        [StringLength(50)]
        public string? Sentiment { get; set; }

        // Language
        public DetectedLanguage Language { get; set; } = DetectedLanguage.Banglish;

        // Response time (in milliseconds)
        public int? ResponseTimeMs { get; set; }

        // User feedback on this specific message
        public bool? WasHelpful { get; set; }

        [StringLength(500)]
        public string? UserFeedback { get; set; }

        // Attachments
        [StringLength(500)]
        public string? AttachmentUrl { get; set; }

        [StringLength(100)]
        public string? AttachmentName { get; set; }

        [StringLength(50)]
        public string? AttachmentType { get; set; }

        // Read status
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        // For agent messages
        public string? AgentId { get; set; }

        [ForeignKey("AgentId")]
        public virtual ApplicationUser? Agent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI Chat Analytics - Daily aggregated metrics
    /// </summary>
    public class AIChatAnalytics
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        // Session metrics
        public int TotalSessions { get; set; }
        public int NewSessions { get; set; }
        public int ReturningUsers { get; set; }
        public int GuestSessions { get; set; }
        public int AuthenticatedSessions { get; set; }

        // Message metrics
        public int TotalMessages { get; set; }
        public int UserMessages { get; set; }
        public int AIMessages { get; set; }
        public int AgentMessages { get; set; }

        // Resolution metrics
        public int SessionsResolved { get; set; }
        public int SessionsHandedOff { get; set; }
        public int SessionsAbandoned { get; set; }

        // Performance metrics
        public double AverageResponseTimeMs { get; set; }
        public double AverageSessionDuration { get; set; }
        public double AverageMessagesPerSession { get; set; }

        // Satisfaction metrics
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int PositiveFeedback { get; set; }
        public int NegativeFeedback { get; set; }

        // Intent metrics (JSON - intent name: count)
        public string? TopIntents { get; set; }

        // Topic metrics (JSON - topic: count)
        public string? TopTopics { get; set; }

        // Language metrics
        public int BengaliMessages { get; set; }
        public int EnglishMessages { get; set; }
        public int BanglishMessages { get; set; }

        // Conversion metrics
        public int CartAdditions { get; set; }
        public int WishlistAdditions { get; set; }
        public int OrdersTracked { get; set; }
        public int ProductsSearched { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI Chat Handoff Queue - For human agent escalation
    /// </summary>
    public class AIChatHandoffQueue
    {
        public int Id { get; set; }

        public int SessionId { get; set; }

        [ForeignKey("SessionId")]
        public virtual AIChatSession Session { get; set; } = null!;

        // UserId is nullable for guest users
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public int Priority { get; set; } = 5; // 1-10, 1 being highest

        // Assigned agent
        public string? AssignedAgentId { get; set; }

        [ForeignKey("AssignedAgentId")]
        public virtual ApplicationUser? AssignedAgent { get; set; }

        public bool IsAssigned { get; set; }
        public bool IsResolved { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AssignedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        [StringLength(1000)]
        public string? ResolutionNotes { get; set; }

        // Estimated wait time (minutes)
        public int? EstimatedWaitTime { get; set; }
    }

    /// <summary>
    /// User AI Chat Preferences
    /// </summary>
    public class UserAIChatPreference
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        // Language preferences
        [StringLength(20)]
        public string PreferredLanguage { get; set; } = "banglish";

        public bool AutoDetectLanguage { get; set; } = true;

        // Notification preferences
        public bool EnableChatNotifications { get; set; } = true;
        public bool EnableEmailNotifications { get; set; } = false;
        public bool EnableSoundNotifications { get; set; } = true;

        // Personalization
        public bool EnablePersonalizedResponses { get; set; } = true;
        public bool RememberConversationHistory { get; set; } = true;
        public bool ShowProductRecommendations { get; set; } = true;

        // Categories of interest (JSON array)
        public string? InterestedCategories { get; set; }

        // Browsing behavior (JSON)
        public string? BrowsingBehavior { get; set; }

        // Purchase preferences (JSON)
        public string? PurchasePreferences { get; set; }

        // Total interactions
        public int TotalSessions { get; set; }
        public int TotalMessages { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Proactive Chat Trigger - Rules for proactive engagement
    /// </summary>
    public class ProactiveChatTrigger
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Trigger conditions
        [StringLength(50)]
        public string TriggerType { get; set; } = "PageView"; // PageView, TimeOnPage, ScrollDepth, CartAbandonment, etc.

        // Condition value (e.g., seconds for time, percentage for scroll)
        public int? TriggerValue { get; set; }

        // Page URL pattern (regex)
        [StringLength(500)]
        public string? PageUrlPattern { get; set; }

        // Target audience
        public bool TargetGuests { get; set; } = true;
        public bool TargetAuthenticated { get; set; } = true;
        public bool TargetNewUsers { get; set; } = true;
        public bool TargetReturningUsers { get; set; } = true;

        // Message to display
        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MessageBengali { get; set; }

        // Quick replies (JSON)
        public string? QuickReplies { get; set; }

        // Timing
        public int DelaySeconds { get; set; } = 0;
        public int CooldownMinutes { get; set; } = 30; // Don't show again for X minutes

        // Priority (higher = shown first if multiple triggers match)
        public int Priority { get; set; } = 5;

        // Analytics
        public int TimesTriggered { get; set; }
        public int TimesClicked { get; set; }
        public int TimesConverted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI Learning Record - Tracks patterns from user feedback for self-improvement
    /// </summary>
    public class AILearningRecord
    {
        public int Id { get; set; }

        // The intent/pattern this learning applies to
        [Required]
        [StringLength(100)]
        public string Intent { get; set; } = string.Empty;

        // Original user query pattern that triggered learning
        [StringLength(500)]
        public string? QueryPattern { get; set; }

        // The response that received feedback
        [StringLength(2000)]
        public string? OriginalResponse { get; set; }

        // Improved response based on negative feedback analysis
        [StringLength(2000)]
        public string? ImprovedResponse { get; set; }

        // Feedback statistics
        public int PositiveFeedbackCount { get; set; }
        public int NegativeFeedbackCount { get; set; }

        // Confidence adjustment based on feedback (-1.0 to 1.0)
        public double ConfidenceAdjustment { get; set; }

        // Keywords to add for better intent detection
        public string? AdditionalKeywords { get; set; }

        // Is this learning active?
        public bool IsActive { get; set; } = true;

        // Last time this learning was applied
        public DateTime? LastAppliedAt { get; set; }
        public int TimesApplied { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI Feedback Analysis - Aggregated feedback data for AI improvement
    /// </summary>
    public class AIFeedbackAnalysis
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Intent { get; set; } = string.Empty;

        // Date for this analysis (one record per intent per day)
        public DateTime AnalysisDate { get; set; }

        // Feedback counts
        public int TotalResponses { get; set; }
        public int PositiveCount { get; set; }
        public int NegativeCount { get; set; }

        // Satisfaction rate (0-100)
        public double SatisfactionRate { get; set; }

        // Common negative feedback patterns (JSON array)
        public string? CommonIssues { get; set; }

        // Suggested improvements (JSON array)
        public string? SuggestedImprovements { get; set; }

        // Has this been processed for learning?
        public bool IsProcessed { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
