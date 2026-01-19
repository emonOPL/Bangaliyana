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
    public class DeliveryChargeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public DeliveryChargeController(ApplicationDbContext context, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: Admin/DeliveryCharge
        public async Task<IActionResult> Index()
        {
            var siteSettings = await _context.SiteSettings.FirstOrDefaultAsync();
            var districtCharges = await _context.DistrictDeliveryCharges
                .Include(d => d.District)
                    .ThenInclude(d => d!.Division)
                .OrderBy(d => d.District!.Name)
                .ToListAsync();

            // Get all districts for adding new ones
            var existingDistrictIds = districtCharges.Select(d => d.DistrictId).ToList();
            var availableDistricts = await _context.Districts
                .Include(d => d.Division)
                .Where(d => !existingDistrictIds.Contains(d.Id))
                .OrderBy(d => d.Name)
                .ToListAsync();

            ViewBag.SiteSettings = siteSettings;
            ViewBag.AvailableDistricts = new SelectList(
                availableDistricts.Select(d => new { d.Id, Name = $"{d.Name} ({d.Division?.Name})" }),
                "Id", "Name");

            return View(districtCharges);
        }

        // POST: Admin/DeliveryCharge/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int districtId, decimal deliveryCharge)
        {
            if (districtId <= 0)
            {
                TempData["error"] = _localizer["PleaseSelectDistrict"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Check if already exists
            var exists = await _context.DistrictDeliveryCharges
                .AnyAsync(d => d.DistrictId == districtId);

            if (exists)
            {
                TempData["error"] = _localizer["DistrictAlreadyHasDeliveryCharge"].Value;
                return RedirectToAction(nameof(Index));
            }

            var charge = new DistrictDeliveryCharge
            {
                DistrictId = districtId,
                DeliveryCharge = deliveryCharge,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DistrictDeliveryCharges.Add(charge);
            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["DistrictDeliveryChargeAddedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/DeliveryCharge/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, decimal deliveryCharge)
        {
            var charge = await _context.DistrictDeliveryCharges.FindAsync(id);
            if (charge == null)
            {
                return Json(new { success = false, error = "Not found" });
            }

            charge.DeliveryCharge = deliveryCharge;
            charge.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: Admin/DeliveryCharge/ToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var charge = await _context.DistrictDeliveryCharges.FindAsync(id);
            if (charge == null)
            {
                return Json(new { success = false, error = "Not found" });
            }

            charge.IsActive = !charge.IsActive;
            charge.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = charge.IsActive });
        }

        // POST: Admin/DeliveryCharge/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var charge = await _context.DistrictDeliveryCharges.FindAsync(id);
            if (charge == null)
            {
                TempData["error"] = _localizer["NotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            _context.DistrictDeliveryCharges.Remove(charge);
            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["DistrictDeliveryChargeRemoved"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/DeliveryCharge/UpdateDefault
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDefault(decimal defaultCharge, decimal? freeThreshold)
        {
            var siteSettings = await _context.SiteSettings.FirstOrDefaultAsync();
            if (siteSettings == null)
            {
                TempData["error"] = _localizer["SiteSettingsNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            siteSettings.DefaultDeliveryCharge = defaultCharge;
            siteSettings.FreeDeliveryThreshold = freeThreshold;
            siteSettings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["DefaultDeliverySettingsUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/DeliveryCharge/GetDistrictsByDivision
        [HttpGet]
        public async Task<IActionResult> GetDistrictsByDivision(int districtId)
        {
            // Get the district to find its division
            var district = await _context.Districts
                .Include(d => d.Division)
                .FirstOrDefaultAsync(d => d.Id == districtId);

            if (district == null || district.DivisionId == null)
            {
                return Json(new { success = false, districts = new List<object>() });
            }

            // Get existing district IDs that already have delivery charges
            var existingDistrictIds = await _context.DistrictDeliveryCharges
                .Select(d => d.DistrictId)
                .ToListAsync();

            // Get all districts in the same division that don't have delivery charges yet
            var divisionDistricts = await _context.Districts
                .Where(d => d.DivisionId == district.DivisionId && !existingDistrictIds.Contains(d.Id))
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();

            return Json(new {
                success = true,
                divisionName = district.Division?.Name,
                districts = divisionDistricts
            });
        }

        // POST: Admin/DeliveryCharge/BulkAdd - Add multiple districts with same charge
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAdd(int[] districtIds, decimal deliveryCharge)
        {
            if (districtIds == null || districtIds.Length == 0)
            {
                TempData["error"] = _localizer["PleaseSelectAtLeastOneDistrict"].Value;
                return RedirectToAction(nameof(Index));
            }

            var existingIds = await _context.DistrictDeliveryCharges
                .Where(d => districtIds.Contains(d.DistrictId))
                .Select(d => d.DistrictId)
                .ToListAsync();

            var newIds = districtIds.Except(existingIds).ToList();

            if (!newIds.Any())
            {
                TempData["error"] = _localizer["AllDistrictsAlreadyHaveDeliveryCharges"].Value;
                return RedirectToAction(nameof(Index));
            }

            foreach (var districtId in newIds)
            {
                _context.DistrictDeliveryCharges.Add(new DistrictDeliveryCharge
                {
                    DistrictId = districtId,
                    DeliveryCharge = deliveryCharge,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["DistrictDeliveryChargesAddedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }
    }
}
