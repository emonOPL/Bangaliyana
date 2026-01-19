using System.Security.Claims;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bangaliyana.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchApiController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly IAIChatService _aiChatService;
        private readonly ILogger<SearchApiController> _logger;

        public SearchApiController(
            ISearchService searchService,
            IAIChatService aiChatService,
            ILogger<SearchApiController> logger)
        {
            _searchService = searchService;
            _aiChatService = aiChatService;
            _logger = logger;
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private string? GetSessionId() => HttpContext.Session.Id;

        /// <summary>
        /// Get AI-powered search suggestions as user types
        /// </summary>
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string q, [FromQuery] int maxResults = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                {
                    return Ok(new SearchSuggestionResult());
                }

                var result = await _searchService.GetSuggestionsAsync(q, GetUserId(), GetSessionId(), maxResults);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for query: {Query}", q);
                return StatusCode(500, new { error = "An error occurred while fetching suggestions" });
            }
        }

        /// <summary>
        /// Perform a full product search with filters
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> SearchProducts([FromQuery] SearchRequest request)
        {
            try
            {
                var result = await _searchService.SearchProductsAsync(request, GetUserId(), GetSessionId());

                // Record the search
                if (!string.IsNullOrWhiteSpace(request.Query))
                {
                    _ = _searchService.RecordSearchAsync(request.Query, GetUserId(), GetSessionId(), result.TotalCount);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return StatusCode(500, new { error = "An error occurred while searching" });
            }
        }

        /// <summary>
        /// Record when a user clicks on a search result
        /// </summary>
        [HttpPost("click")]
        public async Task<IActionResult> RecordClick([FromBody] SearchClickRequest request)
        {
            try
            {
                await _searchService.RecordSearchClickAsync(request.SearchHistoryId, request.ProductId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording search click");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        /// <summary>
        /// Get user's recent searches
        /// </summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentSearches([FromQuery] int count = 5)
        {
            try
            {
                var result = await _searchService.GetRecentSearchesAsync(GetUserId(), GetSessionId(), count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent searches");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        /// <summary>
        /// Delete a specific recent search
        /// </summary>
        [HttpDelete("recent/{id}")]
        public async Task<IActionResult> DeleteRecentSearch(int id)
        {
            try
            {
                await _searchService.DeleteRecentSearchAsync(id, GetUserId(), GetSessionId());
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recent search");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        /// <summary>
        /// Clear all recent searches
        /// </summary>
        [HttpDelete("recent")]
        public async Task<IActionResult> ClearRecentSearches()
        {
            try
            {
                await _searchService.ClearRecentSearchesAsync(GetUserId(), GetSessionId());
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing recent searches");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        /// <summary>
        /// Get trending searches
        /// </summary>
        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingSearches([FromQuery] int count = 8)
        {
            try
            {
                var result = await _searchService.GetTrendingSearchesAsync(count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending searches");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        /// <summary>
        /// Get personalized product recommendations
        /// </summary>
        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendations([FromQuery] int count = 10)
        {
            try
            {
                var result = await _searchService.GetRecommendationsAsync(GetUserId(), GetSessionId(), count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        /// <summary>
        /// Record a product view for analytics
        /// </summary>
        [HttpPost("view")]
        public async Task<IActionResult> RecordProductView([FromBody] ProductViewRequest request)
        {
            try
            {
                await _searchService.RecordProductViewAsync(
                    request.ProductId,
                    GetUserId(),
                    GetSessionId(),
                    request.Source,
                    request.SearchTerm,
                    request.ViewDuration
                );
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording product view");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        /// <summary>
        /// Get available search filters for a query
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetSearchFilters([FromQuery] string q)
        {
            try
            {
                var result = await _searchService.GetSearchFiltersAsync(q ?? string.Empty);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search filters");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        /// <summary>
        /// AI-powered natural language search
        /// Understands queries like "blue shirts under 500 taka" or "laptop for gaming"
        /// </summary>
        [HttpGet("ai-search")]
        public async Task<IActionResult> AISearch([FromQuery] string q, [FromQuery] int maxResults = 8)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                {
                    return Ok(new AISearchResponse { Success = false, Message = "Please enter a search query" });
                }

                // Use AI service to search products with intelligent understanding
                var aiResult = await _aiChatService.SearchProductsWithLinksAsync(q, maxResults);

                // Also get traditional suggestions for fallback
                var suggestions = await _searchService.GetSuggestionsAsync(q, GetUserId(), GetSessionId(), 5);

                // Detect language for localized response
                var detectedLanguage = _aiChatService.DetectLanguage(q);

                return Ok(new AISearchResponse
                {
                    Success = aiResult.Found,
                    Message = aiResult.Message,
                    Products = aiResult.Products.Select(p => new AISearchProduct
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        ImageUrl = p.ImageUrl ?? "images/products/noimage.jpg",
                        ProductUrl = p.ProductUrl,
                        CategoryName = p.CategoryName,
                        SellerName = p.SellerName,
                        Stock = p.Stock,
                        InStock = p.InStock,
                        Rating = p.Rating,
                        ReviewCount = p.ReviewCount,
                        HasVariants = p.HasVariants
                    }).ToList(),
                    QuickReplies = aiResult.QuickReplies,
                    RelatedSuggestions = suggestions.Suggestions?.Where(s => s.Type == SuggestionType.Autocomplete)
                        .Select(s => s.Term).Take(5).ToList() ?? new List<string>(),
                    DetectedLanguage = detectedLanguage.ToString(),
                    TotalFound = aiResult.Products.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing AI search for query: {Query}", q);
                return StatusCode(500, new AISearchResponse { Success = false, Message = "Search failed. Please try again." });
            }
        }

        /// <summary>
        /// Quick add to cart from search results
        /// </summary>
        [HttpPost("quick-add-cart")]
        public async Task<IActionResult> QuickAddToCart([FromBody] QuickAddToCartRequest request)
        {
            try
            {
                var result = await _aiChatService.AddToCartViaChatAsync(
                    request.ProductId,
                    request.Quantity,
                    GetUserId(),
                    request.VariantId);

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    productName = result.ProductName,
                    totalPrice = result.TotalPrice,
                    cartUrl = result.CartUrl,
                    requiresVariant = result.RequiresVariant,
                    shopConflict = result.ShopConflict,
                    conflictMessage = result.ConflictMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart from search");
                return StatusCode(500, new { success = false, message = "Failed to add to cart" });
            }
        }

        /// <summary>
        /// Quick add to wishlist from search results
        /// </summary>
        [HttpPost("quick-add-wishlist")]
        public async Task<IActionResult> QuickAddToWishlist([FromBody] QuickWishlistRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new { success = false, message = "Please login to add to wishlist", requiresLogin = true });
                }

                var result = await _aiChatService.AddToWishlistAsync(request.ProductId, userId);

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    totalItems = result.TotalItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to wishlist from search");
                return StatusCode(500, new { success = false, message = "Failed to add to wishlist" });
            }
        }
    }

    public class SearchClickRequest
    {
        public int SearchHistoryId { get; set; }
        public int ProductId { get; set; }
    }

    public class ProductViewRequest
    {
        public int ProductId { get; set; }
        public string? Source { get; set; }
        public string? SearchTerm { get; set; }
        public int ViewDuration { get; set; }
    }

    public class QuickAddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public int? VariantId { get; set; }
    }

    public class QuickWishlistRequest
    {
        public int ProductId { get; set; }
    }

    public class AISearchResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AISearchProduct> Products { get; set; } = new();
        public List<QuickReplyButton>? QuickReplies { get; set; }
        public List<string> RelatedSuggestions { get; set; } = new();
        public string DetectedLanguage { get; set; } = "Banglish";
        public int TotalFound { get; set; }
    }

    public class AISearchProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string ProductUrl { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? SellerName { get; set; }
        public int Stock { get; set; }
        public bool InStock { get; set; }
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool HasVariants { get; set; }
    }
}
