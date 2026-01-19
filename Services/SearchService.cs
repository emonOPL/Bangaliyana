using System.Text.Json;
using System.Text.RegularExpressions;
using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SearchService> _logger;

        public SearchService(ApplicationDbContext context, ILogger<SearchService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SearchSuggestionResult> GetSuggestionsAsync(string query, string? userId, string? sessionId, int maxResults = 10)
        {
            var result = new SearchSuggestionResult();
            var normalizedQuery = NormalizeSearchTerm(query);

            if (string.IsNullOrWhiteSpace(normalizedQuery))
                return result;

            // 1. Get user's recent searches matching the query
            var recentSearches = await GetMatchingRecentSearchesAsync(normalizedQuery, userId, sessionId, 3);
            foreach (var searchTerm in recentSearches)
            {
                result.Suggestions.Add(new SearchSuggestion
                {
                    Term = searchTerm,
                    HighlightedTerm = HighlightMatch(searchTerm, query),
                    Type = SuggestionType.History,
                    Relevance = 1.0
                });
            }

            // 2. Get trending searches matching the query
            var trendingMatches = await _context.TrendingSearches
                .Where(t => t.NormalizedTerm.Contains(normalizedQuery))
                .OrderByDescending(t => t.TrendScore)
                .Take(3)
                .Select(t => new SearchSuggestion
                {
                    Term = t.SearchTerm,
                    HighlightedTerm = t.SearchTerm,
                    Type = SuggestionType.Trending,
                    ResultCount = t.SearchCount,
                    Relevance = t.TrendScore / 100
                })
                .ToListAsync();

            foreach (var trend in trendingMatches)
            {
                trend.HighlightedTerm = HighlightMatch(trend.Term, query);
                if (!result.Suggestions.Any(s => s.Term.Equals(trend.Term, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Suggestions.Add(trend);
                }
            }

            // 3. Get product name suggestions (excluding suspended seller products)
            var productSuggestions = await _context.Products
                .Include(p => p.Seller)
                .Where(p => p.Status == ProductStatus.Active && p.IsAvailable &&
                           (p.Name.Contains(query) || (p.Brand != null && p.Brand.Contains(query))) &&
                           (p.Seller == null || p.Seller.Status == SellerStatus.Approved))
                .OrderByDescending(p => p.SoldCount)
                .ThenByDescending(p => p.ViewCount)
                .Take(5)
                .Select(p => new ProductSuggestion
                {
                    Id = p.Id,
                    Name = p.Name,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Relevance = 0.8
                })
                .ToListAsync();

            result.Products = productSuggestions;

            // 4. Get category suggestions
            var categorySuggestions = await _context.Categories
                .Where(c => c.IsActive && c.Name.Contains(query))
                .Take(3)
                .Select(c => new CategorySuggestion
                {
                    Id = c.Id,
                    Name = c.Name,
                    ProductCount = c.Products.Count(p => p.Status == ProductStatus.Active && p.IsAvailable),
                    IconClass = c.IconClass
                })
                .ToListAsync();

            result.Categories = categorySuggestions;

            // 5. Generate autocomplete suggestions from product names
            var autocompleteTerms = await GenerateAutocompleteSuggestionsAsync(query, 3);
            foreach (var term in autocompleteTerms)
            {
                if (!result.Suggestions.Any(s => s.Term.Equals(term, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Suggestions.Add(new SearchSuggestion
                    {
                        Term = term,
                        HighlightedTerm = HighlightMatch(term, query),
                        Type = SuggestionType.Autocomplete,
                        Relevance = 0.6
                    });
                }
            }

            // Sort suggestions by relevance and limit
            result.Suggestions = result.Suggestions
                .OrderByDescending(s => s.Type == SuggestionType.History ? 1 : 0)
                .ThenByDescending(s => s.Relevance)
                .Take(maxResults)
                .ToList();

            return result;
        }

        public async Task<SearchResult> SearchProductsAsync(SearchRequest request, string? userId, string? sessionId)
        {
            var result = new SearchResult
            {
                Query = request.Query,
                Page = request.Page,
                PageSize = request.PageSize
            };

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Seller)
                .Where(p => p.Status == ProductStatus.Active
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved));

            // Apply search term filter
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var searchTerms = request.Query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(term) ||
                        (p.Description != null && p.Description.ToLower().Contains(term)) ||
                        (p.Brand != null && p.Brand.ToLower().Contains(term)) ||
                        (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(term)) ||
                        (p.Highlights != null && p.Highlights.ToLower().Contains(term)) ||
                        (p.Category != null && p.Category.Name.ToLower().Contains(term))
                    );
                }
            }

            // Apply filters - including subcategories for hierarchical filtering
            if (request.CategoryId.HasValue)
            {
                var categoryIds = await GetCategoryAndDescendantIdsAsync(request.CategoryId.Value);
                query = query.Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value));
            }

            if (request.MinPrice.HasValue)
                query = query.Where(p => (p.DiscountPrice ?? p.Price) >= request.MinPrice);

            if (request.MaxPrice.HasValue)
                query = query.Where(p => (p.DiscountPrice ?? p.Price) <= request.MaxPrice);

            if (!string.IsNullOrWhiteSpace(request.Brand))
                query = query.Where(p => p.Brand != null && p.Brand.ToLower() == request.Brand.ToLower());

            if (request.InStockOnly)
                query = query.Where(p => p.IsAvailable && p.Stock > 0);

            if (request.OnSaleOnly)
                query = query.Where(p => p.DiscountPrice.HasValue && p.DiscountPrice < p.Price);

            // Get total count before pagination
            result.TotalCount = await query.CountAsync();
            result.TotalPages = (int)Math.Ceiling((double)result.TotalCount / request.PageSize);

            // Get user preferences for personalized ranking
            UserPreference? userPref = null;
            if (!string.IsNullOrEmpty(userId))
            {
                userPref = await _context.UserPreferences.FirstOrDefaultAsync(up => up.UserId == userId);
            }

            // Apply sorting with AI-enhanced relevance
            IOrderedQueryable<Products> orderedQuery = request.SortBy switch
            {
                "price_asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
                "price_desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "popular" => query.OrderByDescending(p => p.SoldCount).ThenByDescending(p => p.ViewCount),
                _ => ApplyRelevanceRanking(query, request.Query, userPref)
            };

            // Apply pagination
            var products = await orderedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Map to search items
            result.Products = products.Select(p => new ProductSearchItem
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                ShortDescription = p.ShortDescription,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Brand = p.Brand,
                CategoryName = p.Category?.Name,
                Stock = p.Stock,
                IsAvailable = p.IsAvailable,
                Rating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews.Count,
                SoldCount = p.SoldCount,
                RelevanceScore = CalculateRelevanceScore(p, request.Query, userPref)
            }).ToList();

            // Get available filters
            result.AvailableFilters = await GetSearchFiltersAsync(request.Query);

            // Get related searches
            result.RelatedSearches = await GetRelatedSearchesAsync(request.Query, 5);

            return result;
        }

        public async Task RecordSearchAsync(string searchTerm, string? userId, string? sessionId, int resultCount)
        {
            try
            {
                var normalizedTerm = NormalizeSearchTerm(searchTerm);

                // Record in search history
                var history = new SearchHistory
                {
                    SearchTerm = searchTerm,
                    UserId = userId,
                    SessionId = sessionId,
                    ResultCount = resultCount,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SearchHistories.Add(history);

                // Update or create trending search
                var trending = await _context.TrendingSearches
                    .FirstOrDefaultAsync(t => t.NormalizedTerm == normalizedTerm);

                if (trending == null)
                {
                    trending = new TrendingSearch
                    {
                        SearchTerm = searchTerm,
                        NormalizedTerm = normalizedTerm,
                        SearchCount = 1,
                        TrendScore = 1,
                        LastSearchedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.TrendingSearches.Add(trending);
                }
                else
                {
                    trending.SearchCount++;
                    trending.LastSearchedAt = DateTime.UtcNow;
                    trending.UpdatedAt = DateTime.UtcNow;
                    // Boost trend score for recent searches
                    trending.TrendScore += 1;
                }

                await _context.SaveChangesAsync();

                // Update user preferences if logged in
                if (!string.IsNullOrEmpty(userId))
                {
                    await UpdateUserPreferencesForSearchAsync(userId, searchTerm);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording search: {SearchTerm}", searchTerm);
            }
        }

        public async Task RecordSearchClickAsync(int searchHistoryId, int productId)
        {
            try
            {
                var history = await _context.SearchHistories.FindAsync(searchHistoryId);
                if (history != null)
                {
                    history.HasClicked = true;
                    history.ClickedProductId = productId;

                    // Update trending search click count
                    var normalizedTerm = NormalizeSearchTerm(history.SearchTerm);
                    var trending = await _context.TrendingSearches
                        .FirstOrDefaultAsync(t => t.NormalizedTerm == normalizedTerm);

                    if (trending != null)
                    {
                        trending.ClickCount++;
                        trending.TrendScore += 0.5; // Clicks are valuable
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording search click");
            }
        }

        public async Task<List<RecentSearch>> GetRecentSearchesAsync(string? userId, string? sessionId, int count = 5)
        {
            var query = _context.SearchHistories.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(h => h.UserId == userId);
            else if (!string.IsNullOrEmpty(sessionId))
                query = query.Where(h => h.SessionId == sessionId);
            else
                return new List<RecentSearch>();

            // Properly await the async operation before processing in-memory
            var results = await query
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new { h.Id, h.SearchTerm, h.CreatedAt })
                .Distinct()
                .Take(count * 2) // Get more to filter duplicates
                .ToListAsync();

            return results
                .GroupBy(h => h.SearchTerm.ToLower())
                .Select(g => g.First())
                .Take(count)
                .Select(h => new RecentSearch
                {
                    Id = h.Id,
                    SearchTerm = h.SearchTerm,
                    Term = h.SearchTerm,
                    SearchedAt = h.CreatedAt
                })
                .ToList();
        }

        public async Task DeleteRecentSearchAsync(int searchHistoryId, string? userId, string? sessionId)
        {
            var query = _context.SearchHistories.Where(h => h.Id == searchHistoryId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(h => h.UserId == userId);
            else if (!string.IsNullOrEmpty(sessionId))
                query = query.Where(h => h.SessionId == sessionId);
            else
                return;

            var history = await query.FirstOrDefaultAsync();
            if (history != null)
            {
                _context.SearchHistories.Remove(history);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearRecentSearchesAsync(string? userId, string? sessionId)
        {
            var query = _context.SearchHistories.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(h => h.UserId == userId);
            else if (!string.IsNullOrEmpty(sessionId))
                query = query.Where(h => h.SessionId == sessionId);
            else
                return;

            var histories = await query.ToListAsync();
            _context.SearchHistories.RemoveRange(histories);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TrendingSearchItem>> GetTrendingSearchesAsync(int count = 8)
        {
            return await _context.TrendingSearches
                .OrderByDescending(t => t.TrendScore)
                .Take(count)
                .Select(t => new TrendingSearchItem
                {
                    SearchTerm = t.SearchTerm,
                    Term = t.SearchTerm,
                    SearchCount = t.SearchCount,
                    TrendScore = t.TrendScore
                })
                .ToListAsync();
        }

        public async Task UpdateTrendingScoresAsync()
        {
            try
            {
                var trends = await _context.TrendingSearches.ToListAsync();
                var now = DateTime.UtcNow;

                foreach (var trend in trends)
                {
                    // Apply time decay - reduce score for older searches
                    var daysSinceLastSearch = (now - trend.LastSearchedAt).TotalDays;
                    var decayFactor = Math.Exp(-0.1 * daysSinceLastSearch); // Exponential decay

                    // Calculate new trend score based on search count, click count, and recency
                    var baseScore = trend.SearchCount + (trend.ClickCount * 2);
                    trend.TrendScore = baseScore * decayFactor;
                    trend.UpdatedAt = now;

                    // Remove very old and low-score trends
                    if (trend.TrendScore < 0.1 && daysSinceLastSearch > 30)
                    {
                        _context.TrendingSearches.Remove(trend);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trending scores");
            }
        }

        public async Task RecordProductViewAsync(int productId, string? userId, string? sessionId, string? source, string? searchTerm, int viewDuration = 0)
        {
            try
            {
                var view = new ProductView
                {
                    ProductId = productId,
                    UserId = userId,
                    SessionId = sessionId,
                    Source = source,
                    SearchTerm = searchTerm,
                    ViewDuration = viewDuration,
                    ViewedAt = DateTime.UtcNow
                };

                _context.ProductViews.Add(view);
                await _context.SaveChangesAsync();

                // Update product view count
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    product.ViewCount++;
                    await _context.SaveChangesAsync();
                }

                // Update user preferences if logged in
                if (!string.IsNullOrEmpty(userId))
                {
                    await UpdateUserPreferencesForViewAsync(userId, productId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording product view: {ProductId}", productId);
            }
        }

        public async Task<List<ProductRecommendation>> GetRecommendationsAsync(string? userId, string? sessionId, int count = 10)
        {
            var recommendations = new List<ProductRecommendation>();

            // 1. Get recently viewed products (if any)
            var recentViews = await GetRecentlyViewedProductsAsync(userId, sessionId, 5);
            recommendations.AddRange(recentViews.Select(p => new ProductRecommendation
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                CategoryName = p.Category?.Name,
                Type = RecommendationType.RecentlyViewed,
                Score = 1.0
            }));

            // 2. Get personalized recommendations based on user preferences
            if (!string.IsNullOrEmpty(userId))
            {
                var personalized = await GetPersonalizedRecommendationsAsync(userId, 5);
                recommendations.AddRange(personalized);
            }

            // 3. Get trending products (excluding suspended seller products)
            var trending = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Where(p => p.Status == ProductStatus.Active && p.IsAvailable
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved))
                .OrderByDescending(p => p.SoldCount + p.ViewCount)
                .Take(5)
                .Select(p => new ProductRecommendation
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Type = RecommendationType.Trending,
                    Score = 0.8
                })
                .ToListAsync();

            recommendations.AddRange(trending.Where(t => !recommendations.Any(r => r.Id == t.Id)));

            // 4. Get new arrivals (excluding suspended seller products)
            var newArrivals = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Where(p => p.Status == ProductStatus.Active && p.IsAvailable
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved))
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new ProductRecommendation
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Type = RecommendationType.NewArrivals,
                    Score = 0.7
                })
                .ToListAsync();

            recommendations.AddRange(newArrivals.Where(n => !recommendations.Any(r => r.Id == n.Id)));

            // Remove duplicates and sort by score
            return recommendations
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .OrderByDescending(r => r.Score)
                .Take(count)
                .ToList();
        }

        public async Task UpdateUserPreferencesAsync(string userId)
        {
            try
            {
                var pref = await _context.UserPreferences.FirstOrDefaultAsync(up => up.UserId == userId);
                if (pref == null)
                {
                    pref = new UserPreference
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserPreferences.Add(pref);
                }

                // Analyze user's viewing history
                var views = await _context.ProductViews
                    .Include(v => v.Product)
                    .ThenInclude(p => p!.Category)
                    .Where(v => v.UserId == userId && v.ViewedAt > DateTime.UtcNow.AddDays(-30))
                    .ToListAsync();

                if (views.Any())
                {
                    // Calculate preferred categories
                    var categoryWeights = views
                        .Where(v => v.Product?.CategoryId != null)
                        .GroupBy(v => v.Product!.CategoryId)
                        .ToDictionary(g => g.Key!.Value, g => g.Count());
                    pref.PreferredCategories = JsonSerializer.Serialize(categoryWeights);

                    // Calculate preferred brands
                    var brandWeights = views
                        .Where(v => !string.IsNullOrEmpty(v.Product?.Brand))
                        .GroupBy(v => v.Product!.Brand)
                        .ToDictionary(g => g.Key!, g => g.Count());
                    pref.PreferredBrands = JsonSerializer.Serialize(brandWeights);

                    // Calculate price range
                    var prices = views.Where(v => v.Product != null).Select(v => v.Product!.DiscountPrice ?? v.Product.Price).ToList();
                    if (prices.Any())
                    {
                        pref.MinPricePreference = prices.Min() * 0.8m;
                        pref.MaxPricePreference = prices.Max() * 1.2m;
                    }

                    pref.TotalProductsViewed = views.Count;
                }

                // Count searches
                pref.TotalSearches = await _context.SearchHistories.CountAsync(h => h.UserId == userId);

                // Count purchases
                pref.TotalPurchases = await _context.Orders.CountAsync(o => o.UserId == userId && o.Status != OrderStatus.Cancelled);

                pref.LastActivityAt = DateTime.UtcNow;
                pref.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences: {UserId}", userId);
            }
        }

        public async Task<SearchFilters> GetSearchFiltersAsync(string query)
        {
            var filters = new SearchFilters();

            try
            {
                // Build the search terms array
                var searchTerms = string.IsNullOrWhiteSpace(query)
                    ? Array.Empty<string>()
                    : query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Helper function to create base query - creates fresh IQueryable each time (excluding suspended sellers)
                IQueryable<Products> CreateBaseQuery()
                {
                    var q = _context.Products
                        .AsNoTracking()
                        .Include(p => p.Seller)
                        .Where(p => p.Status == ProductStatus.Active && p.IsAvailable
                            && (p.Seller == null || p.Seller.Status == SellerStatus.Approved));

                    if (searchTerms.Length > 0)
                    {
                        foreach (var term in searchTerms)
                        {
                            q = q.Where(p =>
                                p.Name.ToLower().Contains(term) ||
                                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                                (p.Brand != null && p.Brand.ToLower().Contains(term)));
                        }
                    }

                    return q;
                }

                // Get category filters - using fresh query
                filters.Categories = await CreateBaseQuery()
                    .Include(p => p.Category)
                    .Where(p => p.Category != null)
                    .GroupBy(p => new { p.CategoryId, p.Category!.Name })
                    .Select(g => new FilterOption
                    {
                        Value = g.Key.CategoryId.ToString()!,
                        Label = g.Key.Name,
                        Count = g.Count()
                    })
                    .OrderByDescending(f => f.Count)
                    .Take(10)
                    .ToListAsync();

                // Get brand filters - using fresh query
                filters.Brands = await CreateBaseQuery()
                    .Where(p => !string.IsNullOrEmpty(p.Brand))
                    .GroupBy(p => p.Brand)
                    .Select(g => new FilterOption
                    {
                        Value = g.Key!,
                        Label = g.Key!,
                        Count = g.Count()
                    })
                    .OrderByDescending(f => f.Count)
                    .Take(10)
                    .ToListAsync();

                // Get price range - using fresh query
                var priceData = await CreateBaseQuery()
                    .Select(p => p.DiscountPrice ?? p.Price)
                    .ToListAsync();

                if (priceData.Any())
                {
                    filters.MinPrice = priceData.Min();
                    filters.MaxPrice = priceData.Max();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search filters for query: {Query}", query);
            }

            return filters;
        }

        // Helper methods
        private string NormalizeSearchTerm(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return string.Empty;

            // Convert to lowercase, remove extra spaces, remove special characters
            var normalized = Regex.Replace(term.ToLower().Trim(), @"[^\w\s]", "");
            normalized = Regex.Replace(normalized, @"\s+", " ");
            return normalized;
        }

        private string HighlightMatch(string text, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return text;

            var pattern = Regex.Escape(query);
            return Regex.Replace(text, pattern, $"<strong>{query}</strong>", RegexOptions.IgnoreCase);
        }

        private async Task<List<string>> GetMatchingRecentSearchesAsync(string query, string? userId, string? sessionId, int count)
        {
            var historyQuery = _context.SearchHistories.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                historyQuery = historyQuery.Where(h => h.UserId == userId);
            else if (!string.IsNullOrEmpty(sessionId))
                historyQuery = historyQuery.Where(h => h.SessionId == sessionId);
            else
                return new List<string>();

            return await historyQuery
                .Where(h => h.SearchTerm.ToLower().Contains(query.ToLower()))
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => h.SearchTerm)
                .Distinct()
                .Take(count)
                .ToListAsync();
        }

        private async Task<List<string>> GenerateAutocompleteSuggestionsAsync(string query, int count)
        {
            // Get product names that start with the query (excluding suspended seller products)
            var suggestions = await _context.Products
                .Include(p => p.Seller)
                .Where(p => p.Status == ProductStatus.Active && p.Name.ToLower().StartsWith(query.ToLower())
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved))
                .OrderByDescending(p => p.SoldCount)
                .Take(count)
                .Select(p => p.Name)
                .ToListAsync();

            return suggestions;
        }

        private IOrderedQueryable<Products> ApplyRelevanceRanking(IQueryable<Products> query, string searchTerm, UserPreference? userPref)
        {
            // Apply a composite ranking based on:
            // 1. Name match (highest weight)
            // 2. Sales and popularity
            // 3. User preferences (if available)
            // 4. Recency

            return query.OrderByDescending(p =>
                (p.Name.ToLower().Contains(searchTerm.ToLower()) ? 100 : 0) +
                (p.SoldCount * 0.5) +
                (p.ViewCount * 0.1) +
                (p.IsFeatured ? 20 : 0) +
                (p.DiscountPrice.HasValue ? 10 : 0)
            );
        }

        private double CalculateRelevanceScore(Products product, string searchTerm, UserPreference? userPref)
        {
            double score = 0;

            // Name match score
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalizedSearch = searchTerm.ToLower();
                if (product.Name.ToLower().Contains(normalizedSearch))
                    score += 50;
                if (product.Name.ToLower().StartsWith(normalizedSearch))
                    score += 30;
            }

            // Popularity score
            score += Math.Min(product.SoldCount * 0.1, 20);
            score += Math.Min(product.ViewCount * 0.01, 10);

            // User preference bonus
            if (userPref != null)
            {
                if (!string.IsNullOrEmpty(userPref.PreferredCategories))
                {
                    try
                    {
                        var prefCategories = JsonSerializer.Deserialize<Dictionary<int, int>>(userPref.PreferredCategories);
                        if (prefCategories != null && product.CategoryId.HasValue && prefCategories.ContainsKey(product.CategoryId.Value))
                        {
                            score += 10 * (prefCategories[product.CategoryId.Value] / 10.0);
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(userPref.PreferredBrands) && !string.IsNullOrEmpty(product.Brand))
                {
                    try
                    {
                        var prefBrands = JsonSerializer.Deserialize<Dictionary<string, int>>(userPref.PreferredBrands);
                        if (prefBrands != null && prefBrands.ContainsKey(product.Brand))
                        {
                            score += 10 * (prefBrands[product.Brand] / 10.0);
                        }
                    }
                    catch { }
                }
            }

            return Math.Round(score, 2);
        }

        private async Task<List<string>> GetRelatedSearchesAsync(string query, int count)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();

            // Get searches that users also made when searching for similar terms
            var normalizedQuery = NormalizeSearchTerm(query);
            var words = normalizedQuery.Split(' ');

            var relatedSearches = await _context.TrendingSearches
                .Where(t => words.Any(w => t.NormalizedTerm.Contains(w)) && t.NormalizedTerm != normalizedQuery)
                .OrderByDescending(t => t.TrendScore)
                .Take(count)
                .Select(t => t.SearchTerm)
                .ToListAsync();

            return relatedSearches;
        }

        private async Task UpdateUserPreferencesForSearchAsync(string userId, string searchTerm)
        {
            try
            {
                var pref = await _context.UserPreferences.FirstOrDefaultAsync(up => up.UserId == userId);
                if (pref == null)
                {
                    pref = new UserPreference
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserPreferences.Add(pref);
                }

                // Update interest keywords
                var keywords = new List<string>();
                if (!string.IsNullOrEmpty(pref.InterestKeywords))
                {
                    try
                    {
                        keywords = JsonSerializer.Deserialize<List<string>>(pref.InterestKeywords) ?? new List<string>();
                    }
                    catch { }
                }

                var searchWords = searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in searchWords)
                {
                    if (word.Length > 2 && !keywords.Contains(word))
                    {
                        keywords.Add(word);
                    }
                }

                // Keep only the most recent 50 keywords
                pref.InterestKeywords = JsonSerializer.Serialize(keywords.TakeLast(50).ToList());
                pref.TotalSearches++;
                pref.LastActivityAt = DateTime.UtcNow;
                pref.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences for search");
            }
        }

        private async Task UpdateUserPreferencesForViewAsync(string userId, int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return;

                var pref = await _context.UserPreferences.FirstOrDefaultAsync(up => up.UserId == userId);
                if (pref == null)
                {
                    pref = new UserPreference
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserPreferences.Add(pref);
                }

                // Update preferred categories
                if (product.CategoryId.HasValue)
                {
                    var categories = new Dictionary<int, int>();
                    if (!string.IsNullOrEmpty(pref.PreferredCategories))
                    {
                        try
                        {
                            categories = JsonSerializer.Deserialize<Dictionary<int, int>>(pref.PreferredCategories) ?? new Dictionary<int, int>();
                        }
                        catch { }
                    }

                    if (categories.ContainsKey(product.CategoryId.Value))
                        categories[product.CategoryId.Value]++;
                    else
                        categories[product.CategoryId.Value] = 1;

                    pref.PreferredCategories = JsonSerializer.Serialize(categories);
                }

                // Update preferred brands
                if (!string.IsNullOrEmpty(product.Brand))
                {
                    var brands = new Dictionary<string, int>();
                    if (!string.IsNullOrEmpty(pref.PreferredBrands))
                    {
                        try
                        {
                            brands = JsonSerializer.Deserialize<Dictionary<string, int>>(pref.PreferredBrands) ?? new Dictionary<string, int>();
                        }
                        catch { }
                    }

                    if (brands.ContainsKey(product.Brand))
                        brands[product.Brand]++;
                    else
                        brands[product.Brand] = 1;

                    pref.PreferredBrands = JsonSerializer.Serialize(brands);
                }

                pref.TotalProductsViewed++;
                pref.LastActivityAt = DateTime.UtcNow;
                pref.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences for view");
            }
        }

        private async Task<List<Products>> GetRecentlyViewedProductsAsync(string? userId, string? sessionId, int count)
        {
            var viewsQuery = _context.ProductViews.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                viewsQuery = viewsQuery.Where(v => v.UserId == userId);
            else if (!string.IsNullOrEmpty(sessionId))
                viewsQuery = viewsQuery.Where(v => v.SessionId == sessionId);
            else
                return new List<Products>();

            var productIds = await viewsQuery
                .OrderByDescending(v => v.ViewedAt)
                .Select(v => v.ProductId)
                .Distinct()
                .Take(count)
                .ToListAsync();

            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Where(p => productIds.Contains(p.Id) && p.Status == ProductStatus.Active
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved))
                .ToListAsync();
        }

        private async Task<List<int>> GetCategoryAndDescendantIdsAsync(int categoryId)
        {
            var ids = new List<int> { categoryId };
            var childIds = await _context.Categories
                .Where(c => c.ParentId == categoryId && c.IsActive)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var childId in childIds)
            {
                ids.AddRange(await GetCategoryAndDescendantIdsAsync(childId));
            }

            return ids;
        }

        private async Task<List<ProductRecommendation>> GetPersonalizedRecommendationsAsync(string userId, int count)
        {
            var pref = await _context.UserPreferences.FirstOrDefaultAsync(up => up.UserId == userId);
            if (pref == null) return new List<ProductRecommendation>();

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Where(p => p.Status == ProductStatus.Active && p.IsAvailable
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved));

            // Apply category preferences
            if (!string.IsNullOrEmpty(pref.PreferredCategories))
            {
                try
                {
                    var prefCategories = JsonSerializer.Deserialize<Dictionary<int, int>>(pref.PreferredCategories);
                    if (prefCategories != null && prefCategories.Any())
                    {
                        var topCategories = prefCategories.OrderByDescending(c => c.Value).Take(5).Select(c => c.Key).ToList();
                        query = query.Where(p => p.CategoryId.HasValue && topCategories.Contains(p.CategoryId.Value));
                    }
                }
                catch { }
            }

            // Exclude recently viewed products
            var viewedProductIds = await _context.ProductViews
                .Where(v => v.UserId == userId && v.ViewedAt > DateTime.UtcNow.AddDays(-7))
                .Select(v => v.ProductId)
                .Distinct()
                .ToListAsync();

            query = query.Where(p => !viewedProductIds.Contains(p.Id));

            return await query
                .OrderByDescending(p => p.SoldCount)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .Select(p => new ProductRecommendation
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    DiscountPrice = p.DiscountPrice,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Type = RecommendationType.PersonalizedForYou,
                    Score = 0.9
                })
                .ToListAsync();
        }
    }
}
