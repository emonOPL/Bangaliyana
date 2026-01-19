using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize]
    public class RegistrationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public RegistrationController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            INotificationService notificationService,
            IStringLocalizer<SharedResources> localizer)
        {
            _userManager = userManager;
            _context = context;
            _notificationService = notificationService;
            _localizer = localizer;
        }

        // GET: /Seller/Registration/Apply
        public async Task<IActionResult> Apply()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if already a seller
            if (await _userManager.IsInRoleAsync(user, "Seller"))
            {
                var existingSeller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                ViewBag.AlreadySeller = true;
                ViewBag.ExistingSeller = existingSeller;
                return View();
            }

            // Check for pending application
            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller != null)
            {
                ViewBag.HasPendingApplication = seller.Status == SellerStatus.Pending;
                ViewBag.ExistingSeller = seller;
                return View();
            }

            // Pre-fill phone from user profile
            var model = new SellerApplicationModel
            {
                BusinessPhone = user.PhoneNumber
            };

            return View(model);
        }

        // POST: /Seller/Registration/Apply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(SellerApplicationModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if already a seller
            if (await _userManager.IsInRoleAsync(user, "Seller"))
            {
                TempData["StatusMessage"] = _localizer["ErrorAlreadySeller"].Value;
                return RedirectToAction("Index", "Shop", new { area = "Seller" });
            }

            // Check for existing application
            var existingSeller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (existingSeller != null)
            {
                TempData["StatusMessage"] = existingSeller.Status == SellerStatus.Pending
                    ? _localizer["ErrorPendingSellerApplication"].Value
                    : _localizer["ErrorApplicationPreviouslyProcessed"].Value;
                return RedirectToAction(nameof(Apply));
            }

            if (!model.AcceptTerms)
            {
                ModelState.AddModelError("AcceptTerms", "You must accept the terms and conditions.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Generate unique slug
            var slug = GenerateSlug(model.ShopName);
            var slugBase = slug;
            var counter = 1;
            while (await _context.Sellers.AnyAsync(s => s.ShopSlug == slug))
            {
                slug = $"{slugBase}-{counter++}";
            }

            // Create seller application
            var seller = new Models.Seller
            {
                UserId = user.Id,
                ShopName = model.ShopName,
                ShopSlug = slug,
                BusinessCategory = model.BusinessCategory,
                BusinessAddress = model.BusinessAddress,
                BusinessPhone = model.BusinessPhone,
                BusinessEmail = user.Email,
                ApplicationNote = model.ApplicationNote,
                Status = SellerStatus.Pending,
                IsVerified = false,
                CommissionRate = 5m,
                AppliedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sellers.Add(seller);
            await _context.SaveChangesAsync();

            // Notify admin about new seller application
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = admin.Id,
                    Type = "admin",
                    Icon = "fa-user-plus",
                    IconColor = "text-info",
                    Title = "New Seller Application",
                    Message = $"{user.FullName ?? user.Email} has applied to become a seller with shop name '{model.ShopName}'.",
                    Link = "/Admin/SellerManagement/Details/" + seller.Id
                });
            }

            TempData["StatusMessage"] = _localizer["SellerApplicationSubmittedSuccess"].Value;
            return RedirectToAction(nameof(Apply));
        }

        private string GenerateSlug(string name)
        {
            return name.ToLower()
                .Replace(" ", "-")
                .Replace("@", "-")
                .Replace(".", "-")
                .Replace("_", "-")
                .Replace("--", "-")
                .Trim('-');
        }
    }

    public class SellerApplicationModel
    {
        [Required(ErrorMessage = "Shop name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Shop name must be between 3 and 200 characters")]
        [Display(Name = "Shop Name")]
        public string ShopName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Business category is required")]
        [Display(Name = "Business Category")]
        public BusinessCategory BusinessCategory { get; set; }

        [StringLength(500)]
        [Display(Name = "Business Address")]
        public string? BusinessAddress { get; set; }

        [Phone]
        [StringLength(20)]
        [Display(Name = "Business Phone")]
        public string? BusinessPhone { get; set; }

        [StringLength(1000)]
        [Display(Name = "Tell us about your business")]
        public string? ApplicationNote { get; set; }

        [Required(ErrorMessage = "You must accept the terms and conditions")]
        [Display(Name = "I accept the seller terms and conditions")]
        public bool AcceptTerms { get; set; }
    }
}
