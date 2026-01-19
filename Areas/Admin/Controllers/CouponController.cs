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
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public CouponController(ApplicationDbContext db, IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        // GET: Admin/Coupon
        public async Task<IActionResult> Index(string? search, string? status, string? type, int page = 1)
        {
            var query = _db.Coupons
                .Include(c => c.Seller)
                .Include(c => c.Category)
                .Include(c => c.AssignedToUser)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(c => c.Code.ToLower().Contains(search) ||
                                        (c.Description != null && c.Description.ToLower().Contains(search)));
            }

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.UtcNow;
                switch (status)
                {
                    case "active":
                        query = query.Where(c => c.IsActive &&
                            (c.StartDate == null || c.StartDate <= now) &&
                            (c.EndDate == null || c.EndDate >= now) &&
                            (c.UsageLimit == null || c.TimesUsed < c.UsageLimit));
                        break;
                    case "inactive":
                        query = query.Where(c => !c.IsActive);
                        break;
                    case "expired":
                        query = query.Where(c => c.EndDate != null && c.EndDate < now);
                        break;
                    case "scheduled":
                        query = query.Where(c => c.StartDate != null && c.StartDate > now);
                        break;
                }
            }

            // Discount type filter
            if (!string.IsNullOrEmpty(type))
            {
                if (Enum.TryParse<DiscountType>(type, out var discountType))
                {
                    query = query.Where(c => c.DiscountType == discountType);
                }
            }

            // Pagination
            int pageSize = 15;
            var totalCoupons = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCoupons / (double)pageSize);

            var coupons = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Stats
            var allCoupons = _db.Coupons.AsQueryable();
            var now2 = DateTime.UtcNow;
            ViewBag.TotalCoupons = await allCoupons.CountAsync();
            ViewBag.ActiveCoupons = await allCoupons.CountAsync(c => c.IsActive &&
                (c.StartDate == null || c.StartDate <= now2) &&
                (c.EndDate == null || c.EndDate >= now2) &&
                (c.UsageLimit == null || c.TimesUsed < c.UsageLimit));
            ViewBag.ExpiredCoupons = await allCoupons.CountAsync(c => c.EndDate != null && c.EndDate < now2);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Type = type;

            return View(coupons);
        }

        // GET: Admin/Coupon/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _db.Coupons
                .Include(c => c.Seller)
                .Include(c => c.Category)
                .Include(c => c.AssignedToUser)
                .Include(c => c.Usages)
                    .ThenInclude(u => u.User)
                .Include(c => c.Usages)
                    .ThenInclude(u => u.Order)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // GET: Admin/Coupon/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new Coupon());
        }

        // POST: Admin/Coupon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            // Check for duplicate code
            var existingCoupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToLower() == coupon.Code.ToLower());

            if (existingCoupon != null)
            {
                await PopulateDropdowns();
                ViewBag.onError = _localizer["CouponCodeAlreadyExists"].Value;
                return View(coupon);
            }

            // Validate dates
            if (coupon.StartDate.HasValue && coupon.EndDate.HasValue && coupon.StartDate > coupon.EndDate)
            {
                await PopulateDropdowns();
                ViewBag.onError = _localizer["EndDateMustBeAfterStartDate"].Value;
                return View(coupon);
            }

            // Validate percentage discount
            if (coupon.DiscountType == DiscountType.Percentage && coupon.DiscountValue > 100)
            {
                await PopulateDropdowns();
                ViewBag.onError = _localizer["PercentageDiscountCannotExceed100"].Value;
                return View(coupon);
            }

            if (ModelState.IsValid)
            {
                coupon.Code = coupon.Code.ToUpperInvariant();
                coupon.CreatedAt = DateTime.UtcNow;
                coupon.UpdatedAt = DateTime.UtcNow;

                _db.Coupons.Add(coupon);
                await _db.SaveChangesAsync();

                TempData["onSave"] = _localizer["CouponCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns();
            return View(coupon);
        }

        // GET: Admin/Coupon/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _db.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }

            await PopulateDropdowns();
            return View(coupon);
        }

        // POST: Admin/Coupon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Coupon coupon)
        {
            if (id != coupon.Id)
            {
                return NotFound();
            }

            // Check for duplicate code (excluding current)
            var existingCoupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToLower() == coupon.Code.ToLower() && c.Id != id);

            if (existingCoupon != null)
            {
                await PopulateDropdowns();
                ViewBag.onError = _localizer["CouponCodeAlreadyExists"].Value;
                return View(coupon);
            }

            // Validate dates
            if (coupon.StartDate.HasValue && coupon.EndDate.HasValue && coupon.StartDate > coupon.EndDate)
            {
                await PopulateDropdowns();
                ViewBag.onError = _localizer["EndDateMustBeAfterStartDate"].Value;
                return View(coupon);
            }

            // Validate percentage discount
            if (coupon.DiscountType == DiscountType.Percentage && coupon.DiscountValue > 100)
            {
                await PopulateDropdowns();
                ViewBag.onError = _localizer["PercentageDiscountCannotExceed100"].Value;
                return View(coupon);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCouponData = await _db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

                    coupon.Code = coupon.Code.ToUpperInvariant();
                    coupon.CreatedAt = existingCouponData?.CreatedAt ?? DateTime.UtcNow;
                    coupon.UpdatedAt = DateTime.UtcNow;
                    coupon.TimesUsed = existingCouponData?.TimesUsed ?? 0;

                    _db.Update(coupon);
                    await _db.SaveChangesAsync();

                    TempData["onEdit"] = _localizer["CouponUpdatedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await CouponExists(coupon.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            await PopulateDropdowns();
            return View(coupon);
        }

        // POST: Admin/Coupon/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _db.Coupons
                .Include(c => c.Usages)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (coupon == null)
            {
                return NotFound();
            }

            if (coupon.Usages.Any())
            {
                TempData["onError"] = _localizer["CannotDeleteUsedCoupon"].Value;
                return RedirectToAction(nameof(Index));
            }

            _db.Coupons.Remove(coupon);
            await _db.SaveChangesAsync();

            TempData["onDelete"] = _localizer["CouponDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Coupon/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var coupon = await _db.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return Json(new { success = false, message = _localizer["CouponNotFound"].Value });
            }

            coupon.IsActive = !coupon.IsActive;
            coupon.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, isActive = coupon.IsActive });
        }

        // GET: Admin/Coupon/GenerateCode
        [HttpGet]
        public IActionResult GenerateCode()
        {
            var code = GenerateUniqueCode();
            return Json(new { code });
        }

        #region Helper Methods

        private async Task PopulateDropdowns()
        {
            ViewBag.Categories = new SelectList(
                await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name");

            ViewBag.Sellers = new SelectList(
                await _db.Sellers.Where(s => s.Status == SellerStatus.Approved).OrderBy(s => s.ShopName).ToListAsync(),
                "Id", "ShopName");

            ViewBag.Users = new SelectList(
                await _db.Users.OrderBy(u => u.Email).Select(u => new { u.Id, Display = u.Email + " (" + u.FullName + ")" }).ToListAsync(),
                "Id", "Display");
        }

        private string GenerateUniqueCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;
            do
            {
                code = new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
            } while (_db.Coupons.Any(c => c.Code == code));

            return code;
        }

        private async Task<bool> CouponExists(int id)
        {
            return await _db.Coupons.AnyAsync(c => c.Id == id);
        }

        #endregion
    }
}
