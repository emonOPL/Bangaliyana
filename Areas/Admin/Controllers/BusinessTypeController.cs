using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class BusinessTypeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public BusinessTypeController(ApplicationDbContext db, IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        // GET: Admin/BusinessType
        public async Task<IActionResult> Index()
        {
            var businessTypes = await _db.BusinessTypes
                .Include(bt => bt.Sellers)
                .OrderBy(bt => bt.DisplayOrder)
                .ToListAsync();
            return View(businessTypes);
        }

        // GET: Admin/BusinessType/Create
        public IActionResult Create()
        {
            var model = new BusinessType
            {
                DisplayOrder = (_db.BusinessTypes.Max(bt => (int?)bt.DisplayOrder) ?? 0) + 1,
                IsActive = true
            };
            return View(model);
        }

        // POST: Admin/BusinessType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BusinessType model)
        {
            // Check for duplicate name
            var isExist = await _db.BusinessTypes.AnyAsync(bt => bt.Name.ToLower() == model.Name.ToLower());
            if (isExist)
            {
                ViewBag.Error = _localizer["BusinessTypeNameAlreadyExists"].Value;
                return View(model);
            }

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                _db.BusinessTypes.Add(model);
                await _db.SaveChangesAsync();
                TempData["Success"] = _localizer["BusinessTypeCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Admin/BusinessType/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var businessType = await _db.BusinessTypes.FindAsync(id);
            if (businessType == null)
            {
                return NotFound();
            }
            return View(businessType);
        }

        // POST: Admin/BusinessType/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BusinessType model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // Check for duplicate name (excluding current)
            var isExist = await _db.BusinessTypes.AnyAsync(bt => bt.Name.ToLower() == model.Name.ToLower() && bt.Id != id);
            if (isExist)
            {
                ViewBag.Error = _localizer["BusinessTypeNameAlreadyExists"].Value;
                return View(model);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingType = await _db.BusinessTypes.FindAsync(id);
                    if (existingType == null)
                    {
                        return NotFound();
                    }

                    existingType.Name = model.Name;
                    existingType.Description = model.Description;
                    existingType.IconClass = model.IconClass;
                    existingType.DisplayOrder = model.DisplayOrder;
                    existingType.IsActive = model.IsActive;
                    existingType.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync();
                    TempData["Success"] = _localizer["BusinessTypeUpdatedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await BusinessTypeExists(id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(model);
        }

        // POST: Admin/BusinessType/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var businessType = await _db.BusinessTypes
                .Include(bt => bt.Sellers)
                .FirstOrDefaultAsync(bt => bt.Id == id);

            if (businessType == null)
            {
                return NotFound();
            }

            // Check if any sellers are using this business type
            if (businessType.Sellers.Any())
            {
                TempData["Error"] = _localizer["CannotDeleteBusinessTypeInUse"].Value;
                return RedirectToAction(nameof(Index));
            }

            _db.BusinessTypes.Remove(businessType);
            await _db.SaveChangesAsync();
            TempData["Success"] = _localizer["BusinessTypeDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/BusinessType/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var businessType = await _db.BusinessTypes.FindAsync(id);
            if (businessType == null)
            {
                return Json(new { success = false, message = "Business type not found." });
            }

            businessType.IsActive = !businessType.IsActive;
            businessType.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, isActive = businessType.IsActive });
        }

        // POST: Admin/BusinessType/UpdateOrder
        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] int[] orderedIds)
        {
            if (orderedIds == null || orderedIds.Length == 0)
            {
                return Json(new { success = false, message = "Invalid order data." });
            }

            for (int i = 0; i < orderedIds.Length; i++)
            {
                var businessType = await _db.BusinessTypes.FindAsync(orderedIds[i]);
                if (businessType != null)
                {
                    businessType.DisplayOrder = i + 1;
                    businessType.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        private async Task<bool> BusinessTypeExists(int id)
        {
            return await _db.BusinessTypes.AnyAsync(bt => bt.Id == id);
        }
    }
}
