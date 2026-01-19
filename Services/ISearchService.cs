using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface ISearchService
    {
        /// <summary>
        /// Get AI-powered search suggestions based on query, user history, and preferences
        /// </summary>
        Task<SearchSuggestionResult> GetSuggestionsAsync(string query, string? userId, string? sessionId, int maxResults = 10);

        /// <summary>
        /// Search products with AI-enhanced ranking
        /// </summary>
        Task<SearchResult> SearchProductsAsync(SearchRequest request, string? userId, string? sessionId);

        /// <summary>
        /// Record a search query for analytics and personalization
        /// </summary>
        Task RecordSearchAsync(string searchTerm, string? userId, string? sessionId, int resultCount);

        /// <summary>
        /// Record when a user clicks on a search result
        /// </summary>
        Task RecordSearchClickAsync(int searchHistoryId, int productId);

        /// <summary>
        /// Get recent searches for the user
        /// </summary>
        Task<List<RecentSearch>> GetRecentSearchesAsync(string? userId, string? sessionId, int count = 5);

        /// <summary>
        /// Delete a specific recent search
        /// </summary>
        Task DeleteRecentSearchAsync(int searchHistoryId, string? userId, string? sessionId);

        /// <summary>
        /// Clear all recent searches for the user
        /// </summary>
        Task ClearRecentSearchesAsync(string? userId, string? sessionId);

        /// <summary>
        /// Get trending searches across the platform
        /// </summary>
        Task<List<TrendingSearchItem>> GetTrendingSearchesAsync(int count = 8);

        /// <summary>
        /// Update trending search scores (should be called periodically)
        /// </summary>
        Task UpdateTrendingScoresAsync();

        /// <summary>
        /// Record a product view for personalization
        /// </summary>
        Task RecordProductViewAsync(int productId, string? userId, string? sessionId, string? source, string? searchTerm, int viewDuration = 0);

        /// <summary>
        /// Get personalized product recommendations
        /// </summary>
        Task<List<ProductRecommendation>> GetRecommendationsAsync(string? userId, string? sessionId, int count = 10);

        /// <summary>
        /// Update user preferences based on activity
        /// </summary>
        Task UpdateUserPreferencesAsync(string userId);

        /// <summary>
        /// Get available search filters based on current results
        /// </summary>
        Task<SearchFilters> GetSearchFiltersAsync(string query);
    }

    // DTOs for search functionality
    public class SearchSuggestionResult
    {
        public List<SearchSuggestion> Suggestions { get; set; } = new();
        public List<ProductSuggestion> Products { get; set; } = new();
        public List<CategorySuggestion> Categories { get; set; } = new();
    }

    public class SearchSuggestion
    {
        public string Term { get; set; } = string.Empty;
        public string HighlightedTerm { get; set; } = string.Empty;
        public SuggestionType Type { get; set; }
        public int? ResultCount { get; set; }
        public double Relevance { get; set; }
    }

    public enum SuggestionType
    {
        History,      // From user's search history
        Trending,     // Popular searches
        Product,      // Product name match
        Category,     // Category name match
        Brand,        // Brand name match
        Autocomplete  // AI-generated autocomplete
    }

    public class ProductSuggestion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? CategoryName { get; set; }
        public double Relevance { get; set; }
    }

    public class CategorySuggestion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public string? IconClass { get; set; }
    }

    public class SearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public int? ProductTypeId { get; set; }
        public int? SpecialTagId { get; set; }
        public int? SubcategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Brand { get; set; }
        public List<string>? Tags { get; set; }
        public string SortBy { get; set; } = "relevance"; // relevance, price_asc, price_desc, newest, popular
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool InStockOnly { get; set; } = false;
        public bool OnSaleOnly { get; set; } = false;
    }

    public class SearchResult
    {
        public List<ProductSearchItem> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public string Query { get; set; } = string.Empty;
        public SearchFilters AvailableFilters { get; set; } = new();
        public string? CorrectedQuery { get; set; } // For typo corrections
        public List<string> RelatedSearches { get; set; } = new();
    }

    public class ProductSearchItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? ShortDescription { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal DiscountedPrice => DiscountPrice ?? Price;
        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
        public string? Brand { get; set; }
        public string? CategoryName { get; set; }
        public string? ProductTypeName { get; set; }
        public int Stock { get; set; }
        public bool IsAvailable { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public int SoldCount { get; set; }
        public double RelevanceScore { get; set; }
    }

    public class SearchFilters
    {
        public List<FilterOption> Categories { get; set; } = new();
        public List<FilterOption> Brands { get; set; } = new();
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public List<FilterOption> Tags { get; set; } = new();
    }

    public class FilterOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RecentSearch
    {
        public int Id { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public DateTime SearchedAt { get; set; }
    }

    public class TrendingSearchItem
    {
        public string SearchTerm { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public int SearchCount { get; set; }
        public double TrendScore { get; set; }
    }

    public class ProductRecommendation
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? CategoryName { get; set; }
        public RecommendationType Type { get; set; }
        public double Score { get; set; }
    }

    public enum RecommendationType
    {
        PersonalizedForYou,
        RecentlyViewed,
        FrequentlyBoughtTogether,
        SimilarProducts,
        Trending,
        NewArrivals
    }
}
