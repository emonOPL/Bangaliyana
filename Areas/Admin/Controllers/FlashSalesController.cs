using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class FlashSalesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public FlashSalesController(ApplicationDbContext db, IWebHostEnvironment environment, IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _environment = environment;
            _localizer = localizer;
        }

        // GET: Admin/FlashSales
        public async Task<IActionResult> Index(string? status, int page = 1)
        {
            var query = _db.FlashSales
                .Include(f => f.Products)
                .AsQueryable();

            var now = DateTime.UtcNow;

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                switch (status)
                {
                    case "active":
                        query = query.Where(f => f.IsActive && f.StartDate <= now && f.EndDate >= now);
                        break;
                    case "upcoming":
                        query = query.Where(f => f.IsActive && f.StartDate > now);
                        break;
                    case "expired":
                        query = query.Where(f => f.EndDate < now);
                        break;
                    case "inactive":
                        query = query.Where(f => !f.IsActive);
                        break;
                }
            }

            // Pagination
            int pageSize = 15;
            var totalFlashSales = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalFlashSales / (double)pageSize);

            var flashSales = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Stats
            ViewBag.TotalFlashSales = await _db.FlashSales.CountAsync();
            ViewBag.ActiveFlashSales = await _db.FlashSales.CountAsync(f => f.IsActive && f.StartDate <= now && f.EndDate >= now);
            ViewBag.UpcomingFlashSales = await _db.FlashSales.CountAsync(f => f.IsActive && f.StartDate > now);
            ViewBag.ExpiredFlashSales = await _db.FlashSales.CountAsync(f => f.EndDate < now);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Status = status;

            return View(flashSales);
        }

        // GET: Admin/FlashSales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flashSale = await _db.FlashSales
                .Include(f => f.Products)
                    .ThenInclude(p => p.Product)
                        .ThenInclude(pr => pr.Images)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flashSale == null)
            {
                return NotFound();
            }

            return View(flashSale);
        }

        // GET: Admin/FlashSales/Create
        public IActionResult Create()
        {
            var flashSale = new FlashSale
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            return View(flashSale);
        }

        // POST: Admin/FlashSales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlashSale flashSale, IFormFile? bannerImage, IFormFile? mobileBannerImage)
        {
            // Validate dates
            if (flashSale.StartDate >= flashSale.EndDate)
            {
                ViewBag.onError = _localizer["EndDateMustBeAfterStartDate"].Value;
                return View(flashSale);
            }

            if (ModelState.IsValid)
            {
                if (bannerImage != null && bannerImage.Length > 0)
                {
                    flashSale.BannerImageUrl = await SaveFileAsync(bannerImage, "flash-sales");
                }

                if (mobileBannerImage != null && mobileBannerImage.Length > 0)
                {
                    flashSale.MobileBannerImageUrl = await SaveFileAsync(mobileBannerImage, "flash-sales");
                }

                flashSale.CreatedAt = DateTime.UtcNow;
                flashSale.UpdatedAt = DateTime.UtcNow;

                _db.FlashSales.Add(flashSale);
                await _db.SaveChangesAsync();

                TempData["onSave"] = _localizer["FlashSaleCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(ManageProducts), new { id = flashSale.Id });
            }

            return View(flashSale);
        }

        // GET: Admin/FlashSales/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flashSale = await _db.FlashSales.FindAsync(id);
            if (flashSale == null)
            {
                return NotFound();
            }

            return View(flashSale);
        }

        // POST: Admin/FlashSales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FlashSale flashSale, IFormFile? bannerImage, IFormFile? mobileBannerImage)
        {
            if (id != flashSale.Id)
            {
                return NotFound();
            }

            // Validate dates
            if (flashSale.StartDate >= flashSale.EndDate)
            {
                ViewBag.onError = _localizer["EndDateMustBeAfterStartDate"].Value;
                return View(flashSale);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingFlashSale = await _db.FlashSales.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);

                    if (bannerImage != null && bannerImage.Length > 0)
                    {
                        DeleteOldImage(existingFlashSale?.BannerImageUrl);
                        flashSale.BannerImageUrl = await SaveFileAsync(bannerImage, "flash-sales");
                    }
                    else
                    {
                        flashSale.BannerImageUrl = existingFlashSale?.BannerImageUrl;
                    }

                    if (mobileBannerImage != null && mobileBannerImage.Length > 0)
                    {
                        DeleteOldImage(existingFlashSale?.MobileBannerImageUrl);
                        flashSale.MobileBannerImageUrl = await SaveFileAsync(mobileBannerImage, "flash-sales");
                    }
                    else
                    {
                        flashSale.MobileBannerImageUrl = existingFlashSale?.MobileBannerImageUrl;
                    }

                    flashSale.CreatedAt = existingFlashSale?.CreatedAt ?? DateTime.UtcNow;
                    flashSale.UpdatedAt = DateTime.UtcNow;

                    _db.Update(flashSale);
                    await _db.SaveChangesAsync();

                    TempData["onEdit"] = _localizer["FlashSaleUpdatedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await FlashSaleExists(flashSale.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            return View(flashSale);
        }

        // GET: Admin/FlashSales/ManageProducts/5
        public async Task<IActionResult> ManageProducts(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flashSale = await _db.FlashSales
                .Include(f => f.Products)
                    .ThenInclude(p => p.Product)
                        .ThenInclude(pr => pr.Images)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flashSale == null)
            {
                return NotFound();
            }

            // Get available products (not already in this flash sale)
            var existingProductIds = flashSale.Products.Select(p => p.ProductId).ToList();
            var availableProducts = await _db.Products
                .Where(p => p.Status == ProductStatus.Active && !existingProductIds.Contains(p.Id))
                .Include(p => p.Images)
                .OrderBy(p => p.Name)
                .Take(100)
                .ToListAsync();

            ViewBag.AvailableProducts = availableProducts;

            return View(flashSale);
        }

        // POST: Admin/FlashSales/AddProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(int flashSaleId, int productId, decimal flashSalePrice, int discountPercentage, int? flashSaleStock)
        {
            var flashSale = await _db.FlashSales.FindAsync(flashSaleId);
            if (flashSale == null)
            {
                return NotFound();
            }

            var product = await _db.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            // Check if product already exists in flash sale
            var existingProduct = await _db.FlashSaleProducts
                .FirstOrDefaultAsync(fp => fp.FlashSaleId == flashSaleId && fp.ProductId == productId);

            if (existingProduct != null)
            {
                TempData["onError"] = _localizer["ProductAlreadyExistsInFlashSale"].Value;
                return RedirectToAction(nameof(ManageProducts), new { id = flashSaleId });
            }

            var flashSaleProduct = new FlashSaleProduct
            {
                FlashSaleId = flashSaleId,
                ProductId = productId,
                FlashSalePrice = flashSalePrice,
                DiscountPercentage = discountPercentage,
                FlashSaleStock = flashSaleStock,
                DisplayOrder = await _db.FlashSaleProducts.Where(fp => fp.FlashSaleId == flashSaleId).CountAsync(),
                CreatedAt = DateTime.UtcNow
            };

            _db.FlashSaleProducts.Add(flashSaleProduct);
            await _db.SaveChangesAsync();

            TempData["onSave"] = _localizer["ProductAddedToFlashSale"].Value;
            return RedirectToAction(nameof(ManageProducts), new { id = flashSaleId });
        }

        // POST: Admin/FlashSales/UpdateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(int id, decimal flashSalePrice, int discountPercentage, int? flashSaleStock)
        {
            var flashSaleProduct = await _db.FlashSaleProducts.FindAsync(id);
            if (flashSaleProduct == null)
            {
                return NotFound();
            }

            flashSaleProduct.FlashSalePrice = flashSalePrice;
            flashSaleProduct.DiscountPercentage = discountPercentage;
            flashSaleProduct.FlashSaleStock = flashSaleStock;

            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: Admin/FlashSales/RemoveProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int id)
        {
            var flashSaleProduct = await _db.FlashSaleProducts.FindAsync(id);
            if (flashSaleProduct == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            var flashSaleId = flashSaleProduct.FlashSaleId;
            _db.FlashSaleProducts.Remove(flashSaleProduct);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: Admin/FlashSales/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var flashSale = await _db.FlashSales.FindAsync(id);
            if (flashSale == null)
            {
                return NotFound();
            }

            DeleteOldImage(flashSale.BannerImageUrl);
            DeleteOldImage(flashSale.MobileBannerImageUrl);

            _db.FlashSales.Remove(flashSale);
            await _db.SaveChangesAsync();

            TempData["onDelete"] = _localizer["FlashSaleDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/FlashSales/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var flashSale = await _db.FlashSales.FindAsync(id);
            if (flashSale == null)
            {
                return Json(new { success = false, message = "Flash sale not found." });
            }

            flashSale.IsActive = !flashSale.IsActive;
            flashSale.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, isActive = flashSale.IsActive });
        }

        // GET: Admin/FlashSales/SearchProducts
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term, int flashSaleId)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            var existingProductIds = await _db.FlashSaleProducts
                .Where(fp => fp.FlashSaleId == flashSaleId)
                .Select(fp => fp.ProductId)
                .ToListAsync();

            var products = await _db.Products
                .Where(p => p.Status == ProductStatus.Active && !existingProductIds.Contains(p.Id) &&
                           (p.Name.Contains(term) || (p.SKU != null && p.SKU.Contains(term))))
                .Include(p => p.Images)
                .Take(10)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.SKU,
                    p.Price,
                    Image = p.Images.FirstOrDefault() != null ? p.Images.First().ImageUrl : null
                })
                .ToListAsync();

            return Json(products);
        }

        #region Helper Methods

        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/{folder}/{uniqueFileName}";
        }

        private void DeleteOldImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch { }
        }

        private async Task<bool> FlashSaleExists(int id)
        {
            return await _db.FlashSales.AnyAsync(f => f.Id == id);
        }

        #endregion
    }
}
