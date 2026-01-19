using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Bangaliyana.Services
{
    public class BlogService : IBlogService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BlogService> _logger;

        private const string FEATURED_POSTS_CACHE_KEY = "FeaturedBlogPosts";
        private const string BLOG_CATEGORIES_CACHE_KEY = "BlogCategories";
        private const int CACHE_DURATION_MINUTES = 15;

        public BlogService(
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<BlogService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        #region Blog Posts

        public async Task<List<BlogPost>> GetPublishedPostsAsync(int page = 1, int pageSize = 10)
        {
            return await _db.BlogPosts
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.PublishedAt.HasValue && p.PublishedAt.Value <= DateTime.UtcNow)
                .OrderByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<BlogPost>> GetFeaturedPostsAsync(int count = 5)
        {
            string cacheKey = $"{FEATURED_POSTS_CACHE_KEY}_{count}";

            if (_cache.TryGetValue(cacheKey, out List<BlogPost>? cached) && cached != null)
            {
                return cached;
            }

            var posts = await _db.BlogPosts
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.IsFeatured && p.PublishedAt.HasValue && p.PublishedAt.Value <= DateTime.UtcNow)
                .OrderByDescending(p => p.PublishedAt)
                .Take(count)
                .ToListAsync();

            _cache.Set(cacheKey, posts, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return posts;
        }

        public async Task<BlogPost?> GetPostBySlugAsync(string slug)
        {
            return await _db.BlogPosts
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);
        }

        public async Task<BlogPost?> GetPostByIdAsync(int id)
        {
            return await _db.BlogPosts
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<BlogPost>> GetAllPostsAsync()
        {
            return await _db.BlogPosts
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<BlogPost>> GetPostsByCategoryAsync(int categoryId, int page = 1, int pageSize = 10)
        {
            return await _db.BlogPosts
                .Include(p => p.Category)
                .Where(p => p.BlogCategoryId == categoryId && p.IsActive && p.PublishedAt.HasValue && p.PublishedAt.Value <= DateTime.UtcNow)
                .OrderByDescending(p => p.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<BlogPost>> SearchPostsAsync(string query, int count = 10)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<BlogPost>();

            var normalizedQuery = query.ToLower().Trim();

            return await _db.BlogPosts
                .Include(p => p.Category)
                .Where(p => p.IsActive &&
                    p.PublishedAt.HasValue && p.PublishedAt.Value <= DateTime.UtcNow &&
                    (p.Title.ToLower().Contains(normalizedQuery) ||
                     (p.Excerpt != null && p.Excerpt.ToLower().Contains(normalizedQuery)) ||
                     (p.Tags != null && p.Tags.ToLower().Contains(normalizedQuery))))
                .OrderByDescending(p => p.PublishedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<BlogPost>> GetRelatedPostsAsync(int postId, int count = 4)
        {
            var post = await _db.BlogPosts.FindAsync(postId);
            if (post == null) return new List<BlogPost>();

            // Get posts from same category, excluding current post
            var relatedPosts = await _db.BlogPosts
                .Include(p => p.Category)
                .Where(p => p.Id != postId &&
                    p.IsActive &&
                    p.PublishedAt.HasValue && p.PublishedAt.Value <= DateTime.UtcNow &&
                    p.BlogCategoryId == post.BlogCategoryId)
                .OrderByDescending(p => p.PublishedAt)
                .Take(count)
                .ToListAsync();

            // If not enough related posts, fill with other posts
            if (relatedPosts.Count < count)
            {
                var additionalPosts = await _db.BlogPosts
                    .Include(p => p.Category)
                    .Where(p => p.Id != postId &&
                        p.IsActive &&
                        p.PublishedAt.HasValue && p.PublishedAt.Value <= DateTime.UtcNow &&
                        !relatedPosts.Select(r => r.Id).Contains(p.Id))
                    .OrderByDescending(p => p.ViewCount)
                    .Take(count - relatedPosts.Count)
                    .ToListAsync();

                relatedPosts.AddRange(additionalPosts);
            }

            return relatedPosts;
        }

        public async Task<int> GetTotalPostCountAsync(int? categoryId = null)
        {
            var query = _db.BlogPosts
                .Where(p => p.IsActive && p.PublishedAt.HasValue && p.PublishedAt.Value <= DateTime.UtcNow);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.BlogCategoryId == categoryId.Value);
            }

            return await query.CountAsync();
        }

        public async Task CreatePostAsync(BlogPost post)
        {
            post.CreatedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;

            // Generate slug if not provided
            if (string.IsNullOrWhiteSpace(post.Slug))
            {
                post.Slug = GenerateSlug(post.Title);
            }

            _db.BlogPosts.Add(post);
            await _db.SaveChangesAsync();
            ClearCache();
        }

        public async Task UpdatePostAsync(BlogPost post)
        {
            post.UpdatedAt = DateTime.UtcNow;
            _db.BlogPosts.Update(post);
            await _db.SaveChangesAsync();
            ClearCache();
        }

        public async Task DeletePostAsync(int id)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            if (post != null)
            {
                _db.BlogPosts.Remove(post);
                await _db.SaveChangesAsync();
                ClearCache();
            }
        }

        public async Task IncrementViewCountAsync(int id)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            if (post != null)
            {
                post.ViewCount++;
                await _db.SaveChangesAsync();
            }
        }

        #endregion

        #region Blog Categories

        public async Task<List<BlogCategory>> GetActiveCategoriesAsync()
        {
            if (_cache.TryGetValue(BLOG_CATEGORIES_CACHE_KEY, out List<BlogCategory>? cached) && cached != null)
            {
                return cached;
            }

            var categories = await _db.BlogCategories
                .Include(c => c.Posts.Where(p => p.IsActive && p.PublishedAt.HasValue && p.PublishedAt.Value <= DateTime.UtcNow))
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            _cache.Set(BLOG_CATEGORIES_CACHE_KEY, categories, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return categories;
        }

        public async Task<BlogCategory?> GetCategoryBySlugAsync(string slug)
        {
            return await _db.BlogCategories
                .Include(c => c.Posts)
                .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
        }

        public async Task<BlogCategory?> GetCategoryByIdAsync(int id)
        {
            return await _db.BlogCategories
                .Include(c => c.Posts)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<BlogCategory>> GetAllCategoriesAsync()
        {
            return await _db.BlogCategories
                .Include(c => c.Posts)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        public async Task CreateCategoryAsync(BlogCategory category)
        {
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            // Generate slug if not provided
            if (string.IsNullOrWhiteSpace(category.Slug))
            {
                category.Slug = GenerateSlug(category.Name);
            }

            _db.BlogCategories.Add(category);
            await _db.SaveChangesAsync();
            ClearCache();
        }

        public async Task UpdateCategoryAsync(BlogCategory category)
        {
            category.UpdatedAt = DateTime.UtcNow;
            _db.BlogCategories.Update(category);
            await _db.SaveChangesAsync();
            ClearCache();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _db.BlogCategories.FindAsync(id);
            if (category != null)
            {
                _db.BlogCategories.Remove(category);
                await _db.SaveChangesAsync();
                ClearCache();
            }
        }

        #endregion

        #region Helpers

        private void ClearCache()
        {
            _cache.Remove(FEATURED_POSTS_CACHE_KEY);
            _cache.Remove(BLOG_CATEGORIES_CACHE_KEY);
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

        #endregion
    }
}
