using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class StylingGuideController : Controller
    {
        private readonly IStylingGuideService _stylingGuideService;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ILogger<StylingGuideController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public StylingGuideController(
            IStylingGuideService stylingGuideService,
            ISiteSettingsService siteSettingsService,
            ILogger<StylingGuideController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _stylingGuideService = stylingGuideService;
            _siteSettingsService = siteSettingsService;
            _logger = logger;
            _localizer = localizer;
        }

        /// <summary>
        /// Styling guides listing page
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, StylingGuideType? type = null, string? search = null)
        {
            const int pageSize = 12;

            List<StylingGuide> guides;
            int totalCount;

            if (!string.IsNullOrWhiteSpace(search))
            {
                guides = await _stylingGuideService.SearchGuidesAsync(search, 50);
                totalCount = guides.Count;
                guides = guides.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                ViewBag.SearchQuery = search;
            }
            else if (type.HasValue)
            {
                guides = await _stylingGuideService.GetGuidesByTypeAsync(type.Value, 50);
                totalCount = guides.Count;
                guides = guides.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                ViewBag.SelectedType = type.Value;
            }
            else
            {
                guides = await _stylingGuideService.GetActiveGuidesAsync(page, pageSize);
                totalCount = await _stylingGuideService.GetTotalGuideCountAsync();
            }

            var featuredGuides = await _stylingGuideService.GetFeaturedGuidesAsync(4);

            ViewBag.FeaturedGuides = featuredGuides;
            ViewBag.GuideTypes = Enum.GetValues<StylingGuideType>();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount = totalCount;

            return View(guides);
        }

        /// <summary>
        /// Single styling guide details
        /// </summary>
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var guide = await _stylingGuideService.GetGuideBySlugAsync(slug);

            if (guide == null)
            {
                return NotFound();
            }

            // Increment view count
            await _stylingGuideService.IncrementViewCountAsync(guide.Id);

            // Get more guides
            var moreGuides = await _stylingGuideService.GetGuidesByTypeAsync(guide.GuideType, 4);
            moreGuides = moreGuides.Where(g => g.Id != guide.Id).Take(3).ToList();

            ViewBag.MoreGuides = moreGuides;

            return View(guide);
        }

        /// <summary>
        /// Guides by type
        /// </summary>
        public IActionResult Type(StylingGuideType type, int page = 1)
        {
            return RedirectToAction(nameof(Index), new { type, page });
        }
    }
}
