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
    public class CategoryPromotionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public CategoryPromotionsController(ApplicationDbContext db, IWebHostEnvironment environment, IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _environment = environment;
            _localizer = localizer;
        }

        // GET: Admin/CategoryPromotions
        public async Task<IActionResult> Index(string? status, int? categoryId, int page = 1)
        {
            var query = _db.CategoryPromotions
                .Include(c => c.Category)
                .AsQueryable();

            var now = DateTime.UtcNow;

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                switch (status)
                {
                    case "active":
                        query = query.Where(c => c.IsActive &&
                            (c.StartDate == null || c.StartDate <= now) &&
                            (c.EndDate == null || c.EndDate >= now));
                        break;
                    case "upcoming":
                        query = query.Where(c => c.IsActive && c.StartDate != null && c.StartDate > now);
                        break;
                    case "expired":
                        query = query.Where(c => c.EndDate != null && c.EndDate < now);
                        break;
                    case "inactive":
                        query = query.Where(c => !c.IsActive);
                        break;
                }
            }

            // Category filter
            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            // Pagination
            int pageSize = 15;
            var totalPromotions = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalPromotions / (double)pageSize);

            var promotions = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Stats
            ViewBag.TotalPromotions = await _db.CategoryPromotions.CountAsync();
            ViewBag.ActivePromotions = await _db.CategoryPromotions.CountAsync(c => c.IsActive &&
                (c.StartDate == null || c.StartDate <= now) &&
                (c.EndDate == null || c.EndDate >= now));
            ViewBag.FeaturedPromotions = await _db.CategoryPromotions.CountAsync(c => c.IsFeatured);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Status = status;
            ViewBag.CategoryId = categoryId;

            await PopulateCategoriesDropdown();

            return View(promotions);
        }

        // GET: Admin/CategoryPromotions/Create
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesDropdown();
            PopulatePromotionTypeDropdown();
            return View(new CategoryPromotion());
        }

        // POST: Admin/CategoryPromotions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryPromotion promotion, IFormFile? bannerImage, IFormFile? mobileBannerImage, IFormFile? thumbnailImage)
        {
            // Validate dates
            if (promotion.StartDate.HasValue && promotion.EndDate.HasValue && promotion.StartDate > promotion.EndDate)
            {
                await PopulateCategoriesDropdown();
                PopulatePromotionTypeDropdown(promotion.PromotionType);
                ViewBag.onError = "End date must be after start date.";
                return View(promotion);
            }

            // Validate discount value for percentage type
            if (promotion.PromotionType == PromotionType.PercentageDiscount && promotion.DiscountValue > 100)
            {
                await PopulateCategoriesDropdown();
                PopulatePromotionTypeDropdown(promotion.PromotionType);
                ViewBag.onError = "Percentage discount cannot exceed 100%.";
                return View(promotion);
            }

            if (ModelState.IsValid)
            {
                if (bannerImage != null && bannerImage.Length > 0)
                {
                    promotion.BannerImageUrl = await SaveFileAsync(bannerImage, "category-promotions");
                }

                if (mobileBannerImage != null && mobileBannerImage.Length > 0)
                {
                    promotion.MobileBannerImageUrl = await SaveFileAsync(mobileBannerImage, "category-promotions");
                }

                if (thumbnailImage != null && thumbnailImage.Length > 0)
                {
                    promotion.ThumbnailImageUrl = await SaveFileAsync(thumbnailImage, "category-promotions");
                }

                promotion.CreatedAt = DateTime.UtcNow;
                promotion.UpdatedAt = DateTime.UtcNow;

                _db.CategoryPromotions.Add(promotion);
                await _db.SaveChangesAsync();

                TempData["success"] = _localizer["CategoryPromotionCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }

            await PopulateCategoriesDropdown();
            PopulatePromotionTypeDropdown(promotion.PromotionType);
            return View(promotion);
        }

        // GET: Admin/CategoryPromotions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _db.CategoryPromotions
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (promotion == null)
            {
                return NotFound();
            }

            await PopulateCategoriesDropdown();
            PopulatePromotionTypeDropdown(promotion.PromotionType);
            return View(promotion);
        }

        // POST: Admin/CategoryPromotions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryPromotion promotion, IFormFile? bannerImage, IFormFile? mobileBannerImage, IFormFile? thumbnailImage)
        {
            if (id != promotion.Id)
            {
                return NotFound();
            }

            // Validate dates
            if (promotion.StartDate.HasValue && promotion.EndDate.HasValue && promotion.StartDate > promotion.EndDate)
            {
                await PopulateCategoriesDropdown();
                PopulatePromotionTypeDropdown(promotion.PromotionType);
                ViewBag.onError = "End date must be after start date.";
                return View(promotion);
            }

            // Validate discount value for percentage type
            if (promotion.PromotionType == PromotionType.PercentageDiscount && promotion.DiscountValue > 100)
            {
                await PopulateCategoriesDropdown();
                PopulatePromotionTypeDropdown(promotion.PromotionType);
                ViewBag.onError = "Percentage discount cannot exceed 100%.";
                return View(promotion);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPromotion = await _db.CategoryPromotions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

                    if (bannerImage != null && bannerImage.Length > 0)
                    {
                        DeleteOldImage(existingPromotion?.BannerImageUrl);
                        promotion.BannerImageUrl = await SaveFileAsync(bannerImage, "category-promotions");
                    }
                    else
                    {
                        promotion.BannerImageUrl = existingPromotion?.BannerImageUrl;
                    }

                    if (mobileBannerImage != null && mobileBannerImage.Length > 0)
                    {
                        DeleteOldImage(existingPromotion?.MobileBannerImageUrl);
                        promotion.MobileBannerImageUrl = await SaveFileAsync(mobileBannerImage, "category-promotions");
                    }
                    else
                    {
                        promotion.MobileBannerImageUrl = existingPromotion?.MobileBannerImageUrl;
                    }

                    if (thumbnailImage != null && thumbnailImage.Length > 0)
                    {
                        DeleteOldImage(existingPromotion?.ThumbnailImageUrl);
                        promotion.ThumbnailImageUrl = await SaveFileAsync(thumbnailImage, "category-promotions");
                    }
                    else
                    {
                        promotion.ThumbnailImageUrl = existingPromotion?.ThumbnailImageUrl;
                    }

                    promotion.CreatedAt = existingPromotion?.CreatedAt ?? DateTime.UtcNow;
                    promotion.UpdatedAt = DateTime.UtcNow;

                    _db.Update(promotion);
                    await _db.SaveChangesAsync();

                    TempData["success"] = _localizer["CategoryPromotionUpdatedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await PromotionExists(promotion.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            await PopulateCategoriesDropdown();
            PopulatePromotionTypeDropdown(promotion.PromotionType);
            return View(promotion);
        }

        // POST: Admin/CategoryPromotions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var promotion = await _db.CategoryPromotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            DeleteOldImage(promotion.BannerImageUrl);
            DeleteOldImage(promotion.MobileBannerImageUrl);
            DeleteOldImage(promotion.ThumbnailImageUrl);

            _db.CategoryPromotions.Remove(promotion);
            await _db.SaveChangesAsync();

            TempData["success"] = _localizer["CategoryPromotionDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/CategoryPromotions/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var promotion = await _db.CategoryPromotions.FindAsync(id);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Promotion not found." });
            }

            promotion.IsActive = !promotion.IsActive;
            promotion.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, isActive = promotion.IsActive });
        }

        // POST: Admin/CategoryPromotions/ToggleFeatured/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            var promotion = await _db.CategoryPromotions.FindAsync(id);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Promotion not found." });
            }

            promotion.IsFeatured = !promotion.IsFeatured;
            promotion.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, isFeatured = promotion.IsFeatured });
        }

        #region Helper Methods

        private async Task PopulateCategoriesDropdown()
        {
            ViewBag.Categories = new SelectList(
                await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name");
        }

        private void PopulatePromotionTypeDropdown(PromotionType? selectedType = null)
        {
            ViewBag.PromotionTypes = Enum.GetValues<PromotionType>()
                .Select(t => new SelectListItem
                {
                    Value = ((int)t).ToString(),
                    Text = t switch
                    {
                        PromotionType.PercentageDiscount => "Percentage Discount",
                        PromotionType.FixedAmountDiscount => "Fixed Amount Discount",
                        PromotionType.BannerOnly => "Banner Only (No Discount)",
                        _ => t.ToString()
                    },
                    Selected = selectedType.HasValue && t == selectedType.Value
                })
                .ToList();
        }

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

        private async Task<bool> PromotionExists(int id)
        {
            return await _db.CategoryPromotions.AnyAsync(c => c.Id == id);
        }

        #endregion
    }
}
