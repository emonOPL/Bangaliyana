using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Stores user search history for personalized suggestions
    /// </summary>
    public class SearchHistory
    {
        public int Id { get; set; }

        [StringLength(500)]
        public string SearchTerm { get; set; } = string.Empty;

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // For anonymous users (session-based tracking)
        [StringLength(100)]
        public string? SessionId { get; set; }

        public int ResultCount { get; set; } = 0;

        // Track if user clicked on any result
        public bool HasClicked { get; set; } = false;

        // Track which product was clicked (if any)
        public int? ClickedProductId { get; set; }

        [ForeignKey("ClickedProductId")]
        public virtual Products? ClickedProduct { get; set; }

        // Enhanced tracking fields
        [StringLength(50)]
        public string? DeviceType { get; set; } // "mobile", "desktop", "tablet"

        [StringLength(100)]
        public string? Browser { get; set; }

        [StringLength(100)]
        public string? OperatingSystem { get; set; }

        [StringLength(50)]
        public string? Location { get; set; } // City or region

        // Search context
        [StringLength(50)]
        public string? SearchSource { get; set; } // "navbar", "category_page", "home_page", "voice"

        public bool IsVoiceSearch { get; set; } = false;

        // Response time tracking (milliseconds)
        public int? ResponseTimeMs { get; set; }

        // Filter usage tracking (JSON)
        [StringLength(1000)]
        public string? AppliedFilters { get; set; }

        // Position of clicked result (1-based)
        public int? ClickedPosition { get; set; }

        // Time spent on search results page before clicking (seconds)
        public int? TimeToClick { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Aggregated trending searches across all users
    /// </summary>
    public class TrendingSearch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string SearchTerm { get; set; } = string.Empty;

        // Normalized search term for grouping similar searches
        [StringLength(500)]
        public string NormalizedTerm { get; set; } = string.Empty;

        public int SearchCount { get; set; } = 0;

        public int ClickCount { get; set; } = 0;

        // Weighted score based on recency and frequency
        public double TrendScore { get; set; } = 0;

        // Enhanced metrics
        public int UniqueUsers { get; set; } = 0;

        public int ConversionCount { get; set; } = 0; // How many searches led to purchases

        public double AvgClickPosition { get; set; } = 0; // Average position of clicked results

        public double ClickThroughRate { get; set; } = 0; // CTR percentage

        // Velocity tracking (change rate)
        public double HourlyVelocity { get; set; } = 0;

        public double DailyVelocity { get; set; } = 0;

        public double WeeklyVelocity { get; set; } = 0;

        // Category association
        public int? PrimaryCategoryId { get; set; }

        [ForeignKey("PrimaryCategoryId")]
        public virtual Category? PrimaryCategory { get; set; }

        // Related searches (JSON array)
        [StringLength(2000)]
        public string? RelatedSearches { get; set; }

        // Admin control
        public bool IsPinned { get; set; } = false; // Admin can pin certain searches

        public bool IsHidden { get; set; } = false; // Admin can hide certain searches

        public int DisplayPriority { get; set; } = 0; // Admin override for display order

        public DateTime LastSearchedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Stores user preferences learned from browsing behavior
    /// </summary>
    public class UserPreference
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Preferred categories (JSON array of category IDs with weights)
        public string? PreferredCategories { get; set; }

        // Preferred brands (JSON array of brand names with weights)
        public string? PreferredBrands { get; set; }

        // Price range preferences
        public decimal? MinPricePreference { get; set; }
        public decimal? MaxPricePreference { get; set; }

        // Preferred product types (JSON array)
        public string? PreferredProductTypes { get; set; }

        // Tags/keywords from viewed products
        public string? InterestKeywords { get; set; }

        // Track browsing patterns
        public int TotalProductsViewed { get; set; } = 0;
        public int TotalSearches { get; set; } = 0;
        public int TotalPurchases { get; set; } = 0;

        // Enhanced behavioral data
        public int TotalCartAdds { get; set; } = 0;
        public int TotalWishlistAdds { get; set; } = 0;
        public int TotalReviews { get; set; } = 0;

        // Session metrics
        public double AvgSessionDuration { get; set; } = 0; // In seconds
        public double AvgPagesPerSession { get; set; } = 0;
        public int TotalSessions { get; set; } = 0;

        // Time-based preferences (JSON: {"morning": 0.3, "afternoon": 0.5, "evening": 0.2})
        public string? PeakActivityTimes { get; set; }

        // Day-based preferences (JSON: {"monday": 0.1, "tuesday": 0.2, ...})
        public string? DayPreferences { get; set; }

        // Device preferences (JSON: {"mobile": 0.6, "desktop": 0.4})
        public string? DevicePreferences { get; set; }

        // Search behavior patterns
        public double AvgSearchLength { get; set; } = 0; // Average chars in search query
        public double SearchToClickRate { get; set; } = 0; // Ratio of searches that result in clicks
        public double SearchToPurchaseRate { get; set; } = 0;

        // Content engagement
        public string? ViewedCategories { get; set; } // JSON: Category view history with timestamps
        public string? ClickedProducts { get; set; } // JSON: Recent product click history
        public string? CartHistory { get; set; } // JSON: Recent cart additions

        // Discount sensitivity (how much price drops affect purchase behavior)
        public double DiscountSensitivity { get; set; } = 0.5; // 0-1 scale

        // Brand loyalty (how often user buys from same brands)
        public double BrandLoyalty { get; set; } = 0.5; // 0-1 scale

        // Price sensitivity (prefers budget vs premium)
        public double PriceSensitivity { get; set; } = 0.5; // 0-1 scale

        // User segment (for targeted marketing)
        [StringLength(50)]
        public string? UserSegment { get; set; } // "high_value", "bargain_hunter", "browser", "loyal", "new"

        // AI-computed user profile score
        public double EngagementScore { get; set; } = 0;
        public double ConversionScore { get; set; } = 0;
        public double LifetimeValue { get; set; } = 0;

        // External data integration (for future use)
        public string? ExternalProfiles { get; set; } // JSON for social media, etc.

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Track product views for recommendation engine
    /// </summary>
    public class ProductView
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [StringLength(100)]
        public string? SessionId { get; set; }

        // Time spent on product page (in seconds)
        public int ViewDuration { get; set; } = 0;

        // Source of the view
        [StringLength(50)]
        public string? Source { get; set; } // "search", "category", "recommendation", "direct"

        // Search term that led to this view (if from search)
        [StringLength(500)]
        public string? SearchTerm { get; set; }

        // Enhanced tracking
        [StringLength(50)]
        public string? DeviceType { get; set; }

        [StringLength(100)]
        public string? Browser { get; set; }

        [StringLength(100)]
        public string? OperatingSystem { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(50)]
        public string? Location { get; set; }

        // User interaction tracking
        public bool DidScrollToBottom { get; set; } = false;
        public bool DidViewImages { get; set; } = false;
        public bool DidViewReviews { get; set; } = false;
        public bool DidAddToCart { get; set; } = false;
        public bool DidAddToWishlist { get; set; } = false;
        public bool DidPurchase { get; set; } = false;

        // Referrer information
        [StringLength(500)]
        public string? ReferrerUrl { get; set; }

        [StringLength(50)]
        public string? ReferrerType { get; set; } // "organic", "social", "email", "direct", "paid"

        // Exit behavior
        public DateTime? ExitTime { get; set; }

        [StringLength(100)]
        public string? ExitPage { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// User session tracking for comprehensive analytics
    /// </summary>
    public class UserSession
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [StringLength(100)]
        public string SessionId { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DeviceType { get; set; }

        [StringLength(100)]
        public string? Browser { get; set; }

        [StringLength(100)]
        public string? OperatingSystem { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(50)]
        public string? Location { get; set; }

        [StringLength(10)]
        public string? Country { get; set; }

        // Session metrics
        public int PageViews { get; set; } = 0;
        public int ProductViews { get; set; } = 0;
        public int SearchesPerformed { get; set; } = 0;
        public int ItemsAddedToCart { get; set; } = 0;

        // Duration tracking
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public int DurationSeconds { get; set; } = 0;

        // Entry and exit pages
        [StringLength(500)]
        public string? EntryPage { get; set; }

        [StringLength(500)]
        public string? ExitPage { get; set; }

        // Referrer
        [StringLength(500)]
        public string? ReferrerUrl { get; set; }

        [StringLength(50)]
        public string? ReferrerType { get; set; }

        [StringLength(100)]
        public string? UtmSource { get; set; }

        [StringLength(100)]
        public string? UtmMedium { get; set; }

        [StringLength(100)]
        public string? UtmCampaign { get; set; }

        // Outcome
        public bool DidConvert { get; set; } = false;
        public decimal? ConversionValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Admin-configurable AI search settings
    /// </summary>
    public class AISearchSettings
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string SettingName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Setting value (JSON for complex settings)
        public string? SettingValue { get; set; }

        // Setting type for UI rendering
        [StringLength(50)]
        public string SettingType { get; set; } = "text"; // "text", "number", "boolean", "json", "select", "range"

        // For select/enum types
        public string? AllowedValues { get; set; } // JSON array

        // For numeric types
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }

        // Default value
        public string? DefaultValue { get; set; }

        // Grouping for UI
        [StringLength(50)]
        public string Category { get; set; } = "General"; // "General", "Relevance", "Personalization", "Performance", "Analytics"

        public int DisplayOrder { get; set; } = 0;

        public bool IsEnabled { get; set; } = true;

        public bool IsVisible { get; set; } = true; // Hide from admin UI if false

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Search analytics aggregation for admin dashboard
    /// </summary>
    public class SearchAnalytics
    {
        public int Id { get; set; }

        // Time period
        public DateTime Date { get; set; }

        [StringLength(20)]
        public string Period { get; set; } = "daily"; // "hourly", "daily", "weekly", "monthly"

        // Search metrics
        public int TotalSearches { get; set; } = 0;
        public int UniqueSearches { get; set; } = 0;
        public int UniqueUsers { get; set; } = 0;
        public int ZeroResultSearches { get; set; } = 0;

        // Click metrics
        public int TotalClicks { get; set; } = 0;
        public double AvgClickPosition { get; set; } = 0;
        public double ClickThroughRate { get; set; } = 0;

        // Conversion metrics
        public int SearchesToConversion { get; set; } = 0;
        public decimal ConversionRevenue { get; set; } = 0;
        public double ConversionRate { get; set; } = 0;

        // Performance metrics
        public double AvgResponseTimeMs { get; set; } = 0;
        public int ErrorCount { get; set; } = 0;

        // Top searches (JSON array)
        public string? TopSearches { get; set; }

        // Top zero-result searches (JSON array)
        public string? TopZeroResultSearches { get; set; }

        // Search by device (JSON: {"mobile": 100, "desktop": 200})
        public string? SearchesByDevice { get; set; }

        // Search by hour (JSON: {"0": 10, "1": 5, ...})
        public string? SearchesByHour { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Product recommendations performance tracking
    /// </summary>
    public class RecommendationAnalytics
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        [StringLength(50)]
        public string RecommendationType { get; set; } = string.Empty; // "personalized", "trending", "similar", "recently_viewed"

        [StringLength(50)]
        public string Placement { get; set; } = string.Empty; // "home", "product_page", "cart", "checkout", "search"

        // Impression metrics
        public int Impressions { get; set; } = 0;
        public int UniqueUsersReached { get; set; } = 0;

        // Click metrics
        public int Clicks { get; set; } = 0;
        public double ClickThroughRate { get; set; } = 0;

        // Conversion metrics
        public int AddToCarts { get; set; } = 0;
        public int Purchases { get; set; } = 0;
        public decimal Revenue { get; set; } = 0;
        public double ConversionRate { get; set; } = 0;

        // Performance by product (JSON)
        public string? TopPerformingProducts { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Promoted search terms configured by admin
    /// </summary>
    public class PromotedSearch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string SearchTerm { get; set; } = string.Empty;

        // What to show when this term is searched
        public int? PromotedProductId { get; set; }

        [ForeignKey("PromotedProductId")]
        public virtual Products? PromotedProduct { get; set; }

        public int? PromotedCategoryId { get; set; }

        [ForeignKey("PromotedCategoryId")]
        public virtual Category? PromotedCategory { get; set; }

        // Banner/content to display
        [StringLength(500)]
        public string? BannerImageUrl { get; set; }

        [StringLength(200)]
        public string? BannerText { get; set; }

        [StringLength(500)]
        public string? BannerLink { get; set; }

        // Position boost (higher = more prominent)
        public int BoostValue { get; set; } = 10;

        // Targeting
        public bool ShowToNewUsers { get; set; } = true;
        public bool ShowToReturningUsers { get; set; } = true;

        // Schedule
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Analytics
        public int Impressions { get; set; } = 0;
        public int Clicks { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Search synonyms and redirects configured by admin
    /// </summary>
    public class SearchSynonym
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string SourceTerm { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TargetTerm { get; set; } = string.Empty;

        [StringLength(20)]
        public string SynonymType { get; set; } = "synonym"; // "synonym", "redirect", "expansion"

        // For redirects
        [StringLength(500)]
        public string? RedirectUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Analytics
        public int TimesUsed { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Blocked/filtered search terms
    /// </summary>
    public class BlockedSearchTerm
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Term { get; set; } = string.Empty;

        [StringLength(50)]
        public string BlockType { get; set; } = "block"; // "block", "filter", "warn"

        [StringLength(500)]
        public string? Reason { get; set; }

        // What to show instead
        [StringLength(500)]
        public string? AlternativeMessage { get; set; }

        [StringLength(500)]
        public string? RedirectUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
