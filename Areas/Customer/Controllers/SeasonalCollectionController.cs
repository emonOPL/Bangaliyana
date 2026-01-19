using Bangaliyana.Models;
using Bangaliyana.Services;
using Bangaliyana.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class SeasonalCollectionController : Controller
    {
        private readonly ISeasonalCollectionService _collectionService;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SeasonalCollectionController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SeasonalCollectionController(
            ISeasonalCollectionService collectionService,
            ISiteSettingsService siteSettingsService,
            ApplicationDbContext context,
            ILogger<SeasonalCollectionController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _collectionService = collectionService;
            _siteSettingsService = siteSettingsService;
            _context = context;
            _logger = logger;
            _localizer = localizer;
        }

        /// <summary>
        /// Seasonal collections listing page
        /// </summary>
        public async Task<IActionResult> Index(SeasonType? season = null)
        {
            List<SeasonalCollection> collections;

            if (season.HasValue)
            {
                collections = await _collectionService.GetCollectionsBySeasonAsync(season.Value, 20);
                ViewBag.SelectedSeason = season.Value;
            }
            else
            {
                collections = await _collectionService.GetCurrentCollectionsAsync();
            }

            var featuredCollections = await _collectionService.GetFeaturedCollectionsAsync(4);

            ViewBag.FeaturedCollections = featuredCollections;
            ViewBag.SeasonTypes = Enum.GetValues<SeasonType>();

            return View(collections);
        }

        /// <summary>
        /// Single collection details with products
        /// </summary>
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var collection = await _collectionService.GetCollectionBySlugAsync(slug);

            if (collection == null)
            {
                return NotFound();
            }

            // Get products for this collection
            var productIds = collection.ProductIdList;
            var categoryIds = collection.CategoryIdList;

            List<Products> products = new();

            if (productIds.Any())
            {
                products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .Where(p => productIds.Contains(p.Id) && p.IsAvailable)
                    .Take(20)
                    .ToListAsync();
            }
            else if (categoryIds.Any())
            {
                products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value) && p.IsAvailable)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(20)
                    .ToListAsync();
            }

            // Get other collections
            var otherCollections = await _collectionService.GetCurrentCollectionsAsync();
            otherCollections = otherCollections.Where(c => c.Id != collection.Id).Take(4).ToList();

            ViewBag.Products = products;
            ViewBag.OtherCollections = otherCollections;

            return View(collection);
        }

        /// <summary>
        /// Collections by season type
        /// </summary>
        public IActionResult Season(SeasonType season)
        {
            return RedirectToAction(nameof(Index), new { season });
        }
    }
}
