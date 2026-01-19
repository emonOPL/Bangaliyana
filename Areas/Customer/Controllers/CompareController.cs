using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;

namespace Bangaliyana.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CompareController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICompareService _compareService;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public CompareController(
            ApplicationDbContext db,
            ICompareService compareService,
            ISiteSettingsService siteSettingsService,
            IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _compareService = compareService;
            _siteSettingsService = siteSettingsService;
            _localizer = localizer;
        }

        // GET: Customer/Compare
        public async Task<IActionResult> Index()
        {
            var compareProductIds = _compareService.GetCompareList();

            var products = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Variants)
                .Include(p => p.SizeGuide)
                .Where(p => compareProductIds.Contains(p.Id))
                .ToListAsync();

            // Maintain order from compare list
            var orderedProducts = compareProductIds
                .Select(id => products.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .ToList();

            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            ViewBag.CurrencySymbol = siteSettings?.CurrencySymbol ?? "৳";

            return View(orderedProducts);
        }

        // POST: Customer/Compare/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
            }

            if (_compareService.IsInCompare(productId))
            {
                return Json(new { success = false, message = _localizer["ProductAlreadyInCompareList"].Value, isInCompare = true });
            }

            var currentCount = _compareService.GetCompareCount();
            if (currentCount >= 4)
            {
                return Json(new { success = false, message = _localizer["Maximum4ProductsCanBeCompared"].Value, isInCompare = false });
            }

            var result = _compareService.AddToCompare(productId);
            if (result)
            {
                return Json(new {
                    success = true,
                    message = _localizer["AddedToCompareList"].Value,
                    isInCompare = true,
                    compareCount = _compareService.GetCompareCount()
                });
            }

            return Json(new { success = false, message = _localizer["FailedToAddToCompareList"].Value });
        }

        // POST: Customer/Compare/Remove
        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var result = _compareService.RemoveFromCompare(productId);
            if (result)
            {
                return Json(new {
                    success = true,
                    message = _localizer["RemovedFromCompareList"].Value,
                    isInCompare = false,
                    compareCount = _compareService.GetCompareCount()
                });
            }

            return Json(new { success = false, message = _localizer["ProductNotInCompareList"].Value });
        }

        // POST: Customer/Compare/Toggle
        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
            }

            bool isInCompare = _compareService.IsInCompare(productId);

            if (isInCompare)
            {
                _compareService.RemoveFromCompare(productId);
                return Json(new {
                    success = true,
                    message = _localizer["RemovedFromCompareList"].Value,
                    isInCompare = false,
                    compareCount = _compareService.GetCompareCount()
                });
            }
            else
            {
                var currentCount = _compareService.GetCompareCount();
                if (currentCount >= 4)
                {
                    return Json(new { success = false, message = _localizer["Maximum4ProductsCanBeCompared"].Value, isInCompare = false });
                }

                _compareService.AddToCompare(productId);
                return Json(new {
                    success = true,
                    message = _localizer["AddedToCompareList"].Value,
                    isInCompare = true,
                    compareCount = _compareService.GetCompareCount()
                });
            }
        }

        // POST: Customer/Compare/Clear
        [HttpPost]
        public IActionResult Clear()
        {
            _compareService.ClearCompare();
            return Json(new { success = true, message = _localizer["CompareListCleared"].Value, compareCount = 0 });
        }

        // GET: Customer/Compare/Check
        [HttpGet]
        public IActionResult Check(int productId)
        {
            var isInCompare = _compareService.IsInCompare(productId);
            return Json(new { isInCompare });
        }

        // GET: Customer/Compare/Count
        [HttpGet]
        public IActionResult Count()
        {
            var count = _compareService.GetCompareCount();
            return Json(new { count });
        }

        // GET: Customer/Compare/GetList
        [HttpGet]
        public IActionResult GetList()
        {
            var productIds = _compareService.GetCompareList();
            return Json(new { productIds, count = productIds.Count });
        }
    }
}
