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
    public class PaymentMethodsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PaymentMethodsModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<SavedPaymentMethod> PaymentMethods { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public PaymentMethodInput Input { get; set; } = new();

        public class SavedPaymentMethod
        {
            public int Id { get; set; }
            public string Type { get; set; } = string.Empty;
            public string MaskedNumber { get; set; } = string.Empty;
            public string ProviderName { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string IconColor { get; set; } = string.Empty;
            public bool IsDefault { get; set; }
            public DateTime AddedAt { get; set; }
        }

        public class PaymentMethodInput
        {
            [Required]
            [Display(Name = "Payment Type")]
            public string Type { get; set; } = string.Empty;

            [Required]
            [StringLength(11, MinimumLength = 11, ErrorMessage = "Mobile number must be exactly 11 digits")]
            [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "Invalid mobile number")]
            [Display(Name = "Mobile Number")]
            public string MobileNumber { get; set; } = string.Empty;

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

            await LoadPaymentMethodsAsync(user.Id);
            return Page();
        }

        private async Task LoadPaymentMethodsAsync(string userId)
        {
            var methods = await _context.UserPaymentMethods
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.IsDefault)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            PaymentMethods = methods.Select(m => new SavedPaymentMethod
            {
                Id = m.Id,
                Type = m.ProviderType,
                MaskedNumber = m.MaskedNumber,
                ProviderName = m.ProviderName,
                Icon = m.Icon,
                IconColor = m.IconColor,
                IsDefault = m.IsDefault,
                AddedAt = m.CreatedAt
            }).ToList();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadPaymentMethodsAsync(user.Id);
                return Page();
            }

            // Check if this mobile number already exists for this provider
            var exists = await _context.UserPaymentMethods
                .AnyAsync(p => p.UserId == user.Id &&
                              p.ProviderType == Input.Type &&
                              p.MobileNumber == Input.MobileNumber);

            if (exists)
            {
                ModelState.AddModelError(string.Empty, "This payment method already exists.");
                await LoadPaymentMethodsAsync(user.Id);
                return Page();
            }

            // If this is set as default, unset other defaults
            if (Input.IsDefault)
            {
                var currentDefaults = await _context.UserPaymentMethods
                    .Where(p => p.UserId == user.Id && p.IsDefault)
                    .ToListAsync();

                foreach (var pm in currentDefaults)
                {
                    pm.IsDefault = false;
                }
            }

            // Create new payment method
            var paymentMethod = new UserPaymentMethod
            {
                UserId = user.Id,
                ProviderType = Input.Type,
                MobileNumber = Input.MobileNumber,
                IsDefault = Input.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserPaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            StatusMessage = "Payment method added successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var paymentMethod = await _context.UserPaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (paymentMethod != null)
            {
                _context.UserPaymentMethods.Remove(paymentMethod);
                await _context.SaveChangesAsync();
                StatusMessage = "Payment method removed.";
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

            var paymentMethods = await _context.UserPaymentMethods
                .Where(p => p.UserId == user.Id)
                .ToListAsync();

            foreach (var pm in paymentMethods)
            {
                pm.IsDefault = pm.Id == id;
                pm.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            StatusMessage = "Default payment method updated.";

            return RedirectToPage();
        }
    }
}
