using Bangaliyana.Data;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISiteSettingsService _siteSettingsService;

        public AddressController(ApplicationDbContext context, ISiteSettingsService siteSettingsService)
        {
            _context = context;
            _siteSettingsService = siteSettingsService;
        }

        [HttpGet("divisions")]
        public async Task<IActionResult> GetDivisions()
        {
            var divisions = await _context.Divisions
                .OrderBy(d => d.Name)
                .Select(d => new { id = d.Id, name = d.Name })
                .ToListAsync();

            return Ok(divisions);
        }

        [HttpGet("districts/{divisionId}")]
        public async Task<IActionResult> GetDistricts(int divisionId)
        {
            var districts = await _context.Districts
                .Where(d => d.DivisionId == divisionId)
                .OrderBy(d => d.Name)
                .Select(d => new { id = d.Id, name = d.Name })
                .ToListAsync();

            return Ok(districts);
        }

        [HttpGet("upazilas/{districtId}")]
        public async Task<IActionResult> GetUpazilas(int districtId)
        {
            var upazilas = await _context.Upazilas
                .Where(u => u.DistrictId == districtId)
                .OrderBy(u => u.Name)
                .Select(u => new { id = u.Id, name = u.Name })
                .ToListAsync();

            return Ok(upazilas);
        }

        [HttpGet("delivery-charge/{divisionId}")]
        public IActionResult GetDeliveryChargeByDivision(int divisionId)
        {
            // Legacy endpoint - kept for backwards compatibility
            // Get division name to check if it's Rangpur or Dhaka
            var division = _context.Divisions.FirstOrDefault(d => d.Id == divisionId);
            if (division == null)
                return BadRequest("Invalid division");

            decimal deliveryCharge = 100m; // Default for other divisions

            if (division.Name.Equals("Rangpur", StringComparison.OrdinalIgnoreCase) ||
                division.Name.Equals("Dhaka", StringComparison.OrdinalIgnoreCase))
            {
                deliveryCharge = 50m;
            }

            return Ok(new { deliveryCharge, divisionName = division.Name });
        }

        // Districts that have 50 taka delivery charge
        private static readonly HashSet<string> ReducedDeliveryDistricts = new(StringComparer.OrdinalIgnoreCase)
        {
            "Dhaka", "Gazipur", "Narayanganj", "Rangpur", "Gaibandha"
        };

        [HttpGet("delivery-charge-by-district/{districtId}")]
        public async Task<IActionResult> GetDeliveryChargeByDistrict(int districtId)
        {
            // Get site settings for default delivery charge and free threshold
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            decimal defaultCharge = siteSettings?.DefaultDeliveryCharge ?? 100m;
            decimal? freeThreshold = siteSettings?.FreeDeliveryThreshold;

            // Get district info
            var district = await _context.Districts
                .Include(d => d.Division)
                .FirstOrDefaultAsync(d => d.Id == districtId);

            if (district == null)
                return BadRequest("Invalid district");

            // First check for district-specific charge in database
            var districtCharge = await _context.DistrictDeliveryCharges
                .FirstOrDefaultAsync(d => d.DistrictId == districtId && d.IsActive);

            decimal deliveryCharge;
            bool isCustomCharge = false;

            if (districtCharge != null)
            {
                // Use database entry if exists
                deliveryCharge = districtCharge.DeliveryCharge;
                isCustomCharge = true;
            }
            else if (ReducedDeliveryDistricts.Contains(district.Name))
            {
                // Legacy fallback: 50 taka for specific districts
                deliveryCharge = 50m;
            }
            else
            {
                // Use default charge from site settings
                deliveryCharge = defaultCharge;
            }

            return Ok(new {
                deliveryCharge,
                districtName = district.Name,
                divisionName = district.Division?.Name,
                isCustomCharge,
                freeDeliveryThreshold = freeThreshold
            });
        }
    }
}