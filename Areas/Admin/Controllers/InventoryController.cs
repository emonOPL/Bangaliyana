using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IStockAlertService _stockAlertService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public InventoryController(
            ApplicationDbContext db,
            IStockAlertService stockAlertService,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _stockAlertService = stockAlertService;
            _userManager = userManager;
            _localizer = localizer;
        }

        // GET: Admin/Inventory
        public async Task<IActionResult> Index(string? search, string? status, int? categoryId, int? sellerId, int page = 1)
        {
            var query = _db.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Where(p => p.Status == ProductStatus.Active)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) ||
                                        (p.SKU != null && p.SKU.Contains(search)));
            }

            // Status filter (stock status)
            if (!string.IsNullOrEmpty(status))
            {
                switch (status)
                {
                    case "outofstock":
                        query = query.Where(p => p.Stock <= 0);
                        break;
                    case "lowstock":
                        query = query.Where(p => p.Stock > 0 && p.Stock <= 5);
                        break;
                    case "instock":
                        query = query.Where(p => p.Stock > 5);
                        break;
                }
            }

            // Category filter
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            // Seller filter
            if (sellerId.HasValue)
            {
                query = query.Where(p => p.SellerId == sellerId);
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
            var stats = await _stockAlertService.GetInventoryStatsAsync();

            ViewBag.TotalProducts = stats.TotalProducts;
            ViewBag.InStockProducts = stats.InStockProducts;
            ViewBag.LowStockProducts = stats.LowStockProducts;
            ViewBag.OutOfStockProducts = stats.OutOfStockProducts;
            ViewBag.TotalStockUnits = stats.TotalStockUnits;
            ViewBag.TotalStockValue = stats.TotalStockValue;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CategoryId = categoryId;
            ViewBag.SellerId = sellerId;

            // Dropdowns
            ViewBag.Categories = new SelectList(
                await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name", categoryId);
            ViewBag.Sellers = new SelectList(
                await _db.Sellers.Where(s => s.Status == SellerStatus.Approved).OrderBy(s => s.ShopName).ToListAsync(),
                "Id", "ShopName", sellerId);

            return View(products);
        }

        // GET: Admin/Inventory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Get stock history
            var stockHistory = await _stockAlertService.GetStockHistoryAsync(product.Id, null, 30);
            ViewBag.StockHistory = stockHistory;

            // Get restock subscriber count
            ViewBag.RestockSubscribers = await _stockAlertService.GetSubscriberCountAsync(product.Id);

            return View(product);
        }

        // GET: Admin/Inventory/Adjust/5
        public async Task<IActionResult> Adjust(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Admin/Inventory/Adjust
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(int id, int? variantId, int newStock, string reason, string? notes)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            var success = await _stockAlertService.AdjustStockAsync(id, variantId, newStock, reason, notes, userId!);

            if (success)
            {
                TempData["AlertifySuccess"] = _localizer["StockAdjustedSuccessfully"].Value;
            }
            else
            {
                TempData["AlertifyError"] = _localizer["FailedToAdjustStock"].Value;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Inventory/History
        public async Task<IActionResult> History(int? productId, int page = 1)
        {
            var query = _db.StockHistories
                .Include(sh => sh.Product)
                .Include(sh => sh.ProductVariant)
                .Include(sh => sh.Order)
                .Include(sh => sh.ChangedBy)
                .AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(sh => sh.ProductId == productId);
                var product = await _db.Products.FindAsync(productId);
                ViewBag.ProductName = product?.Name;
            }

            // Pagination
            int pageSize = 30;
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

        // GET: Admin/Inventory/LowStock
        public async Task<IActionResult> LowStock()
        {
            var products = await _stockAlertService.GetLowStockProductsAsync();
            return View(products);
        }

        // GET: Admin/Inventory/OutOfStock
        public async Task<IActionResult> OutOfStock()
        {
            var products = await _stockAlertService.GetOutOfStockProductsAsync();
            return View(products);
        }

        // POST: Admin/Inventory/QuickUpdate
        [HttpPost]
        public async Task<IActionResult> QuickUpdate(int productId, int? variantId, int newStock)
        {
            var userId = _userManager.GetUserId(User);

            var success = await _stockAlertService.AdjustStockAsync(
                productId, variantId, newStock, "Quick update from inventory dashboard", null, userId!);

            if (success)
            {
                return Json(new { success = true, message = "Stock updated successfully" });
            }

            return Json(new { success = false, message = "Failed to update stock" });
        }

        // POST: Admin/Inventory/BulkUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdate(List<BulkStockUpdateItem> items)
        {
            var userId = _userManager.GetUserId(User);
            int updated = 0;

            foreach (var item in items)
            {
                var success = await _stockAlertService.AdjustStockAsync(
                    item.ProductId, item.VariantId, item.NewStock,
                    "Bulk stock update", item.Notes, userId!);

                if (success) updated++;
            }

            TempData["AlertifySuccess"] = _localizer["ProductsUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }
    }

    public class BulkStockUpdateItem
    {
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public int NewStock { get; set; }
        public string? Notes { get; set; }
    }
}
