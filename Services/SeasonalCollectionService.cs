using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Bangaliyana.Services
{
    public class SeasonalCollectionService : ISeasonalCollectionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SeasonalCollectionService> _logger;

        private const string ACTIVE_COLLECTIONS_CACHE_KEY = "ActiveSeasonalCollections";
        private const string FEATURED_COLLECTIONS_CACHE_KEY = "FeaturedSeasonalCollections";
        private const int CACHE_DURATION_MINUTES = 15;

        public SeasonalCollectionService(
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<SeasonalCollectionService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<SeasonalCollection>> GetActiveCollectionsAsync()
        {
            if (_cache.TryGetValue(ACTIVE_COLLECTIONS_CACHE_KEY, out List<SeasonalCollection>? cached) && cached != null)
            {
                return cached;
            }

            var collections = await _db.SeasonalCollections
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();

            _cache.Set(ACTIVE_COLLECTIONS_CACHE_KEY, collections, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return collections;
        }

        public async Task<List<SeasonalCollection>> GetFeaturedCollectionsAsync(int count = 4)
        {
            string cacheKey = $"{FEATURED_COLLECTIONS_CACHE_KEY}_{count}";

            if (_cache.TryGetValue(cacheKey, out List<SeasonalCollection>? cached) && cached != null)
            {
                return cached;
            }

            var collections = await _db.SeasonalCollections
                .Where(c => c.IsActive && c.IsFeatured)
                .OrderBy(c => c.DisplayOrder)
                .ThenByDescending(c => c.CreatedAt)
                .Take(count)
                .ToListAsync();

            _cache.Set(cacheKey, collections, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return collections;
        }

        public async Task<List<SeasonalCollection>> GetCurrentCollectionsAsync()
        {
            var now = DateTime.UtcNow;

            return await _db.SeasonalCollections
                .Where(c => c.IsActive &&
                    (!c.StartDate.HasValue || c.StartDate.Value <= now) &&
                    (!c.EndDate.HasValue || c.EndDate.Value >= now))
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<SeasonalCollection>> GetCollectionsBySeasonAsync(SeasonType season, int count = 10)
        {
            return await _db.SeasonalCollections
                .Where(c => c.IsActive && c.SeasonType == season)
                .OrderBy(c => c.DisplayOrder)
                .ThenByDescending(c => c.Year)
                .Take(count)
                .ToListAsync();
        }

        public async Task<SeasonalCollection?> GetCollectionBySlugAsync(string slug)
        {
            return await _db.SeasonalCollections
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
        }

        public async Task<SeasonalCollection?> GetCollectionByIdAsync(int id)
        {
            return await _db.SeasonalCollections.FindAsync(id);
        }

        public async Task<List<SeasonalCollection>> GetAllCollectionsAsync()
        {
            return await _db.SeasonalCollections
                .OrderBy(c => c.DisplayOrder)
                .ThenByDescending(c => c.Year)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task CreateCollectionAsync(SeasonalCollection collection)
        {
            collection.CreatedAt = DateTime.UtcNow;
            collection.UpdatedAt = DateTime.UtcNow;

            // Generate slug if not provided
            if (string.IsNullOrWhiteSpace(collection.Slug))
            {
                collection.Slug = GenerateSlug(collection.Name);
            }

            _db.SeasonalCollections.Add(collection);
            await _db.SaveChangesAsync();
            ClearCache();
        }

        public async Task UpdateCollectionAsync(SeasonalCollection collection)
        {
            collection.UpdatedAt = DateTime.UtcNow;
            _db.SeasonalCollections.Update(collection);
            await _db.SaveChangesAsync();
            ClearCache();
        }

        public async Task DeleteCollectionAsync(int id)
        {
            var collection = await _db.SeasonalCollections.FindAsync(id);
            if (collection != null)
            {
                _db.SeasonalCollections.Remove(collection);
                await _db.SaveChangesAsync();
                ClearCache();
            }
        }

        private void ClearCache()
        {
            _cache.Remove(ACTIVE_COLLECTIONS_CACHE_KEY);
            _cache.Remove(FEATURED_COLLECTIONS_CACHE_KEY);
        }

        private static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var slug = name.ToLower()
                .Replace(" ", "-")
                .Replace("&", "and");

            // Remove invalid characters
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
            // Remove duplicate hyphens
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
            // Trim hyphens from ends
            slug = slug.Trim('-');

            return slug;
        }
    }
}
