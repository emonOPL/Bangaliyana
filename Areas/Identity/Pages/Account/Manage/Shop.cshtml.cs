using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    [Authorize(Roles = "Seller,Admin")]
    public class ShopModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ShopModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public Models.Seller? Seller { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Shop name is required")]
            [StringLength(200, ErrorMessage = "Shop name cannot exceed 200 characters")]
            [Display(Name = "Shop Name")]
            public string ShopName { get; set; } = string.Empty;

            [StringLength(200)]
            [Display(Name = "Shop Slug (URL)")]
            [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
            public string? ShopSlug { get; set; }

            [StringLength(1000)]
            [Display(Name = "Shop Description")]
            public string? ShopDescription { get; set; }

            [Display(Name = "Business Category")]
            public BusinessCategory BusinessCategory { get; set; }

            [StringLength(500)]
            [Display(Name = "Business Address")]
            public string? BusinessAddress { get; set; }

            [Phone]
            [StringLength(20)]
            [Display(Name = "Business Phone")]
            public string? BusinessPhone { get; set; }

            [EmailAddress]
            [StringLength(100)]
            [Display(Name = "Business Email")]
            public string? BusinessEmail { get; set; }

            [StringLength(2000)]
            [Display(Name = "Shop Policies")]
            public string? ShopPolicies { get; set; }

            [StringLength(2000)]
            [Display(Name = "Return Policy")]
            public string? ReturnPolicy { get; set; }

            [StringLength(2000)]
            [Display(Name = "Shipping Information")]
            public string? ShippingInfo { get; set; }

            [StringLength(500)]
            [Display(Name = "Operating Hours")]
            public string? OperatingHours { get; set; }

            [Url]
            [StringLength(200)]
            [Display(Name = "Facebook URL")]
            public string? FacebookUrl { get; set; }

            [Url]
            [StringLength(200)]
            [Display(Name = "Instagram URL")]
            public string? InstagramUrl { get; set; }

            [StringLength(20)]
            [Display(Name = "WhatsApp Number")]
            public string? WhatsAppNumber { get; set; }

            [StringLength(100)]
            [Display(Name = "Trade License Number")]
            public string? TradeLicenseNumber { get; set; }

            [StringLength(100)]
            [Display(Name = "NID Number")]
            public string? NIDNumber { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            Seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (Seller == null)
            {
                // Create a new seller record if user has Seller role but no entity
                if (await _userManager.IsInRoleAsync(user, "Seller"))
                {
                    Seller = new Models.Seller
                    {
                        UserId = user.Id,
                        ShopName = user.FullName ?? user.Email ?? "My Shop",
                        ShopSlug = GenerateSlug(user.FullName ?? user.Email ?? "shop"),
                        ShopDescription = "Welcome to my shop!",
                        BusinessEmail = user.Email,
                        BusinessPhone = user.PhoneNumber,
                        BusinessCategory = BusinessCategory.Other,
                        Status = SellerStatus.Approved,
                        IsVerified = true,
                        CommissionRate = 5m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        ApprovedAt = DateTime.UtcNow
                    };

                    _context.Sellers.Add(Seller);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    return RedirectToPage("./Index");
                }
            }

            // Load stats
            ProductCount = await _context.Products.CountAsync(p => p.SellerId == Seller.Id);
            OrderCount = await _context.OrderItems
                .Where(oi => oi.SellerId == Seller.Id)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            // Populate input model
            Input = new InputModel
            {
                ShopName = Seller.ShopName,
                ShopSlug = Seller.ShopSlug,
                ShopDescription = Seller.ShopDescription,
                BusinessCategory = Seller.BusinessCategory,
                BusinessAddress = Seller.BusinessAddress,
                BusinessPhone = Seller.BusinessPhone,
                BusinessEmail = Seller.BusinessEmail,
                ShopPolicies = Seller.ShopPolicies,
                ReturnPolicy = Seller.ReturnPolicy,
                ShippingInfo = Seller.ShippingInfo,
                OperatingHours = Seller.OperatingHours,
                FacebookUrl = Seller.FacebookUrl,
                InstagramUrl = Seller.InstagramUrl,
                WhatsAppNumber = Seller.WhatsAppNumber,
                TradeLicenseNumber = Seller.TradeLicenseNumber,
                NIDNumber = Seller.NIDNumber
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            Seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (Seller == null)
            {
                return NotFound("Seller not found.");
            }

            if (!ModelState.IsValid)
            {
                ProductCount = await _context.Products.CountAsync(p => p.SellerId == Seller.Id);
                OrderCount = await _context.OrderItems
                    .Where(oi => oi.SellerId == Seller.Id)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();
                return Page();
            }

            // Check if slug is unique (if changed)
            var slug = string.IsNullOrWhiteSpace(Input.ShopSlug)
                ? GenerateSlug(Input.ShopName)
                : Input.ShopSlug.ToLower().Trim();

            var existingSlug = await _context.Sellers
                .AnyAsync(s => s.ShopSlug == slug && s.Id != Seller.Id);

            if (existingSlug)
            {
                ModelState.AddModelError("Input.ShopSlug", "This shop URL is already taken. Please choose another.");
                ProductCount = await _context.Products.CountAsync(p => p.SellerId == Seller.Id);
                OrderCount = await _context.OrderItems
                    .Where(oi => oi.SellerId == Seller.Id)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();
                return Page();
            }

            // Update seller
            Seller.ShopName = Input.ShopName;
            Seller.ShopSlug = slug;
            Seller.ShopDescription = Input.ShopDescription;
            Seller.BusinessCategory = Input.BusinessCategory;
            Seller.BusinessAddress = Input.BusinessAddress;
            Seller.BusinessPhone = Input.BusinessPhone;
            Seller.BusinessEmail = Input.BusinessEmail;
            Seller.ShopPolicies = Input.ShopPolicies;
            Seller.ReturnPolicy = Input.ReturnPolicy;
            Seller.ShippingInfo = Input.ShippingInfo;
            Seller.OperatingHours = Input.OperatingHours;
            Seller.FacebookUrl = Input.FacebookUrl;
            Seller.InstagramUrl = Input.InstagramUrl;
            Seller.WhatsAppNumber = Input.WhatsAppNumber;
            Seller.TradeLicenseNumber = Input.TradeLicenseNumber;
            Seller.NIDNumber = Input.NIDNumber;
            Seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            StatusMessage = "Your shop has been updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUploadLogoAsync(IFormFile logo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            Seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (Seller == null) return NotFound();

            if (logo == null || logo.Length == 0)
            {
                StatusMessage = "Error: Please select a valid image file.";
                return RedirectToPage();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                StatusMessage = "Error: Only image files (JPG, PNG, GIF, WebP) are allowed.";
                return RedirectToPage();
            }

            if (logo.Length > 2 * 1024 * 1024) // 2MB limit
            {
                StatusMessage = "Error: Image file size must be less than 2MB.";
                return RedirectToPage();
            }

            // Delete old logo if exists
            if (!string.IsNullOrEmpty(Seller.ShopLogoUrl))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, Seller.ShopLogoUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            // Save new logo
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "shops");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"logo-{Seller.Id}-{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logo.CopyToAsync(stream);
            }

            Seller.ShopLogoUrl = $"/images/shops/{uniqueFileName}";
            Seller.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            StatusMessage = "Shop logo has been updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUploadBannerAsync(IFormFile banner)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            Seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (Seller == null) return NotFound();

            if (banner == null || banner.Length == 0)
            {
                StatusMessage = "Error: Please select a valid image file.";
                return RedirectToPage();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(banner.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                StatusMessage = "Error: Only image files (JPG, PNG, GIF, WebP) are allowed.";
                return RedirectToPage();
            }

            if (banner.Length > 5 * 1024 * 1024) // 5MB limit for banners
            {
                StatusMessage = "Error: Banner file size must be less than 5MB.";
                return RedirectToPage();
            }

            // Delete old banner if exists
            if (!string.IsNullOrEmpty(Seller.ShopBannerUrl))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, Seller.ShopBannerUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            // Save new banner
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "shops");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"banner-{Seller.Id}-{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await banner.CopyToAsync(stream);
            }

            Seller.ShopBannerUrl = $"/images/shops/{uniqueFileName}";
            Seller.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            StatusMessage = "Shop banner has been updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveLogoAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            Seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (Seller == null) return NotFound();

            if (!string.IsNullOrEmpty(Seller.ShopLogoUrl))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, Seller.ShopLogoUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }

                Seller.ShopLogoUrl = null;
                Seller.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            StatusMessage = "Shop logo has been removed.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveBannerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            Seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (Seller == null) return NotFound();

            if (!string.IsNullOrEmpty(Seller.ShopBannerUrl))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, Seller.ShopBannerUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }

                Seller.ShopBannerUrl = null;
                Seller.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            StatusMessage = "Shop banner has been removed.";
            return RedirectToPage();
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
}
