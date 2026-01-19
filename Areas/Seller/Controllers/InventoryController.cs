using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "SuperAdmin,Seller,Admin")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStockAlertService _stockAlertService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public InventoryController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IStockAlertService stockAlertService,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _stockAlertService = stockAlertService;
            _localizer = localizer;
        }

        private async Task<Bangaliyana.Models.Seller?> GetCurrentSellerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Status == SellerStatus.Approved);
        }

        // GET: Seller/Inventory
        public async Task<IActionResult> Index(string? search, string? status, int page = 1)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var query = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Where(p => p.SellerId == seller.Id && p.Status == ProductStatus.Active)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) ||
                                        (p.SKU != null && p.SKU.Contains(search)));
            }

            // Status filter
            var threshold = seller.LowStockThreshold > 0 ? seller.LowStockThreshold : 5;
            if (!string.IsNullOrEmpty(status))
            {
                switch (status)
                {
                    case "outofstock":
                        query = query.Where(p => p.Stock <= 0);
                        break;
                    case "lowstock":
                        query = query.Where(p => p.Stock > 0 && p.Stock <= threshold);
                        break;
                    case "instock":
                        query = query.Where(p => p.Stock > threshold);
                        break;
                }
            }

            // Pagination
            int pageSize = 20;
            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            var products = await query
                .OrderBy(p => p.Stock)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Stats
            var stats = await _stockAlertService.GetInventoryStatsAsync(seller.Id);

            ViewBag.TotalProducts = stats.TotalProducts;
            ViewBag.InStockProducts = stats.InStockProducts;
            ViewBag.LowStockProducts = stats.LowStockProducts;
            ViewBag.OutOfStockProducts = stats.OutOfStockProducts;
            ViewBag.TotalStockUnits = stats.TotalStockUnits;
            ViewBag.TotalStockValue = stats.TotalStockValue;
            ViewBag.LowStockThreshold = threshold;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Seller = seller;

            return View(products);
        }

        // GET: Seller/Inventory/Settings
        public async Task<IActionResult> Settings()
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            return View(seller);
        }

        // POST: Seller/Inventory/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(int lowStockThreshold, bool enableLowStockEmailAlerts, bool enableLowStockInAppAlerts)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            seller.LowStockThreshold = Math.Max(1, Math.Min(100, lowStockThreshold));
            seller.EnableLowStockEmailAlerts = enableLowStockEmailAlerts;
            seller.EnableLowStockInAppAlerts = enableLowStockInAppAlerts;

            await _context.SaveChangesAsync();

            TempData["AlertifySuccess"] = _localizer["InventoryAlertSettingsUpdatedSuccess"].Value;
            return RedirectToAction(nameof(Settings));
        }

        // POST: Seller/Inventory/QuickUpdate
        [HttpPost]
        public async Task<IActionResult> QuickUpdate(int productId, int? variantId, int newStock)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            // Verify product belongs to seller
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == seller.Id);

            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var userId = _userManager.GetUserId(User);
            var success = await _stockAlertService.AdjustStockAsync(
                productId, variantId, newStock,
                "Quick update from seller inventory",
                null, userId!);

            if (success)
            {
                return Json(new { success = true, message = "Stock updated successfully" });
            }

            return Json(new { success = false, message = "Failed to update stock" });
        }

        // GET: Seller/Inventory/History
        public async Task<IActionResult> History(int? productId, int page = 1)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var query = _context.StockHistories
                .Include(sh => sh.Product)
                .Include(sh => sh.ProductVariant)
                .Include(sh => sh.Order)
                .Include(sh => sh.ChangedBy)
                .Where(sh => sh.Product != null && sh.Product.SellerId == seller.Id)
                .AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(sh => sh.ProductId == productId);
                var product = await _context.Products.FindAsync(productId);
                ViewBag.ProductName = product?.Name;
            }

            // Pagination
            int pageSize = 25;
            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var history = await query
                .OrderByDescending(sh => sh.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.ProductId = productId;

            return View(history);
        }

        // GET: Seller/Inventory/Adjust/5
        public async Task<IActionResult> Adjust(int? id)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == seller.Id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Seller/Inventory/Adjust
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(int id, int? variantId, int newStock, string reason, string? notes)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == seller.Id);

            if (product == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var success = await _stockAlertService.AdjustStockAsync(id, variantId, newStock, reason, notes, userId!);

            if (success)
            {
                TempData["AlertifySuccess"] = _localizer["StockAdjustedSuccess"].Value;
            }
            else
            {
                TempData["AlertifyError"] = _localizer["FailedToAdjustStock"].Value;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
