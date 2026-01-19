using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "SuperAdmin,Seller,Admin")]
    public class ShopController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ShopController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IStringLocalizer<SharedResources> localizer)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found.");

            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (seller == null)
            {
                // Create a new seller record if user has Seller role but no entity
                if (await _userManager.IsInRoleAsync(user, "Seller"))
                {
                    seller = new Models.Seller
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

                    _context.Sellers.Add(seller);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    return RedirectToAction("Apply", "Registration", new { area = "Seller" });
                }
            }

            // Load stats
            var productCount = await _context.Products.CountAsync(p => p.SellerId == seller.Id);
            var orderCount = await _context.OrderItems
                .Where(oi => oi.SellerId == seller.Id)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            ViewBag.ProductCount = productCount;
            ViewBag.OrderCount = orderCount;

            return View(seller);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ShopInputModel input, IFormFile? Logo, IFormFile? Banner)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found.");

            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (seller == null) return NotFound("Seller not found.");

            if (!ModelState.IsValid)
            {
                ViewBag.ProductCount = await _context.Products.CountAsync(p => p.SellerId == seller.Id);
                ViewBag.OrderCount = await _context.OrderItems
                    .Where(oi => oi.SellerId == seller.Id)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();
                return View("Index", seller);
            }

            // Check if slug is unique (if changed)
            var slug = string.IsNullOrWhiteSpace(input.ShopSlug)
                ? GenerateSlug(input.ShopName)
                : input.ShopSlug.ToLower().Trim();

            var existingSlug = await _context.Sellers
                .AnyAsync(s => s.ShopSlug == slug && s.Id != seller.Id);

            if (existingSlug)
            {
                ModelState.AddModelError("ShopSlug", "This shop URL is already taken. Please choose another.");
                ViewBag.ProductCount = await _context.Products.CountAsync(p => p.SellerId == seller.Id);
                ViewBag.OrderCount = await _context.OrderItems
                    .Where(oi => oi.SellerId == seller.Id)
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .CountAsync();
                return View("Index", seller);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "shops");

            // Handle Logo upload
            if (Logo != null && Logo.Length > 0)
            {
                var logoExtension = Path.GetExtension(Logo.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(logoExtension))
                {
                    ModelState.AddModelError("", "Logo: Only image files (JPG, PNG, GIF, WebP) are allowed.");
                    ViewBag.ProductCount = await _context.Products.CountAsync(p => p.SellerId == seller.Id);
                    ViewBag.OrderCount = await _context.OrderItems
                        .Where(oi => oi.SellerId == seller.Id)
                        .Select(oi => oi.OrderId)
                        .Distinct()
                        .CountAsync();
                    return View("Index", seller);
                }

                if (Logo.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Logo: Image file size must be less than 2MB.");
                    ViewBag.ProductCount = await _context.Products.CountAsync(p => p.SellerId == seller.Id);
                    ViewBag.OrderCount = await _context.OrderItems
                        .Where(oi => oi.SellerId == seller.Id)
                        .Select(oi => oi.OrderId)
                        .Distinct()
                        .CountAsync();
                    return View("Index", seller);
                }

                // Delete old logo if exists
                if (!string.IsNullOrEmpty(seller.ShopLogoUrl))
                {
                    var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, seller.ShopLogoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var logoFileName = $"logo-{seller.Id}-{Guid.NewGuid()}{logoExtension}";
                var logoPath = Path.Combine(uploadsFolder, logoFileName);

                using (var stream = new FileStream(logoPath, FileMode.Create))
                {
                    await Logo.CopyToAsync(stream);
                }

                seller.ShopLogoUrl = $"/images/shops/{logoFileName}";
            }

            // Handle Banner upload
            if (Banner != null && Banner.Length > 0)
            {
                var bannerExtension = Path.GetExtension(Banner.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(bannerExtension))
                {
                    ModelState.AddModelError("", "Banner: Only image files (JPG, PNG, GIF, WebP) are allowed.");
                    ViewBag.ProductCount = await _context.Products.CountAsync(p => p.SellerId == seller.Id);
                    ViewBag.OrderCount = await _context.OrderItems
                        .Where(oi => oi.SellerId == seller.Id)
                        .Select(oi => oi.OrderId)
                        .Distinct()
                        .CountAsync();
                    return View("Index", seller);
                }

                if (Banner.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "Banner: File size must be less than 5MB.");
                    ViewBag.ProductCount = await _context.Products.CountAsync(p => p.SellerId == seller.Id);
                    ViewBag.OrderCount = await _context.OrderItems
                        .Where(oi => oi.SellerId == seller.Id)
                        .Select(oi => oi.OrderId)
                        .Distinct()
                        .CountAsync();
                    return View("Index", seller);
                }

                // Delete old banner if exists
                if (!string.IsNullOrEmpty(seller.ShopBannerUrl))
                {
                    var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, seller.ShopBannerUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var bannerFileName = $"banner-{seller.Id}-{Guid.NewGuid()}{bannerExtension}";
                var bannerPath = Path.Combine(uploadsFolder, bannerFileName);

                using (var stream = new FileStream(bannerPath, FileMode.Create))
                {
                    await Banner.CopyToAsync(stream);
                }

                seller.ShopBannerUrl = $"/images/shops/{bannerFileName}";
            }

            // Update seller
            seller.ShopName = input.ShopName;
            seller.ShopSlug = slug;
            seller.ShopDescription = input.ShopDescription;
            seller.BusinessCategory = input.BusinessCategory;
            seller.BusinessAddress = input.BusinessAddress;
            seller.BusinessPhone = input.BusinessPhone;
            seller.BusinessEmail = input.BusinessEmail;
            seller.ShopPolicies = input.ShopPolicies;
            seller.ReturnPolicy = input.ReturnPolicy;
            seller.ShippingInfo = input.ShippingInfo;
            seller.OperatingHours = input.OperatingHours;
            seller.FacebookUrl = input.FacebookUrl;
            seller.InstagramUrl = input.InstagramUrl;
            seller.WhatsAppNumber = input.WhatsAppNumber;
            seller.TradeLicenseNumber = input.TradeLicenseNumber;
            seller.NIDNumber = input.NIDNumber;
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = _localizer["ShopUpdatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLogo(IFormFile logo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                ? Json(new { success = false, message = "User not found" })
                : NotFound();

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null) return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                ? Json(new { success = false, message = "Seller not found" })
                : NotFound();

            if (logo == null || logo.Length == 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = _localizer["PleaseSelectValidImageFile"].Value });
                TempData["StatusMessage"] = _localizer["ErrorPleaseSelectValidImageFile"].Value;
                return RedirectToAction(nameof(Index));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = _localizer["OnlyImageFilesAllowed"].Value });
                TempData["StatusMessage"] = _localizer["ErrorOnlyImageFilesAllowed"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (logo.Length > 2 * 1024 * 1024) // 2MB limit
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = _localizer["ImageFileSizeMustBeLessThan2MB"].Value });
                TempData["StatusMessage"] = _localizer["ErrorImageFileSizeMustBeLessThan2MB"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Delete old logo if exists
            if (!string.IsNullOrEmpty(seller.ShopLogoUrl))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, seller.ShopLogoUrl.TrimStart('/'));
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

            var uniqueFileName = $"logo-{seller.Id}-{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logo.CopyToAsync(stream);
            }

            seller.ShopLogoUrl = $"/images/shops/{uniqueFileName}";
            seller.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = _localizer["ShopLogoUploadedSuccess"].Value, imageUrl = seller.ShopLogoUrl });

            TempData["StatusMessage"] = _localizer["ShopLogoUpdatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadBanner(IFormFile banner)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                ? Json(new { success = false, message = "User not found" })
                : NotFound();

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null) return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                ? Json(new { success = false, message = "Seller not found" })
                : NotFound();

            if (banner == null || banner.Length == 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = _localizer["PleaseSelectValidImageFile"].Value });
                TempData["StatusMessage"] = _localizer["ErrorPleaseSelectValidImageFile"].Value;
                return RedirectToAction(nameof(Index));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(banner.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = _localizer["OnlyImageFilesAllowed"].Value });
                TempData["StatusMessage"] = _localizer["ErrorOnlyImageFilesAllowed"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (banner.Length > 5 * 1024 * 1024) // 5MB limit for banners
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = _localizer["BannerFileSizeMustBeLessThan5MB"].Value });
                TempData["StatusMessage"] = _localizer["ErrorBannerFileSizeMustBeLessThan5MB"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Delete old banner if exists
            if (!string.IsNullOrEmpty(seller.ShopBannerUrl))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, seller.ShopBannerUrl.TrimStart('/'));
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

            var uniqueFileName = $"banner-{seller.Id}-{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await banner.CopyToAsync(stream);
            }

            seller.ShopBannerUrl = $"/images/shops/{uniqueFileName}";
            seller.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = _localizer["ShopBannerUploadedSuccess"].Value, imageUrl = seller.ShopBannerUrl });

            TempData["StatusMessage"] = _localizer["ShopBannerUpdatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogo()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null) return NotFound();

            if (!string.IsNullOrEmpty(seller.ShopLogoUrl))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, seller.ShopLogoUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }

                seller.ShopLogoUrl = null;
                seller.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            TempData["StatusMessage"] = _localizer["ShopLogoRemoved"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBanner()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null) return NotFound();

            if (!string.IsNullOrEmpty(seller.ShopBannerUrl))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, seller.ShopBannerUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }

                seller.ShopBannerUrl = null;
                seller.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            TempData["StatusMessage"] = _localizer["ShopBannerRemoved"].Value;
            return RedirectToAction(nameof(Index));
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

    public class ShopInputModel
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

        [Required(ErrorMessage = "Business category is required")]
        [Display(Name = "Business Category")]
        public BusinessCategory BusinessCategory { get; set; }

        [StringLength(500)]
        [Display(Name = "Business Address")]
        public string? BusinessAddress { get; set; }

        [Required(ErrorMessage = "Business phone is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20)]
        [Display(Name = "Business Phone")]
        public string BusinessPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Business email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        [Display(Name = "Business Email")]
        public string BusinessEmail { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "NID/Birth Certificate number is required")]
        [StringLength(100)]
        [Display(Name = "NID/Birth Certificate Number")]
        public string NIDNumber { get; set; } = string.Empty;
    }
}
