using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Bangaliyana.Services
{
    public class StylingGuideService : IStylingGuideService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<StylingGuideService> _logger;

        private const string FEATURED_GUIDES_CACHE_KEY = "FeaturedStylingGuides";
        private const int CACHE_DURATION_MINUTES = 15;

        public StylingGuideService(
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<StylingGuideService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<StylingGuide>> GetActiveGuidesAsync(int page = 1, int pageSize = 10)
        {
            return await _db.StylingGuides
                .Where(g => g.IsActive)
                .OrderBy(g => g.DisplayOrder)
                .ThenByDescending(g => g.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<StylingGuide>> GetFeaturedGuidesAsync(int count = 4)
        {
            string cacheKey = $"{FEATURED_GUIDES_CACHE_KEY}_{count}";

            if (_cache.TryGetValue(cacheKey, out List<StylingGuide>? cached) && cached != null)
            {
                return cached;
            }

            var guides = await _db.StylingGuides
                .Where(g => g.IsActive && g.IsFeatured)
                .OrderBy(g => g.DisplayOrder)
                .ThenByDescending(g => g.CreatedAt)
                .Take(count)
                .ToListAsync();

            _cache.Set(cacheKey, guides, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return guides;
        }

        public async Task<List<StylingGuide>> GetGuidesByTypeAsync(StylingGuideType type, int count = 10)
        {
            return await _db.StylingGuides
                .Where(g => g.IsActive && g.GuideType == type)
                .OrderBy(g => g.DisplayOrder)
                .ThenByDescending(g => g.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<StylingGuide?> GetGuideBySlugAsync(string slug)
        {
            return await _db.StylingGuides
                .FirstOrDefaultAsync(g => g.Slug == slug && g.IsActive);
        }

        public async Task<StylingGuide?> GetGuideByIdAsync(int id)
        {
            return await _db.StylingGuides.FindAsync(id);
        }

        public async Task<List<StylingGuide>> GetAllGuidesAsync()
        {
            return await _db.StylingGuides
                .OrderBy(g => g.DisplayOrder)
                .ThenByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<StylingGuide>> SearchGuidesAsync(string query, int count = 10)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<StylingGuide>();

            var normalizedQuery = query.ToLower().Trim();

            return await _db.StylingGuides
                .Where(g => g.IsActive &&
                    (g.Title.ToLower().Contains(normalizedQuery) ||
                     (g.Subtitle != null && g.Subtitle.ToLower().Contains(normalizedQuery)) ||
                     (g.Occasion != null && g.Occasion.ToLower().Contains(normalizedQuery))))
                .OrderBy(g => g.DisplayOrder)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetTotalGuideCountAsync(StylingGuideType? type = null)
        {
            var query = _db.StylingGuides.Where(g => g.IsActive);

            if (type.HasValue)
            {
                query = query.Where(g => g.GuideType == type.Value);
            }

            return await query.CountAsync();
        }

        public async Task CreateGuideAsync(StylingGuide guide)
        {
            guide.CreatedAt = DateTime.UtcNow;
            guide.UpdatedAt = DateTime.UtcNow;

            // Generate slug if not provided
            if (string.IsNullOrWhiteSpace(guide.Slug))
            {
                guide.Slug = GenerateSlug(guide.Title);
            }

            _db.StylingGuides.Add(guide);
            await _db.SaveChangesAsync();
            ClearCache();
        }

        public async Task UpdateGuideAsync(StylingGuide guide)
        {
            guide.UpdatedAt = DateTime.UtcNow;
            _db.StylingGuides.Update(guide);
            await _db.SaveChangesAsync();
            ClearCache();
        }

        public async Task DeleteGuideAsync(int id)
        {
            var guide = await _db.StylingGuides.FindAsync(id);
            if (guide != null)
            {
                _db.StylingGuides.Remove(guide);
                await _db.SaveChangesAsync();
                ClearCache();
            }
        }

        public async Task IncrementViewCountAsync(int id)
        {
            var guide = await _db.StylingGuides.FindAsync(id);
            if (guide != null)
            {
                guide.ViewCount++;
                await _db.SaveChangesAsync();
            }
        }

        private void ClearCache()
        {
            _cache.Remove(FEATURED_GUIDES_CACHE_KEY);
        }

        private static string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;

            var slug = title.ToLower()
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
