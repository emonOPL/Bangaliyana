using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Bangaliyana.Data;
using Bangaliyana.Models;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public class SavedAddressesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public SavedAddressesModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<UserAddress> Addresses { get; set; } = new();
        public List<Division> Divisions { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public AddressInputModel Input { get; set; } = new();

        public class AddressInputModel
        {
            public int? Id { get; set; }

            [Required]
            [StringLength(100)]
            [Display(Name = "Address Label")]
            public string Label { get; set; } = string.Empty;

            [Display(Name = "Address Type")]
            public AddressType AddressType { get; set; } = AddressType.Home;

            [Required]
            [StringLength(100)]
            [Display(Name = "Recipient Name")]
            public string RecipientName { get; set; } = string.Empty;

            [Required]
            [StringLength(11, MinimumLength = 11, ErrorMessage = "Phone number must be exactly 11 digits")]
            [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "Phone must start with 013, 014, 015, 016, 017, 018, or 019")]
            [Display(Name = "Phone Number")]
            public string Phone { get; set; } = string.Empty;

            [Required(ErrorMessage = "Division is required")]
            [Display(Name = "Division")]
            public int? DivisionId { get; set; }

            [Required(ErrorMessage = "District is required")]
            [Display(Name = "District")]
            public int? DistrictId { get; set; }

            [Required(ErrorMessage = "Upazila is required")]
            [Display(Name = "Upazila")]
            public int? UpazilaId { get; set; }

            [Required]
            [StringLength(500)]
            [Display(Name = "Full Address")]
            public string FullAddress { get; set; } = string.Empty;

            [StringLength(10)]
            [RegularExpression(@"^\d*$", ErrorMessage = "Postal code must contain only digits")]
            [Display(Name = "Postal Code")]
            public string? PostalCode { get; set; }

            [Display(Name = "Set as Default")]
            public bool IsDefault { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadDataAsync(user.Id);
            return Page();
        }

        private async Task LoadDataAsync(string userId)
        {
            Addresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .Include(a => a.Division)
                .Include(a => a.District)
                .Include(a => a.Upazila)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            Divisions = await _context.Divisions.OrderBy(d => d.Name).ToListAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadDataAsync(user.Id);
                return Page();
            }

            UserAddress address;
            if (Input.Id.HasValue && Input.Id.Value > 0)
            {
                address = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.Id == Input.Id.Value && a.UserId == user.Id);

                if (address == null)
                {
                    return NotFound();
                }

                address.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                address = new UserAddress
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserAddresses.Add(address);
            }

            address.Label = Input.Label;
            address.AddressType = Input.AddressType;
            address.RecipientName = Input.RecipientName;
            address.Phone = Input.Phone;
            address.DivisionId = Input.DivisionId;
            address.DistrictId = Input.DistrictId;
            address.UpazilaId = Input.UpazilaId;
            address.FullAddress = Input.FullAddress;
            address.PostalCode = Input.PostalCode;
            address.UpdatedAt = DateTime.UtcNow;

            // Handle default address
            if (Input.IsDefault)
            {
                var currentDefault = await _context.UserAddresses
                    .Where(a => a.UserId == user.Id && a.IsDefault && a.Id != address.Id)
                    .ToListAsync();

                foreach (var addr in currentDefault)
                {
                    addr.IsDefault = false;
                }
            }
            address.IsDefault = Input.IsDefault;

            await _context.SaveChangesAsync();
            StatusMessage = Input.Id.HasValue ? "Address updated successfully." : "Address added successfully.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (address != null)
            {
                _context.UserAddresses.Remove(address);
                await _context.SaveChangesAsync();
                StatusMessage = "Address deleted successfully.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetDefaultAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            foreach (var addr in addresses)
            {
                addr.IsDefault = addr.Id == id;
            }

            await _context.SaveChangesAsync();
            StatusMessage = "Default address updated.";

            return RedirectToPage();
        }

        // API endpoints for cascading dropdowns
        public async Task<IActionResult> OnGetDistrictsAsync(int divisionId)
        {
            var districts = await _context.Districts
                .Where(d => d.DivisionId == divisionId)
                .OrderBy(d => d.Name)
                .Select(d => new { id = d.Id, name = d.Name })
                .ToListAsync();

            return new JsonResult(districts);
        }

        public async Task<IActionResult> OnGetUpazilasAsync(int districtId)
        {
            var upazilas = await _context.Upazilas
                .Where(u => u.DistrictId == districtId)
                .OrderBy(u => u.Name)
                .Select(u => new { id = u.Id, name = u.Name })
                .ToListAsync();

            return new JsonResult(upazilas);
        }
    }
}
