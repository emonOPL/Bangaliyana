using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class BlogController : Controller
    {
        private readonly IBlogService _blogService;
        private readonly IStylingGuideService _stylingGuideService;
        private readonly ISeasonalCollectionService _collectionService;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ILogger<BlogController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public BlogController(
            IBlogService blogService,
            IStylingGuideService stylingGuideService,
            ISeasonalCollectionService collectionService,
            ISiteSettingsService siteSettingsService,
            ILogger<BlogController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _blogService = blogService;
            _stylingGuideService = stylingGuideService;
            _collectionService = collectionService;
            _siteSettingsService = siteSettingsService;
            _logger = logger;
            _localizer = localizer;
        }

        /// <summary>
        /// Blog landing page with Fashion Tips, Styling Guides, Seasonal Collections
        /// </summary>
        public async Task<IActionResult> Landing()
        {
            // Get latest blog posts (Fashion Tips)
            var latestPosts = await _blogService.GetPublishedPostsAsync(1, 6);
            var featuredPosts = await _blogService.GetFeaturedPostsAsync(3);

            // Get styling guides
            var stylingGuides = await _stylingGuideService.GetActiveGuidesAsync(1, 6);
            var featuredGuides = await _stylingGuideService.GetFeaturedGuidesAsync(3);

            // Get seasonal collections
            var collections = await _collectionService.GetCurrentCollectionsAsync();
            var featuredCollections = await _collectionService.GetFeaturedCollectionsAsync(3);

            ViewBag.LatestPosts = latestPosts;
            ViewBag.FeaturedPosts = featuredPosts;
            ViewBag.StylingGuides = stylingGuides;
            ViewBag.FeaturedGuides = featuredGuides;
            ViewBag.Collections = collections.Take(6).ToList();
            ViewBag.FeaturedCollections = featuredCollections;

            return View();
        }

        /// <summary>
        /// Blog listing page with pagination
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, int? categoryId = null, string? search = null)
        {
            const int pageSize = 9;

            List<BlogPost> posts;
            int totalCount;

            if (!string.IsNullOrWhiteSpace(search))
            {
                posts = await _blogService.SearchPostsAsync(search, 50);
                totalCount = posts.Count;
                posts = posts.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                ViewBag.SearchQuery = search;
            }
            else if (categoryId.HasValue)
            {
                posts = await _blogService.GetPostsByCategoryAsync(categoryId.Value, page, pageSize);
                totalCount = await _blogService.GetTotalPostCountAsync(categoryId);
                ViewBag.SelectedCategoryId = categoryId.Value;
            }
            else
            {
                posts = await _blogService.GetPublishedPostsAsync(page, pageSize);
                totalCount = await _blogService.GetTotalPostCountAsync();
            }

            var categories = await _blogService.GetActiveCategoriesAsync();
            var featuredPosts = await _blogService.GetFeaturedPostsAsync(3);

            ViewBag.Categories = categories;
            ViewBag.FeaturedPosts = featuredPosts;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(posts);
        }

        /// <summary>
        /// Single blog post details
        /// </summary>
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var post = await _blogService.GetPostBySlugAsync(slug);

            if (post == null || !post.IsPublished)
            {
                return NotFound();
            }

            // Increment view count
            await _blogService.IncrementViewCountAsync(post.Id);

            // Get related posts
            var relatedPosts = await _blogService.GetRelatedPostsAsync(post.Id, 4);
            var categories = await _blogService.GetActiveCategoriesAsync();

            ViewBag.RelatedPosts = relatedPosts;
            ViewBag.Categories = categories;

            return View(post);
        }

        /// <summary>
        /// Blog posts by category
        /// </summary>
        public async Task<IActionResult> Category(string slug, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return RedirectToAction(nameof(Index));
            }

            var category = await _blogService.GetCategoryBySlugAsync(slug);

            if (category == null)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index), new { categoryId = category.Id, page });
        }
    }
}
