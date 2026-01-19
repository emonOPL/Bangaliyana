using Bangaliyana.Data;
using Bangaliyana.Extensions;
using Bangaliyana.Models;
using Bangaliyana.Models.ViewModels;
using Bangaliyana.Services;
using Bangaliyana.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using X.PagedList.Extensions;
using System.Text;

namespace Bangaliyana.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ICartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ISearchService _searchService;
        private readonly IRewardService _rewardService;
        private readonly IShopRatingService _shopRatingService;
        private readonly IShopFollowService _shopFollowService;
        private readonly IStockAlertService _stockAlertService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly PdfGeneratorService _pdfGenerator;

        public HomeController(
            ApplicationDbContext db,
            ILogger<HomeController> logger,
            IEmailService emailService,
            IConfiguration config,
            ICartService cartService,
            UserManager<ApplicationUser> userManager,
            ISiteSettingsService siteSettingsService,
            ISearchService searchService,
            IRewardService rewardService,
            IShopRatingService shopRatingService,
            IShopFollowService shopFollowService,
            IStockAlertService stockAlertService,
            IStringLocalizer<SharedResources> localizer,
            PdfGeneratorService pdfGenerator)
        {
            _db = db;
            _logger = logger;
            _emailService = emailService;
            _config = config;
            _cartService = cartService;
            _userManager = userManager;
            _siteSettingsService = siteSettingsService;
            _searchService = searchService;
            _rewardService = rewardService;
            _shopRatingService = shopRatingService;
            _shopFollowService = shopFollowService;
            _stockAlertService = stockAlertService;
            _localizer = localizer;
            _pdfGenerator = pdfGenerator;
        }

        // Get current user ID
        private string? GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return _userManager.GetUserId(User);
            }
            return null;
        }

        // Get delivery charge based on district (fully database-driven)
        private async Task<decimal> GetDeliveryChargeAsync(int? districtId, decimal? subtotal = null)
        {
            // Get site settings for default delivery charge and free threshold
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            decimal defaultCharge = siteSettings?.DefaultDeliveryCharge ?? 100m;
            decimal? freeThreshold = siteSettings?.FreeDeliveryThreshold;

            // Check if order qualifies for free delivery based on threshold
            if (freeThreshold.HasValue && freeThreshold.Value > 0 && subtotal.HasValue && subtotal.Value >= freeThreshold.Value)
            {
                return 0m; // Free delivery for orders above threshold
            }

            if (!districtId.HasValue)
            {
                return defaultCharge; // Default charge when no district selected
            }

            // Check for district-specific charge in database
            var districtCharge = await _db.DistrictDeliveryCharges
                .FirstOrDefaultAsync(d => d.DistrictId == districtId.Value && d.IsActive);

            if (districtCharge != null)
            {
                return districtCharge.DeliveryCharge;
            }

            return defaultCharge; // Default charge for districts without specific pricing
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int? page)
        {
            var userId = GetCurrentUserId();
            var pageNumber = page ?? 1;
            var pageSize = 12;

            // Get site settings
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();

            // Get all products with includes (excluding suspended seller products)
            var allProductsQuery = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Seller)
                .Where(p => p.IsAvailable && p.Status == ProductStatus.Active
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved));

            // Build the view model
            var viewModel = new HomePageViewModel
            {
                SiteSettings = siteSettings,

                // Hero Banners (for slider)
                HeroBanners = await _siteSettingsService.GetActiveBannersByLocationAsync(BannerLocation.Hero),

                // Categories - Get root categories with children included
                Categories = await _db.Categories
                    .Include(c => c.Children)
                    .Where(c => c.IsActive && c.ParentId == null)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .Take(8)
                    .ToListAsync(),

                FeaturedCategories = await _db.Categories
                    .Include(c => c.Children)
                    .Where(c => c.IsActive && c.IsFeatured)
                    .OrderBy(c => c.DisplayOrder)
                    .Take(4)
                    .ToListAsync(),

                // All Products (paginated)
                AllProducts = allProductsQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList()
                    .ToPagedList(pageNumber, pageSize),

                // Trending Products (most viewed)
                TrendingProducts = await allProductsQuery
                    .OrderByDescending(p => p.ViewCount)
                    .Take(8)
                    .ToListAsync(),

                // Best Selling Products
                BestSellingProducts = await allProductsQuery
                    .OrderByDescending(p => p.SoldCount)
                    .Take(8)
                    .ToListAsync(),

                // New Arrivals
                NewArrivals = await allProductsQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync(),

                // Featured Products
                FeaturedProducts = await allProductsQuery
                    .Where(p => p.IsFeatured)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync(),

                // Flash Sale Products (products with discount)
                FlashSaleProducts = await allProductsQuery
                    .Where(p => p.DiscountPrice.HasValue && p.DiscountPrice < p.Price)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync(),

                // Promotional Banners
                PromotionalBanners = await _siteSettingsService.GetActiveBannersByLocationAsync(BannerLocation.HomeMiddle),

                // Offer Banners
                OfferBanners = await _siteSettingsService.GetActiveBannersByLocationAsync(BannerLocation.HomeBottom),

                // Home Features
                HomeFeatures = await _siteSettingsService.GetActiveFeaturesBySectionAsync(FeatureSection.HomePage),

                // Testimonials
                Testimonials = await _siteSettingsService.GetFeaturedTestimonialsAsync(),

                // Flash Sale End Time (next midnight for demo)
                FlashSaleEndTime = DateTime.Today.AddDays(1)
            };

            // Get user's wishlist product IDs for displaying filled heart icons
            var wishlistProductIds = new HashSet<int>();
            if (!string.IsNullOrEmpty(userId))
            {
                wishlistProductIds = (await _db.Wishlists
                    .Where(w => w.UserId == userId)
                    .Select(w => w.ProductId)
                    .ToListAsync())
                    .ToHashSet();
            }
            ViewBag.WishlistProductIds = wishlistProductIds;

            // Personalized Products (for logged-in users, based on their order history)
            if (!string.IsNullOrEmpty(userId))
            {
                var orderedProductIds = await _db.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.UserId == userId)
                    .Select(oi => oi.Product != null ? oi.Product.CategoryId : null)
                    .Where(cid => cid.HasValue)
                    .Distinct()
                    .ToListAsync();

                if (orderedProductIds.Any())
                {
                    viewModel.PersonalizedProducts = await allProductsQuery
                        .Where(p => p.CategoryId.HasValue && orderedProductIds.Contains(p.CategoryId))
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(8)
                        .ToListAsync();
                }
                else
                {
                    // If no order history, show featured products
                    viewModel.PersonalizedProducts = viewModel.FeaturedProducts;
                }
            }
            else
            {
                // For guests, show trending products
                viewModel.PersonalizedProducts = viewModel.TrendingProducts;
            }

            return View(viewModel);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubscribeNewsletter(string email, string? name)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Please enter a valid email address." });
            }

            try
            {
                // Check if already subscribed
                var existingSubscription = await _db.NewsletterSubscriptions
                    .FirstOrDefaultAsync(n => n.Email.ToLower() == email.ToLower());

                if (existingSubscription != null)
                {
                    if (existingSubscription.IsActive)
                    {
                        return Json(new { success = false, message = "You are already subscribed to our newsletter." });
                    }
                    else
                    {
                        // Reactivate subscription
                        existingSubscription.IsActive = true;
                        existingSubscription.UnsubscribedAt = null;
                        existingSubscription.SubscribedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                        return Json(new { success = true, message = "Welcome back! Your subscription has been reactivated." });
                    }
                }

                // Create new subscription
                var subscription = new NewsletterSubscription
                {
                    Email = email,
                    Name = name,
                    Source = "Homepage",
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _db.NewsletterSubscriptions.Add(subscription);
                await _db.SaveChangesAsync();

                // Send welcome email with dynamic site name
                try
                {
                    var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
                    var siteName = siteSettings?.SiteName ?? "our shop";
                    var emailBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <h2 style='color: #4F46E5;'>Thank you for subscribing to {siteName}!</h2>
                            <p>Welcome to our newsletter community. You'll now receive updates about:</p>
                            <ul>
                                <li>Latest products and arrivals</li>
                                <li>Exclusive offers and discounts</li>
                                <li>Special promotions and news</li>
                            </ul>
                            <p>We're excited to have you with us!</p>
                            <p style='color: #666;'>- The {siteName} Team</p>
                        </div>";
                    await _emailService.SendEmailAsync(email,
                        $"Welcome to {siteName} Newsletter!",
                        emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send newsletter welcome email to {Email}", email);
                }

                return Json(new { success = true, message = "Thank you for subscribing! Check your email for confirmation." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to newsletter: {Email}", email);
                return Json(new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        [AllowAnonymous]
        public IActionResult Tag(int id, int? page)
        {
            // SpecialTags feature has been removed - redirect to home
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public async Task<IActionResult> Categories()
        {
            // Get all active categories with their hierarchy
            var allCategories = await _db.Categories
                .Include(c => c.Children)
                .Include(c => c.Products)
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Get only root categories for the tree view
            var rootCategories = allCategories.Where(c => c.ParentId == null).ToList();

            ViewBag.AllCategories = allCategories;
            return View(rootCategories);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Category(int id, int? page, string? sort, decimal? minPrice, decimal? maxPrice,
            string? size, string? color, string? brand, int? rating, string? view)
        {
            var category = await _db.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children.Where(ch => ch.IsActive))
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            if (category == null)
            {
                return NotFound();
            }

            // Build category breadcrumb path
            var categoryPath = new List<Category>();
            var currentCat = category;
            while (currentCat != null)
            {
                categoryPath.Insert(0, currentCat);
                currentCat = currentCat.ParentId.HasValue
                    ? await _db.Categories.FindAsync(currentCat.ParentId)
                    : null;
            }
            ViewBag.CategoryPath = categoryPath;

            // Get all descendant category IDs for including products from subcategories
            var categoryIds = await GetDescendantCategoryIds(id);
            categoryIds.Add(id);

            var pageNumber = page ?? 1;
            var pageSize = 12;

            var productsQuery = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .Include(p => p.Seller)
                .Where(p => categoryIds.Contains(p.CategoryId ?? 0) && p.IsAvailable && p.Status == ProductStatus.Active
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved));

            // Price filtering
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
            }

            // Size filtering (from ProductVariants)
            if (!string.IsNullOrEmpty(size))
            {
                productsQuery = productsQuery.Where(p => p.Variants.Any(v => v.Size == size && v.IsAvailable));
            }

            // Color filtering (from ProductVariants)
            if (!string.IsNullOrEmpty(color))
            {
                productsQuery = productsQuery.Where(p => p.Variants.Any(v => v.Color == color && v.IsAvailable));
            }

            // Brand filtering
            if (!string.IsNullOrEmpty(brand))
            {
                productsQuery = productsQuery.Where(p => p.Brand == brand);
            }

            // Rating filtering
            if (rating.HasValue && rating.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= rating.Value);
            }

            // Sorting
            productsQuery = sort switch
            {
                "price_asc" => productsQuery.OrderBy(p => p.DiscountPrice ?? p.Price),
                "price_desc" => productsQuery.OrderByDescending(p => p.DiscountPrice ?? p.Price),
                "name_asc" => productsQuery.OrderBy(p => p.Name),
                "name_desc" => productsQuery.OrderByDescending(p => p.Name),
                "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
                "bestselling" => productsQuery.OrderByDescending(p => p.SoldCount),
                "trending" => productsQuery.OrderByDescending(p => p.ViewCount),
                _ => productsQuery.OrderByDescending(p => p.CreatedAt)
            };

            var products = productsQuery.ToList().ToPagedList(pageNumber, pageSize);

            // Get site settings for currency symbol
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();

            // Get available filter options for this category (including subcategories)
            var categoryProducts = await _db.Products
                .Include(p => p.Variants)
                .Include(p => p.Seller)
                .Where(p => categoryIds.Contains(p.CategoryId ?? 0) && p.IsAvailable && p.Status == ProductStatus.Active
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved))
                .ToListAsync();

            var availableSizes = categoryProducts
                .SelectMany(p => p.Variants)
                .Where(v => !string.IsNullOrEmpty(v.Size) && v.IsAvailable)
                .Select(v => v.Size!)
                .Distinct()
                .OrderBy(s => s == "S" ? 0 : s == "M" ? 1 : s == "L" ? 2 : s == "XL" ? 3 : s == "XXL" ? 4 : 5)
                .ToList();

            var availableColors = categoryProducts
                .SelectMany(p => p.Variants)
                .Where(v => !string.IsNullOrEmpty(v.Color) && v.IsAvailable)
                .Select(v => new { Color = v.Color!, ColorCode = v.ColorCode })
                .DistinctBy(v => v.Color)
                .ToList();

            var availableBrands = categoryProducts
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            // Price range for slider
            var priceMin = categoryProducts.Any() ? categoryProducts.Min(p => p.DiscountPrice ?? p.Price) : 0;
            var priceMax = categoryProducts.Any() ? categoryProducts.Max(p => p.DiscountPrice ?? p.Price) : 10000;

            ViewBag.Category = category;
            ViewBag.CategoryId = id;
            ViewBag.CurrentSort = sort;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SelectedSize = size;
            ViewBag.SelectedColor = color;
            ViewBag.SelectedBrand = brand;
            ViewBag.SelectedRating = rating;
            ViewBag.CurrentView = view ?? "grid";
            ViewBag.AvailableSizes = availableSizes;
            ViewBag.AvailableColors = availableColors;
            ViewBag.AvailableBrands = availableBrands;
            ViewBag.PriceMin = priceMin;
            ViewBag.PriceMax = priceMax;
            ViewBag.CurrencySymbol = siteSettings.CurrencySymbol ?? "৳";
            ViewBag.Subcategories = category.Children?.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList() ?? new List<Category>();
            ViewBag.TotalProducts = await _db.Products
                .Include(p => p.Seller)
                .CountAsync(p => categoryIds.Contains(p.CategoryId ?? 0) && p.IsAvailable && p.Status == ProductStatus.Active
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved));

            return View(products);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(p => p.Variants.Where(v => v.IsAvailable).OrderBy(v => v.DisplayOrder))
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                    .ThenInclude(r => r.User)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Images)
                .Include(p => p.Questions.Where(q => q.IsApproved && q.IsAnswered))
                    .ThenInclude(q => q.User)
                .Include(p => p.SizeGuide)
                    .ThenInclude(sg => sg!.Entries.OrderBy(e => e.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Block access to suspended seller products
            if (product.Seller != null && product.Seller.Status == SellerStatus.Suspended)
            {
                return NotFound();
            }

            // Increment view count
            product.ViewCount++;
            await _db.SaveChangesAsync();

            // Get site settings for currency
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            ViewBag.CurrencySymbol = siteSettings.CurrencySymbol ?? "৳";

            // Calculate average rating
            var approvedReviews = product.Reviews.Where(r => r.IsApproved).ToList();
            ViewBag.AverageRating = approvedReviews.Any() ? approvedReviews.Average(r => r.Rating) : 0;
            ViewBag.ReviewCount = approvedReviews.Count;
            ViewBag.QuestionCount = product.Questions.Count(q => q.IsApproved && q.IsAnswered);

            // Rating distribution
            ViewBag.RatingDistribution = Enumerable.Range(1, 5)
                .ToDictionary(
                    rating => rating,
                    rating => approvedReviews.Count(r => r.Rating == rating)
                );

            // Get unique colors and sizes from variants (only show in-stock variants)
            ViewBag.AvailableColors = product.Variants
                .Where(v => !string.IsNullOrEmpty(v.Color) && v.Stock > 0)
                .GroupBy(v => v.Color)
                .Select(g => new {
                    Color = g.Key,
                    ColorCode = g.First().ColorCode,
                    Stock = g.Sum(v => v.Stock),
                    ImageUrl = g.First().ImageUrl
                })
                .ToList();

            ViewBag.AvailableSizes = product.Variants
                .Where(v => !string.IsNullOrEmpty(v.Size) && v.Stock > 0)
                .Select(v => v.Size)
                .Distinct()
                .ToList();

            // Get related products (same category, different product, excluding suspended seller products)
            var relatedProducts = await _db.Products
                .Include(p => p.Seller)
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsAvailable && p.Status == ProductStatus.Active
                    && (p.Seller == null || p.Seller.Status == SellerStatus.Approved))
                .OrderByDescending(p => p.SoldCount)
                .Take(8)
                .ToListAsync();
            ViewBag.RelatedProducts = relatedProducts;

            // Check if user has this in wishlist
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                ViewBag.IsInWishlist = await _db.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == product.Id);
                ViewBag.HasPurchased = await _db.Orders
                    .Include(o => o.OrderItems)
                    .AnyAsync(o => o.UserId == userId && o.OrderItems.Any(oi => oi.ProductId == product.Id) && o.Status == OrderStatus.Delivered);
            }
            else
            {
                ViewBag.IsInWishlist = false;
                ViewBag.HasPurchased = false;
            }

            return View(product);
        }

        [AllowAnonymous]
        public async Task<IActionResult> QuickView(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Block access to suspended seller products
            if (product.Seller != null && product.Seller.Status == SellerStatus.Suspended)
            {
                return NotFound();
            }

            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            ViewBag.CurrencySymbol = siteSettings.CurrencySymbol ?? "৳";

            return PartialView("_QuickView", product);
        }

        [HttpPost]
        [ActionName("Details")]
        public async Task<IActionResult> ProductDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = _db.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Use CartService to add to cart with shop validation
            var userId = GetCurrentUserId();
            var result = await _cartService.AddToCartAsync(product.Id, 1, userId);

            if (result.ShopConflict)
            {
                TempData["ShopConflict"] = "true";
                TempData["ConflictProductId"] = product.Id;
                TempData["ConflictQuantity"] = 1;
                TempData["ExistingShopName"] = result.ExistingShopName;
                return RedirectToAction("Details", new { id = product.Id });
            }

            if (!result.Success)
            {
                TempData["error"] = result.Message;
                return RedirectToAction("Details", new { id = product.Id });
            }

            TempData["onAdd"] = _localizer["ProductAddedToCart"].Value;
            return View(product);
        }

        public async Task<IActionResult> Cart()
        {
            var userId = GetCurrentUserId();
            var cartItems = await _cartService.GetCartItemsAsync(userId);

            // Get site settings for delivery charge
            var siteSettings = await _db.SiteSettings.FirstOrDefaultAsync();
            ViewBag.SiteSettings = siteSettings;

            // Check for applied coupon in session
            var appliedCouponId = HttpContext.Session.GetInt32("AppliedCouponId");
            if (appliedCouponId.HasValue)
            {
                var coupon = await _db.Coupons.FindAsync(appliedCouponId.Value);
                if (coupon != null && coupon.IsValid)
                {
                    ViewBag.AppliedCoupon = coupon;
                    var subtotal = cartItems.Sum(i => i.EffectivePrice * i.Quantity);
                    ViewBag.DiscountAmount = CalculateCouponDiscount(coupon, subtotal);
                }
                else
                {
                    // Invalid coupon, remove from session
                    HttpContext.Session.Remove("AppliedCouponId");
                }
            }

            return View(cartItems);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DecrementCart(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var userId = GetCurrentUserId();
            var cartItems = await _cartService.GetCartItemsAsync(userId);
            var item = cartItems.FirstOrDefault(c => c.Product.Id == id);
            
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    await _cartService.UpdateQuantityAsync(id.Value, item.Quantity - 1, userId);
                }
                else
                {
                    await _cartService.RemoveFromCartAsync(id.Value, userId);
                }
            }
            
            return RedirectToAction("Cart");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> IncrementCart(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var userId = GetCurrentUserId();
            var cartItems = await _cartService.GetCartItemsAsync(userId);
            var item = cartItems.FirstOrDefault(c => c.Product.Id == id);
            
            if (item != null)
            {
                await _cartService.UpdateQuantityAsync(id.Value, item.Quantity + 1, userId);
            }
            
            return RedirectToAction("Cart");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.GetCartItemsAsync(userId);

            if (!cart.Any())
            {
                TempData["error"] = _localizer["YourCartIsEmpty"].Value;
                return RedirectToAction("Index");
            }

            // Calculate cart subtotal for free delivery threshold check
            decimal cartSubtotal = cart.Sum(item => item.EffectivePrice * item.Quantity);

            // Get default delivery charge from site settings (with free threshold check)
            decimal deliveryCharge = await GetDeliveryChargeAsync(null, cartSubtotal);

            var viewModel = new CheckoutViewModel
            {
                CartItems = cart,
                DeliveryCharge = deliveryCharge,
                PaymentMethod = PaymentMethod.CashOnDelivery
            };

            // Check for applied coupon from cart session
            var appliedCouponId = HttpContext.Session.GetInt32("AppliedCouponId");
            if (appliedCouponId.HasValue)
            {
                var coupon = await _db.Coupons.FindAsync(appliedCouponId.Value);
                if (coupon != null && coupon.IsValid)
                {
                    ViewBag.AppliedCoupon = coupon;
                    var subtotal = cart.Sum(i => i.EffectivePrice * i.Quantity);
                    ViewBag.CouponDiscountAmount = CalculateCouponDiscount(coupon, subtotal);
                }
                else
                {
                    // Invalid coupon, remove from session
                    HttpContext.Session.Remove("AppliedCouponId");
                }
            }

            // Pre-populate user info and load saved addresses
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    viewModel.CustomerName = user.FullName;
                    viewModel.Email = user.Email ?? "";
                    viewModel.Phone = user.PhoneNumber ?? "";

                    // Check if COD is allowed for this user
                    ViewBag.IsCODAllowed = user.IsCODAllowed;
                    ViewBag.CODRestrictionReason = user.CODRestrictionReason;

                    // If COD is not allowed, default to online payment
                    if (!user.IsCODAllowed)
                    {
                        viewModel.PaymentMethod = PaymentMethod.bKash; // Default to bKash if COD restricted
                    }

                    // Load saved addresses
                    var savedAddresses = await _db.UserAddresses
                        .Include(a => a.Division)
                        .Include(a => a.District)
                        .Include(a => a.Upazila)
                        .Where(a => a.UserId == userId)
                        .OrderByDescending(a => a.IsDefault)
                        .ThenByDescending(a => a.CreatedAt)
                        .ToListAsync();

                    viewModel.SavedAddresses = savedAddresses;

                    // If user has saved addresses, use the default one
                    var defaultAddress = savedAddresses.FirstOrDefault(a => a.IsDefault) ?? savedAddresses.FirstOrDefault();
                    if (defaultAddress != null)
                    {
                        viewModel.UseSavedAddress = true;
                        viewModel.SelectedAddressId = defaultAddress.Id;
                        viewModel.DivisionId = defaultAddress.DivisionId;
                        viewModel.DistrictId = defaultAddress.DistrictId;
                        viewModel.UpazilaId = defaultAddress.UpazilaId;
                        viewModel.Address = defaultAddress.FullAddress;
                        viewModel.PostalCode = defaultAddress.PostalCode;
                        viewModel.Phone = defaultAddress.Phone;
                        viewModel.CustomerName = defaultAddress.RecipientName;

                        // Update delivery charge based on selected address district (with free threshold check)
                        viewModel.DeliveryCharge = await GetDeliveryChargeAsync(defaultAddress.DistrictId, cartSubtotal);
                    }
                    else
                    {
                        // No saved addresses, use user's profile address
                        viewModel.UseSavedAddress = false;
                        viewModel.DivisionId = user.DivisionId;
                        viewModel.DistrictId = user.DistrictId;
                        viewModel.UpazilaId = user.UpazilaId;
                        viewModel.Address = user.Address ?? "";
                        viewModel.PostalCode = user.PostalCode;

                        // Update delivery charge based on user's district (with free threshold check)
                        viewModel.DeliveryCharge = await GetDeliveryChargeAsync(user.DistrictId, cartSubtotal);
                    }
                }
            }

            // Pass MFS numbers and delivery settings from SiteSettings
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            ViewBag.BkashNumber = siteSettings?.BkashNumber ?? "01XXXXXXXXX";
            ViewBag.NagadNumber = siteSettings?.NagadNumber ?? "01XXXXXXXXX";
            ViewBag.RocketNumber = siteSettings?.RocketNumber ?? "01XXXXXXXXX";
            ViewBag.UpayNumber = siteSettings?.UpayNumber ?? "01XXXXXXXXX";
            ViewBag.FreeDeliveryThreshold = siteSettings?.FreeDeliveryThreshold ?? 0m;

            // Load rewards and wallet data for logged in user
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Get available rewards (vouchers)
                    var availableRewards = await _rewardService.GetAvailableRewardsAsync(userId);
                    viewModel.AvailableFreeShippingVouchers = availableRewards
                        .Where(r => r.RewardType == RewardType.FreeShipping)
                        .ToList();
                    viewModel.AvailableDiscountVouchers = availableRewards
                        .Where(r => r.RewardType == RewardType.DiscountVoucher)
                        .ToList();

                    // Wallet balance
                    viewModel.AvailableWalletBalance = user.WalletBalance;

                    // Premium status
                    viewModel.IsPremiumMember = await _rewardService.CheckPremiumStatusAsync(userId);
                    viewModel.HasActivePremiumDiscount = await _rewardService.HasActivePremiumDiscountAsync(userId);

                    // If premium member, apply free shipping automatically
                    if (viewModel.IsPremiumMember)
                    {
                        viewModel.DeliveryCharge = 0m;
                        viewModel.FreeShippingApplied = true;
                    }
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.GetCartItemsAsync(userId);

            if (!cart.Any())
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = _localizer["YourCartIsEmpty"].Value });
                }
                TempData["error"] = _localizer["YourCartIsEmpty"].Value;
                return RedirectToAction("Index");
            }

            // Calculate cart subtotal for free delivery threshold check
            decimal cartSubtotalForThreshold = cart.Sum(item => item.EffectivePrice * item.Quantity);

            // If using saved address, populate address fields from selected address
            if (model.UseSavedAddress && model.SelectedAddressId.HasValue)
            {
                var savedAddress = await _db.UserAddresses
                    .Include(a => a.Division)
                    .Include(a => a.District)
                    .Include(a => a.Upazila)
                    .FirstOrDefaultAsync(a => a.Id == model.SelectedAddressId.Value && a.UserId == userId);

                if (savedAddress != null)
                {
                    model.CustomerName = savedAddress.RecipientName;
                    model.Phone = savedAddress.Phone;
                    model.DivisionId = savedAddress.DivisionId;
                    model.DistrictId = savedAddress.DistrictId;
                    model.UpazilaId = savedAddress.UpazilaId;
                    model.Address = savedAddress.FullAddress;
                    model.PostalCode = savedAddress.PostalCode;

                    // Update delivery charge based on district (with free threshold check)
                    model.DeliveryCharge = await GetDeliveryChargeAsync(savedAddress.DistrictId, cartSubtotalForThreshold);
                }
            }

            // Clear address-related validation errors if using saved address
            if (model.UseSavedAddress && model.SelectedAddressId.HasValue)
            {
                // Remove validation for all fields that are populated from saved address
                ModelState.Remove("CustomerName");
                ModelState.Remove("Email");
                ModelState.Remove("Phone");
                ModelState.Remove("DivisionId");
                ModelState.Remove("DistrictId");
                ModelState.Remove("UpazilaId");
                ModelState.Remove("Address");

                // Get user's email for the order since it's not in the form when using saved address
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    model.Email = user.Email ?? "";
                }
            }

            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    return Json(new { success = false, errors = errors });
                }

                model.CartItems = cart;
                model.SavedAddresses = await _db.UserAddresses
                    .Include(a => a.Division)
                    .Include(a => a.District)
                    .Include(a => a.Upazila)
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToListAsync();
                return View(model);
            }

            // Check COD restriction for the user
            if (model.PaymentMethod == PaymentMethod.CashOnDelivery)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && !user.IsCODAllowed)
                {
                    var errorMessage = _localizer["CODNotAvailableForYourAccount"].Value;
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = errorMessage, codRestricted = true });
                    }
                    TempData["error"] = errorMessage;
                    model.CartItems = cart;
                    model.SavedAddresses = await _db.UserAddresses
                        .Include(a => a.Division)
                        .Include(a => a.District)
                        .Include(a => a.Upazila)
                        .Where(a => a.UserId == userId)
                        .OrderByDescending(a => a.IsDefault)
                        .ThenByDescending(a => a.CreatedAt)
                        .ToListAsync();
                    ViewBag.IsCODAllowed = false;
                    ViewBag.CODRestrictionReason = user.CODRestrictionReason;
                    return View(model);
                }
            }

            try
            {
                // Calculate total using EffectivePrice (includes variant AdditionalPrice)
                decimal subtotal = cart.Sum(item => item.EffectivePrice * item.Quantity);
                decimal deliveryCharge = model.DeliveryCharge;

                // Initialize reward/wallet tracking variables
                decimal rewardDiscountAmount = 0m;
                decimal premiumDiscountAmount = 0m;
                decimal walletAmountUsed = 0m;
                decimal couponDiscountAmount = 0m;
                bool freeShippingApplied = false;
                bool isPremiumOrder = false;
                int? appliedRewardId = null;
                int? appliedFreeShippingRewardId = null;
                int? appliedCouponId = null;
                UserRedeemedReward? discountVoucher = null;
                UserRedeemedReward? freeShippingVoucher = null;
                Coupon? appliedCoupon = null;

                // Check for applied coupon from cart session
                var sessionCouponId = HttpContext.Session.GetInt32("AppliedCouponId");
                if (sessionCouponId.HasValue)
                {
                    appliedCoupon = await _db.Coupons.FindAsync(sessionCouponId.Value);
                    if (appliedCoupon != null && appliedCoupon.IsValid)
                    {
                        couponDiscountAmount = CalculateCouponDiscount(appliedCoupon, subtotal);
                        appliedCouponId = appliedCoupon.Id;
                    }
                }

                // Get user for premium/wallet checks
                var orderUser = await _userManager.FindByIdAsync(userId ?? "");

                if (orderUser != null)
                {
                    // Check premium status
                    isPremiumOrder = await _rewardService.CheckPremiumStatusAsync(orderUser.Id);

                    // Premium member gets free shipping on all orders
                    if (isPremiumOrder)
                    {
                        deliveryCharge = 0m;
                        freeShippingApplied = true;
                    }

                    // Apply 5% premium discount if within first week of premium activation
                    if (await _rewardService.HasActivePremiumDiscountAsync(orderUser.Id))
                    {
                        premiumDiscountAmount = Math.Round(subtotal * 0.05m, 2);
                    }

                    // Apply selected free shipping voucher (if not already premium)
                    if (!freeShippingApplied && model.SelectedFreeShippingRewardId.HasValue)
                    {
                        freeShippingVoucher = await _db.UserRedeemedRewards
                            .FirstOrDefaultAsync(r => r.Id == model.SelectedFreeShippingRewardId.Value
                                && r.UserId == orderUser.Id
                                && r.Status == RewardStatus.Available
                                && r.RewardType == RewardType.FreeShipping
                                && r.ExpiresAt > DateTime.UtcNow);

                        if (freeShippingVoucher != null)
                        {
                            deliveryCharge = 0m;
                            freeShippingApplied = true;
                            appliedFreeShippingRewardId = freeShippingVoucher.Id;
                        }
                    }

                    // Apply selected discount voucher
                    if (model.SelectedDiscountRewardId.HasValue)
                    {
                        discountVoucher = await _db.UserRedeemedRewards
                            .FirstOrDefaultAsync(r => r.Id == model.SelectedDiscountRewardId.Value
                                && r.UserId == orderUser.Id
                                && r.Status == RewardStatus.Available
                                && r.RewardType == RewardType.DiscountVoucher
                                && r.ExpiresAt > DateTime.UtcNow);

                        if (discountVoucher != null)
                        {
                            // Check minimum order amount
                            if (!discountVoucher.MinimumOrderAmount.HasValue || subtotal >= discountVoucher.MinimumOrderAmount.Value)
                            {
                                rewardDiscountAmount = discountVoucher.DiscountAmount ?? 0m;
                                appliedRewardId = discountVoucher.Id;
                            }
                        }
                    }

                    // Apply wallet balance if requested
                    if (model.UseWalletBalance && orderUser.WalletBalance > 0 && model.WalletAmountToUse > 0)
                    {
                        // Calculate what can be applied (can't exceed wallet balance or order total)
                        decimal remainingTotal = subtotal + deliveryCharge - rewardDiscountAmount - premiumDiscountAmount;
                        walletAmountUsed = Math.Round(Math.Min(Math.Min(model.WalletAmountToUse, orderUser.WalletBalance), remainingTotal)); // Round to nearest integer
                        walletAmountUsed = Math.Max(0, walletAmountUsed); // Ensure non-negative
                    }
                }

                // Calculate final total (including coupon discount)
                decimal totalAmount = subtotal + deliveryCharge - rewardDiscountAmount - premiumDiscountAmount - walletAmountUsed - couponDiscountAmount;
                totalAmount = Math.Max(0, totalAmount); // Ensure non-negative
                totalAmount = Math.Round(totalAmount); // Round to nearest integer (no fractions)

                // Build address string for order
                var addressParts = new List<string>();
                if (model.DivisionId.HasValue)
                {
                    var division = await _db.Divisions.FindAsync(model.DivisionId.Value);
                    if (division != null) addressParts.Add(division.Name + " Division");
                }
                if (model.DistrictId.HasValue)
                {
                    var district = await _db.Districts.FindAsync(model.DistrictId.Value);
                    if (district != null) addressParts.Add(district.Name + " District");
                }
                if (model.UpazilaId.HasValue)
                {
                    var upazila = await _db.Upazilas.FindAsync(model.UpazilaId.Value);
                    if (upazila != null) addressParts.Add(upazila.Name + " Upazila");
                }

                if (!string.IsNullOrEmpty(model.Address))
                {
                    addressParts.Add(model.Address);
                }

                var fullAddress = string.Join(", ", addressParts);

                // Get seller ID from cart items (single-shop-per-order)
                var orderSellerId = cart.FirstOrDefault()?.Product?.SellerId;

                // Create order
                var order = new Order
                {
                    CustomerName = model.CustomerName,
                    Email = model.Email ?? "customer@example.com",
                    Phone = model.Phone,
                    Address = fullAddress,
                    PostalCode = model.PostalCode ?? "",
                    DivisionId = model.DivisionId,
                    DistrictId = model.DistrictId,
                    UpazilaId = model.UpazilaId,
                    SubTotal = subtotal,
                    DeliveryCharge = deliveryCharge,
                    TotalAmount = totalAmount,
                    PaymentMethod = model.PaymentMethod,
                    TransactionId = model.TransactionId,
                    SenderMobileNumber = model.MobileBankingNumber,
                    Notes = model.DeliveryInstructions,
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.Now,
                    IsPaymentReceived = false,
                    UserId = userId,
                    SellerId = orderSellerId,
                    // Coupon fields
                    CouponId = appliedCouponId,
                    DiscountAmount = couponDiscountAmount,
                    // Reward/Wallet fields
                    AppliedRewardId = appliedRewardId,
                    AppliedFreeShippingRewardId = appliedFreeShippingRewardId,
                    RewardDiscountAmount = rewardDiscountAmount,
                    FreeShippingApplied = freeShippingApplied,
                    WalletAmountUsed = walletAmountUsed,
                    PremiumDiscountAmount = premiumDiscountAmount,
                    IsPremiumOrder = isPremiumOrder
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                // Mark vouchers as used and update user wallet balance
                if (orderUser != null)
                {
                    // Mark discount voucher as used
                    if (discountVoucher != null && appliedRewardId.HasValue)
                    {
                        discountVoucher.Status = RewardStatus.Used;
                        discountVoucher.UsedOnOrderId = order.Id;
                        discountVoucher.UsedAt = DateTime.UtcNow;
                    }

                    // Mark free shipping voucher as used
                    if (freeShippingVoucher != null && appliedFreeShippingRewardId.HasValue)
                    {
                        freeShippingVoucher.Status = RewardStatus.Used;
                        freeShippingVoucher.UsedOnOrderId = order.Id;
                        freeShippingVoucher.UsedAt = DateTime.UtcNow;
                    }

                    // Deduct wallet balance
                    if (walletAmountUsed > 0)
                    {
                        orderUser.WalletBalance -= walletAmountUsed;
                    }

                    await _db.SaveChangesAsync();
                }

                // Track coupon usage and clear from session
                if (appliedCoupon != null && appliedCouponId.HasValue)
                {
                    // Increment times used
                    appliedCoupon.TimesUsed++;
                    appliedCoupon.UpdatedAt = DateTime.UtcNow;

                    // Create coupon usage record
                    var couponUsage = new CouponUsage
                    {
                        CouponId = appliedCoupon.Id,
                        UserId = userId ?? "",
                        OrderId = order.Id,
                        DiscountAmount = couponDiscountAmount,
                        UsedAt = DateTime.UtcNow
                    };
                    _db.CouponUsages.Add(couponUsage);
                    await _db.SaveChangesAsync();

                    // Clear coupon from session
                    HttpContext.Session.Remove("AppliedCouponId");
                }

                // Save address for future orders if requested
                if (!model.UseSavedAddress && model.SaveAddress && !string.IsNullOrEmpty(userId))
                {
                    // Check if similar address already exists
                    var existingAddress = await _db.UserAddresses
                        .FirstOrDefaultAsync(a => a.UserId == userId &&
                            a.DivisionId == model.DivisionId &&
                            a.DistrictId == model.DistrictId &&
                            a.UpazilaId == model.UpazilaId &&
                            a.FullAddress == model.Address);

                    if (existingAddress == null)
                    {
                        var hasExistingAddresses = await _db.UserAddresses.AnyAsync(a => a.UserId == userId);
                        var newAddress = new UserAddress
                        {
                            UserId = userId,
                            RecipientName = model.CustomerName,
                            Phone = model.Phone,
                            DivisionId = model.DivisionId,
                            DistrictId = model.DistrictId,
                            UpazilaId = model.UpazilaId,
                            FullAddress = model.Address ?? "",
                            PostalCode = model.PostalCode,
                            AddressType = AddressType.Home,
                            IsDefault = !hasExistingAddresses, // Set as default if first address
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.UserAddresses.Add(newAddress);
                        await _db.SaveChangesAsync();
                    }
                }

                // Add order items with variant info
                foreach (var item in cart)
                {
                    // Calculate unit price including variant additional price
                    var basePrice = item.Product.HasDiscount && item.Product.DiscountPrice.HasValue
                        ? item.Product.DiscountPrice.Value
                        : item.Product.Price;
                    var unitPrice = basePrice + item.AdditionalPrice;

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.Product.Id,
                        ProductVariantId = item.ProductVariantId,
                        ProductName = item.Product.Name,
                        ProductImageUrl = item.Product.ImageUrl,
                        VariantInfo = item.VariantInfo,
                        SKU = item.VariantSKU ?? item.Product.SKU,
                        UnitPrice = unitPrice,
                        Quantity = item.Quantity,
                        TotalPrice = unitPrice * item.Quantity,
                        SellerId = item.Product.SellerId
                    };
                    _db.OrderItems.Add(orderItem);

                    // Deduct stock from variant or product and record history
                    if (item.ProductVariantId.HasValue)
                    {
                        var variant = await _db.ProductVariants.FindAsync(item.ProductVariantId.Value);
                        if (variant != null)
                        {
                            var oldStock = variant.Stock;
                            variant.Stock -= item.Quantity;
                            if (variant.Stock <= 0)
                            {
                                variant.IsAvailable = false;
                            }
                            // Record stock change
                            await _stockAlertService.RecordStockChangeAsync(
                                item.Product.Id, variant.Id, oldStock, variant.Stock,
                                StockChangeReason.OrderPlaced, $"Order #{order.OrderNumber}", order.Id, userId);
                        }
                    }
                    else
                    {
                        var oldStock = item.Product.Stock;
                        item.Product.Stock -= item.Quantity;
                        // Record stock change
                        await _stockAlertService.RecordStockChangeAsync(
                            item.Product.Id, null, oldStock, item.Product.Stock,
                            StockChangeReason.OrderPlaced, $"Order #{order.OrderNumber}", order.Id, userId);
                    }
                }
                await _db.SaveChangesAsync();

                // Build email body with payment method info
                var paymentMethodName = model.PaymentMethod switch
                {
                    PaymentMethod.CashOnDelivery => "Cash on Delivery",
                    PaymentMethod.OnlinePayment => "Online Payment",
                    PaymentMethod.bKash => "bKash",
                    PaymentMethod.Nagad => "Nagad",
                    PaymentMethod.Rocket => "Rocket",
                    PaymentMethod.CreditDebitCard => "Credit/Debit Card",
                    _ => "Cash on Delivery"
                };

                var emailSubject = "Order Confirmation - #" + order.Id;
                var emailBody = $@"
                    <h2>Thank you for your order!</h2>
                    <p>Order Number: <strong>#{order.Id}</strong></p>
                    <p>Total Amount: <strong>৳{order.TotalAmount:N2}</strong></p>
                    <p>Payment Method: <strong>{paymentMethodName}</strong></p>
                    <p>Delivery Address: {order.Address}</p>
                    {(!string.IsNullOrEmpty(model.DeliveryInstructions) ? $"<p>Delivery Instructions: {model.DeliveryInstructions}</p>" : "")}
                    <p>We will contact you at {order.Phone} to confirm your order.</p>
                ";

                try
                {
                    await _emailService.SendEmailAsync(model.Email ?? "customer@example.com", emailSubject, emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send order confirmation email");
                }

                // Handle payment method
                bool requiresOnlinePayment = model.PaymentMethod == PaymentMethod.OnlinePayment ||
                                            model.PaymentMethod == PaymentMethod.CreditDebitCard;

                // For mobile banking (bKash, Nagad, Rocket) - if transaction ID provided, treat as manual payment
                bool isMobileBanking = model.PaymentMethod == PaymentMethod.bKash ||
                                      model.PaymentMethod == PaymentMethod.Nagad ||
                                      model.PaymentMethod == PaymentMethod.Rocket;

                if (requiresOnlinePayment)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new
                        {
                            success = true,
                            orderId = order.Id,
                            paymentUrl = Url.Action("InitiateSSLPayment", "Home", new { orderId = order.Id })
                        });
                    }
                    return RedirectToAction("InitiateSSLPayment", new { orderId = order.Id });
                }
                else
                {
                    // Cash on Delivery or Mobile Banking with manual transaction - complete the order
                    await _cartService.ClearCartAsync(userId);
                    _logger.LogInformation($"Cart cleared for user {userId} after order #{order.Id}");

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new
                        {
                            success = true,
                            orderId = order.Id,
                            redirectUrl = Url.Action("OrderConfirmation", "Home", new { id = order.Id })
                        });
                    }

                    TempData["success"] = _localizer["OrderPlacedSuccessfully"].Value + $" #{order.Id}";
                    return RedirectToAction("OrderConfirmation", new { id = order.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = _localizer["ErrorOccurredWhilePlacingOrder"].Value });
                }

                TempData["error"] = _localizer["ErrorOccurredWhilePlacingOrder"].Value;
                model.CartItems = cart;
                model.SavedAddresses = await _db.UserAddresses
                    .Include(a => a.Division)
                    .Include(a => a.District)
                    .Include(a => a.Upazila)
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToListAsync();
                model.DeliveryCharge = await GetDeliveryChargeAsync(model.DistrictId, cartSubtotalForThreshold);
                return View(model);
            }
        }

        [Authorize]
        public IActionResult OrderConfirmation(int id)
        {
            var order = _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        [Authorize]
        public async Task<IActionResult> Orders(int? page)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index");
            }

            var pageSize = 10;
            var pageNumber = page ?? 1;

            var orders = _db.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToPagedList(pageNumber, pageSize);

            return View(orders);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Privacy()
        {
            // Try to get dynamic content from CMS
            var page = await _siteSettingsService.GetPageContentBySlugAsync("privacy-policy");
            if (page != null)
            {
                return View("Page", page);
            }
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Page(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var page = await _siteSettingsService.GetPageContentBySlugAsync(slug);
            if (page == null)
            {
                return NotFound();
            }

            ViewData["Title"] = page.Title;
            ViewData["MetaDescription"] = page.MetaDescription;
            ViewData["MetaKeywords"] = page.MetaKeywords;

            return View(page);
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var userId = GetCurrentUserId();
            await _cartService.RemoveFromCartAsync(id.Value, userId);
            
            TempData["success"] = _localizer["ProductRemovedFromCart"].Value;
            return RedirectToAction("Cart");
        }

        [Authorize]
        public async Task<IActionResult> InitiateSSLPayment(int orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction("Index");
            }

            // SSL ECOMMERCE Configuration (These should be in appsettings.json in production)
            var storeId = _config["SSLCommerz:StoreId"] ?? "testbox";
            var storePassword = _config["SSLCommerz:StorePassword"] ?? "qwerty";
            var isLive = bool.Parse(_config["SSLCommerz:IsLive"] ?? "false");
            
            var sslUrl = isLive ? "https://securepay.sslcommerz.com/gwprocess/v4/api.php" 
                               : "https://sandbox.sslcommerz.com/gwprocess/v4/api.php";

            // Prepare SSL ECOMMERCE payment data
            var formData = new Dictionary<string, string>
            {
                {"store_id", storeId},
                {"store_passwd", storePassword},
                {"total_amount", order.TotalAmount.ToString("F2")},
                {"currency", "BDT"},
                {"tran_id", $"TXN{order.Id}_{DateTime.Now:yyyyMMddHHmmss}"},
                {"success_url", Url.Action("PaymentSuccess", "Home", null, Request.Scheme)!},
                {"fail_url", Url.Action("PaymentFailed", "Home", null, Request.Scheme)!},
                {"cancel_url", Url.Action("PaymentCancelled", "Home", null, Request.Scheme)!},
                {"ipn_url", Url.Action("PaymentIPN", "Home", null, Request.Scheme)!},
                
                // Customer Information
                {"cus_name", order.CustomerName},
                {"cus_email", order.Email},
                {"cus_phone", order.Phone},
                {"cus_add1", order.Address},
                {"cus_city", "Dhaka"},
                {"cus_postcode", order.PostalCode},
                {"cus_country", "Bangladesh"},
                
                // Product Information
                {"product_name", $"Order #{order.Id}"},
                {"product_category", "General"},
                {"product_profile", "general"},
                
                // Shipping Information
                {"ship_name", order.CustomerName},
                {"ship_add1", order.Address},
                {"ship_city", "Dhaka"},
                {"ship_postcode", order.PostalCode},
                {"ship_country", "Bangladesh"}
            };

            // Create form for auto-submission
            ViewBag.SSLUrl = sslUrl;
            ViewBag.FormData = formData;
            ViewBag.OrderId = order.Id;
            
            return View("SSLPayment");
        }

        public async Task<IActionResult> PaymentSuccess(string val_id, string tran_id, string amount)
        {
            try
            {
                // Extract order ID from transaction ID
                var orderIdStr = tran_id.Split('_')[0].Replace("TXN", "");
                if (!int.TryParse(orderIdStr, out int orderId))
                {
                    TempData["error"] = _localizer["InvalidTransactionId"].Value;
                    return RedirectToAction("Index");
                }

                var order = await _db.Orders.FindAsync(orderId);
                if (order == null)
                {
                    TempData["error"] = _localizer["OrderNotFound"].Value;
                    return RedirectToAction("Index");
                }

                // Update order status
                order.Status = OrderStatus.Paid;
                order.IsPaymentReceived = true;
                order.TransactionId = val_id;
                await _db.SaveChangesAsync();

                // Clear cart for this order's user
                await _cartService.ClearCartAsync(order.UserId);
                _logger.LogInformation($"Cart cleared after successful payment for order #{order.Id}");

                TempData["success"] = _localizer["PaymentSuccessful"].Value + $" #{order.Id}";
                return RedirectToAction("OrderConfirmation", new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment success");
                TempData["error"] = _localizer["ErrorOccurredWhileProcessingPayment"].Value;
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> PaymentFailed(string tran_id)
        {
            try
            {
                // Extract order ID from transaction ID
                var orderIdStr = tran_id?.Split('_')[0].Replace("TXN", "");
                if (int.TryParse(orderIdStr, out int orderId))
                {
                    var order = await _db.Orders.FindAsync(orderId);
                    if (order != null)
                    {
                        order.Status = OrderStatus.Cancelled;
                        await _db.SaveChangesAsync();
                    }
                }

                TempData["error"] = _localizer["PaymentFailedOrderCancelled"].Value;
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment failure");
                TempData["error"] = _localizer["PaymentFailedTryAgain"].Value;
                return RedirectToAction("Cart");
            }
        }

        public async Task<IActionResult> PaymentCancelled(string tran_id)
        {
            try
            {
                // Extract order ID from transaction ID
                var orderIdStr = tran_id?.Split('_')[0].Replace("TXN", "");
                if (int.TryParse(orderIdStr, out int orderId))
                {
                    var order = await _db.Orders.FindAsync(orderId);
                    if (order != null)
                    {
                        order.Status = OrderStatus.Cancelled;
                        await _db.SaveChangesAsync();
                    }
                }

                TempData["warning"] = _localizer["PaymentCancelledOrderCancelled"].Value;
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment cancellation");
                TempData["error"] = _localizer["ErrorOccurredTryAgain"].Value;
                return RedirectToAction("Cart");
            }
        }

        [HttpPost]
        public async Task<IActionResult> PaymentIPN()
        {
            // SSL ECOMMERCE IPN (Instant Payment Notification) handler
            // This is called by SSL ECOMMERCE to notify about payment status
            try
            {
                var tranId = Request.Form["tran_id"].ToString();
                var valId = Request.Form["val_id"].ToString();
                var status = Request.Form["status"].ToString();

                if (!string.IsNullOrEmpty(tranId) && status == "VALID")
                {
                    var orderIdStr = tranId.Split('_')[0].Replace("TXN", "");
                    if (int.TryParse(orderIdStr, out int orderId))
                    {
                        var order = await _db.Orders.FindAsync(orderId);
                        if (order != null && !order.IsPaymentReceived)
                        {
                            order.Status = OrderStatus.Paid;
                            order.IsPaymentReceived = true;
                            order.TransactionId = valId;
                            await _db.SaveChangesAsync();

                            _logger.LogInformation($"Payment confirmed via IPN for order #{orderId}");
                        }
                    }
                }

                return Ok("IPN processed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing IPN");
                return BadRequest("IPN processing failed");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = _localizer["YouMustBeLoggedInToCancelOrder"].Value;
                return RedirectToAction("Index");
            }

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFoundOrNoPermission"].Value;
                return RedirectToAction("Index");
            }

            // Check if order can be cancelled (not confirmed, paid, or shipped)
            if (order.Status == OrderStatus.Confirmed || order.Status == OrderStatus.Paid || order.Status == OrderStatus.Shipped)
            {
                TempData["error"] = _localizer["OrderCannotBeCancelledAlreadyProcessed"].Value;
                return RedirectToAction("OrderConfirmation", new { id = order.Id });
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                TempData["warning"] = _localizer["OrderAlreadyCancelled"].Value;
                return RedirectToAction("OrderConfirmation", new { id = order.Id });
            }

            // Cancel the order
            order.Status = OrderStatus.Cancelled;
            await _db.SaveChangesAsync();

            TempData["success"] = _localizer["OrderCancelledSuccessfully"].Value + $" #{order.Id}";
            return RedirectToAction("OrderConfirmation", new { id = order.Id });
        }

        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index");
            }

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Division)
                .Include(o => o.District)
                .Include(o => o.Upazila)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction("Orders");
            }

            return View(order);
        }

        // Track Order - Public access with order number
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> TrackOrder(string? orderNumber, string? phone)
        {
            // Show empty form if no search parameters
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                return View();
            }

            // Search for order by order number and optional phone
            var query = _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Division)
                .Include(o => o.District)
                .Include(o => o.Upazila)
                .Where(o => o.OrderNumber == orderNumber || o.Id.ToString() == orderNumber);

            // If phone provided, verify it matches
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var normalizedPhone = phone.Trim().Replace("-", "").Replace(" ", "");
                query = query.Where(o => o.Phone != null && o.Phone.Replace("-", "").Replace(" ", "") == normalizedPhone);
            }

            var order = await query.FirstOrDefaultAsync();

            if (order == null)
            {
                ViewBag.SearchError = "Order not found. Please check your order number and phone number.";
                ViewBag.SearchedOrderNumber = orderNumber;
                ViewBag.SearchedPhone = phone;
                return View();
            }

            return View(order);
        }

        // API endpoint for real-time order status
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetOrderStatus(string orderNumber, string? phone)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                return Json(new { success = false, message = "Order number is required" });
            }

            var query = _db.Orders
                .Where(o => o.OrderNumber == orderNumber || o.Id.ToString() == orderNumber);

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var normalizedPhone = phone.Trim().Replace("-", "").Replace(" ", "");
                query = query.Where(o => o.Phone != null && o.Phone.Replace("-", "").Replace(" ", "") == normalizedPhone);
            }

            var order = await query.Select(o => new
            {
                o.Id,
                o.OrderNumber,
                Status = o.Status.ToString(),
                StatusDisplay = o.Status.GetDisplayName(),
                o.PaymentStatus,
                o.DeliveryStatus,
                o.TrackingNumber,
                o.CourierName,
                o.ShippedAt,
                o.DeliveredAt,
                o.CancelledAt,
                o.ReturnedAt,
                LastUpdated = o.UpdatedAt
            }).FirstOrDefaultAsync();

            if (order == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            return Json(new { success = true, order });
        }

        [Authorize]
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index");
            }

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction("Orders");
            }

            var pdfBytes = await _pdfGenerator.GenerateOrderInvoicePdfAsync(order);

            var fileName = $"Invoice_{order.OrderNumber ?? order.Id.ToString("D6")}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [Authorize]
        public async Task<IActionResult> ContactSeller(int orderId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index");
            }

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction("Orders");
            }

            // Pre-fill ticket with order information
            TempData["TicketSubject"] = $"Query regarding Order #{order.OrderNumber ?? order.Id.ToString("D6")}";
            TempData["TicketOrderId"] = order.Id;
            TempData["TicketCategory"] = "OrderIssue";

            // Redirect to support tickets page
            return RedirectToPage("/Account/Manage/SupportTickets", new { area = "Identity" });
        }

        // POST: Customer/Home/SetLanguage
        // Changes the UI language (culture) for the site
        [HttpPost]
        [AllowAnonymous]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                Response.Cookies.Append(
                    "Bangaliyana.Culture",
                    Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(
                        new Microsoft.AspNetCore.Localization.RequestCulture(culture)),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true,
                        SameSite = SameSiteMode.Lax
                    }
                );
            }

            return LocalRedirect(returnUrl ?? "~/");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // GET: Customer/Home/Search
        [AllowAnonymous]
        public async Task<IActionResult> Search(string q, int? productType, int? specialTag,
            decimal? minPrice, decimal? maxPrice, string? sortBy, int page = 1, int pageSize = 12)
        {
            var userId = GetCurrentUserId();
            var sessionId = HttpContext.Session.Id;

            // Create search request
            var searchRequest = new SearchRequest
            {
                Query = q ?? string.Empty,
                ProductTypeId = productType,
                SpecialTagId = specialTag,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy ?? "relevance",
                Page = page,
                PageSize = pageSize
            };

            // Get search results from search service (includes filters)
            var searchResult = await _searchService.SearchProductsAsync(searchRequest, userId, sessionId);

            // Record the search - await properly to avoid DbContext concurrency issues
            if (!string.IsNullOrWhiteSpace(q))
            {
                await _searchService.RecordSearchAsync(q, userId, sessionId, searchResult.TotalCount);
            }

            // Get trending searches (sequential to avoid DbContext concurrency)
            var trendingSearches = await _searchService.GetTrendingSearchesAsync(8);

            // Get recommendations if no search results
            var recommendations = new List<ProductRecommendation>();
            if (searchResult.TotalCount == 0)
            {
                recommendations = (await _searchService.GetRecommendationsAsync(userId, sessionId, 8)).ToList();
            }

            ViewBag.Query = q;
            ViewBag.ProductType = productType;
            ViewBag.SpecialTag = specialTag;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortBy = sortBy;
            ViewBag.SearchResult = searchResult;
            ViewBag.Filters = searchResult.AvailableFilters; // Use filters from search result
            ViewBag.TrendingSearches = trendingSearches;
            ViewBag.Recommendations = recommendations;
            ViewBag.PageNumber = page;
            ViewBag.TotalPages = (int)Math.Ceiling(searchResult.TotalCount / (double)pageSize);

            return View();
        }

        // POST: Customer/Home/AddToCart (Form submission - for Details page)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, int? variantId = null)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["error"] = _localizer["ProductNotFound"].Value;
                return RedirectToAction("Index");
            }

            var userId = GetCurrentUserId();
            var result = await _cartService.AddToCartAsync(productId, quantity, userId, variantId);

            if (result.ShopConflict)
            {
                // Store conflict info in TempData for modal
                TempData["ShopConflict"] = "true";
                TempData["ConflictProductId"] = productId;
                TempData["ConflictQuantity"] = quantity;
                TempData["ConflictVariantId"] = variantId;
                TempData["ExistingShopName"] = result.ExistingShopName;
                return RedirectToAction("Details", new { id = productId });
            }

            if (result.RequiresVariant)
            {
                TempData["error"] = result.Message;
                return RedirectToAction("Details", new { id = productId });
            }

            if (!result.Success)
            {
                TempData["error"] = result.Message;
                return RedirectToAction("Details", new { id = productId });
            }

            TempData["onAdd"] = _localizer["ProductAddedToCart"].Value;
            return RedirectToAction("Details", new { id = productId });
        }

        // POST: Customer/Home/AddToCartAjax (AJAX - for product cards, stays on same page)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddToCartAjax(int productId, int quantity = 1, int? variantId = null)
        {
            var product = await _db.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            if (!product.IsAvailable || product.Stock <= 0)
            {
                return Json(new { success = false, message = "Product is out of stock" });
            }

            // Check if product has variants and requires variant selection
            var availableVariants = product.Variants.Where(v => v.IsAvailable).ToList();
            if (availableVariants.Any() && !variantId.HasValue)
            {
                return Json(new { success = false, requiresVariant = true, message = "Please select a size and/or color" });
            }

            // Check stock based on variant or product
            if (variantId.HasValue)
            {
                var variant = availableVariants.FirstOrDefault(v => v.Id == variantId.Value);
                if (variant != null && quantity > variant.Stock)
                {
                    return Json(new { success = false, message = $"Only {variant.Stock} items available for this variant" });
                }
            }
            else if (quantity > product.Stock)
            {
                return Json(new { success = false, message = $"Only {product.Stock} items available" });
            }

            var userId = GetCurrentUserId();
            var result = await _cartService.AddToCartAsync(productId, quantity, userId, variantId);

            if (result.ShopConflict)
            {
                return Json(new {
                    success = false,
                    shopConflict = true,
                    existingShopName = result.ExistingShopName,
                    message = result.Message
                });
            }

            if (result.RequiresVariant)
            {
                return Json(new { success = false, requiresVariant = true, message = result.Message });
            }

            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }

            // Get updated cart count
            var cartItems = await _cartService.GetCartItemsAsync(userId);
            var cartCount = cartItems.Sum(i => i.Quantity);

            return Json(new { success = true, message = "Product added to cart!", cartCount });
        }

        // POST: Customer/Home/ConfirmShopChange (AJAX - user confirmed to replace cart)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ConfirmShopChange(int productId, int quantity = 1, int? variantId = null)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var userId = GetCurrentUserId();
            var result = await _cartService.ClearAndAddToCartAsync(productId, quantity, userId, variantId);

            if (result.RequiresVariant)
            {
                return Json(new { success = false, requiresVariant = true, message = result.Message });
            }

            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, message = "Cart updated!", cartCount = quantity });
        }

        // POST: Customer/Home/BuyNow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1, int? variantId = null)
        {
            var product = await _db.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                TempData["error"] = _localizer["ProductNotFound"].Value;
                return RedirectToAction("Index");
            }

            // Validate variant requirement
            var availableVariants = product.Variants.Where(v => v.IsAvailable).ToList();
            if (availableVariants.Any() && !variantId.HasValue)
            {
                TempData["error"] = _localizer["PleaseSelectSizeOrColor"].Value;
                return RedirectToAction("Details", new { id = productId });
            }

            var userId = GetCurrentUserId();

            // Clear cart and add only this product with variant
            await _cartService.ClearCartAsync(userId);
            var result = await _cartService.AddToCartAsync(productId, quantity, userId, variantId);

            if (!result.Success)
            {
                TempData["error"] = result.Message;
                return RedirectToAction("Details", new { id = productId });
            }

            return RedirectToAction("Checkout");
        }

        // POST: Customer/Home/AskQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AskQuestion(int productId, string question)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = _localizer["PleaseLoginToAskQuestion"].Value;
                return RedirectToAction("Details", new { id = productId });
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                TempData["error"] = _localizer["PleaseEnterQuestion"].Value;
                return RedirectToAction("Details", new { id = productId });
            }

            var productQuestion = new ProductQuestion
            {
                ProductId = productId,
                UserId = userId,
                Question = question.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsApproved = false,
                IsAnswered = false
            };

            _db.ProductQuestions.Add(productQuestion);
            await _db.SaveChangesAsync();

            TempData["success"] = _localizer["QuestionSubmittedSuccessfully"].Value;
            return RedirectToAction("Details", new { id = productId });
        }

        // POST: Customer/Home/SubmitReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string? title, string comment, string? pros, string? cons, IFormFileCollection? images)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = _localizer["PleaseLoginToSubmitReview"].Value;
                return RedirectToAction("Details", new { id = productId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["error"] = _localizer["PleaseSelectValidRating"].Value;
                return RedirectToAction("Details", new { id = productId });
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["error"] = _localizer["PleaseEnterYourReview"].Value;
                return RedirectToAction("Details", new { id = productId });
            }

            // Block reviews for suspended seller products
            var productToReview = await _db.Products
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (productToReview?.Seller != null && productToReview.Seller.Status == SellerStatus.Suspended)
            {
                TempData["error"] = _localizer["ReviewsCannotBeSubmittedForSuspendedSellers"].Value;
                return RedirectToAction("Details", new { id = productId });
            }

            // Check if user has already reviewed this product (prevent duplicates)
            if (await _rewardService.HasUserReviewedProductAsync(userId, productId))
            {
                TempData["error"] = _localizer["YouHaveAlreadyReviewedThisProduct"].Value;
                return RedirectToAction("Details", new { id = productId });
            }

            // Check if user has purchased this product
            var hasPurchased = await _db.Orders
                .Include(o => o.OrderItems)
                .AnyAsync(o => o.UserId == userId && o.OrderItems.Any(oi => oi.ProductId == productId) && o.Status == OrderStatus.Delivered);

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Title = title?.Trim(),
                Comment = comment.Trim(),
                Pros = pros?.Trim(),
                Cons = cons?.Trim(),
                IsVerifiedPurchase = hasPurchased,
                IsApproved = true, // Auto-approve reviews
                CreatedAt = DateTime.UtcNow
            };

            _db.ProductReviews.Add(review);
            await _db.SaveChangesAsync();

            // Update seller's shop rating
            var product = await _db.Products.FindAsync(productId);
            if (product != null && product.SellerId.HasValue)
            {
                await _shopRatingService.UpdateShopRatingAsync(product.SellerId.Value);
            }

            // Award points for 3+ star reviews
            if (rating >= 3)
            {
                await _rewardService.AwardProductReviewPointsAsync(review.Id);
            }

            // Handle image uploads
            if (images != null && images.Count > 0)
            {
                foreach (var image in images.Take(5)) // Max 5 images
                {
                    if (image.Length > 0 && image.ContentType.StartsWith("image/"))
                    {
                        var fileName = $"reviews/{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileName);

                        // Ensure directory exists
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        var reviewImage = new ReviewImage
                        {
                            ReviewId = review.Id,
                            ImageUrl = fileName
                        };
                        _db.ReviewImages.Add(reviewImage);
                    }
                }
                await _db.SaveChangesAsync();
            }

            TempData["success"] = _localizer["ThankYouForYourReview"].Value;
            return RedirectToAction("Details", new { id = productId });
        }

        // POST: Customer/Home/MarkReviewHelpful
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkReviewHelpful(int reviewId)
        {
            var review = await _db.ProductReviews.FindAsync(reviewId);
            if (review == null)
            {
                return Json(new { success = false, message = "Review not found" });
            }

            review.HelpfulCount++;
            await _db.SaveChangesAsync();

            return Json(new { success = true, helpfulCount = review.HelpfulCount });
        }

        // POST: Customer/Home/MarkQuestionHelpful
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkQuestionHelpful(int questionId)
        {
            var question = await _db.ProductQuestions.FindAsync(questionId);
            if (question == null)
            {
                return Json(new { success = false, message = "Question not found" });
            }

            question.HelpfulCount++;
            await _db.SaveChangesAsync();

            return Json(new { success = true, helpfulCount = question.HelpfulCount });
        }

        // POST: Customer/Home/UpdateCartQuantity
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateCartQuantity(int productId, int quantity, int? variantId = null)
        {
            var userId = GetCurrentUserId();

            if (quantity < 1)
            {
                return Json(new { success = false, message = "Quantity must be at least 1" });
            }

            // Check stock based on variant or product
            if (variantId.HasValue)
            {
                var variant = await _db.ProductVariants.FindAsync(variantId.Value);
                if (variant == null)
                {
                    return Json(new { success = false, message = "Variant not found" });
                }
                if (quantity > variant.Stock)
                {
                    return Json(new { success = false, message = $"Only {variant.Stock} items available for this variant" });
                }
            }
            else
            {
                var product = await _db.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }
                if (quantity > product.Stock)
                {
                    return Json(new { success = false, message = $"Only {product.Stock} items available in stock" });
                }
            }

            await _cartService.UpdateQuantityAsync(productId, quantity, userId, variantId);
            return Json(new { success = true });
        }

        // POST: Customer/Home/RemoveFromCart
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RemoveFromCart(int id, int? variantId = null)
        {
            var userId = GetCurrentUserId();
            await _cartService.RemoveFromCartAsync(id, userId, variantId);
            return Json(new { success = true });
        }

        // POST: Customer/Home/MoveToWishlist
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MoveToWishlist(int productId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login first", requireLogin = true });
            }

            // Check if product exists
            var product = await _db.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            // Add to wishlist if not already there
            var existingWishlistItem = await _db.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingWishlistItem == null)
            {
                _db.Wishlists.Add(new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            // Remove from cart
            await _cartService.RemoveFromCartAsync(productId, userId);

            return Json(new { success = true, message = "Item moved to wishlist" });
        }

        // POST: Customer/Home/ClearCart
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetCurrentUserId();
            await _cartService.ClearCartAsync(userId);

            // Also remove any applied coupon
            HttpContext.Session.Remove("AppliedCouponId");

            return Json(new { success = true });
        }

        // POST: Customer/Home/ApplyCoupon
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApplyCoupon(string couponCode)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return Json(new { success = false, message = "Please enter a coupon code" });
            }

            var coupon = await _db.Coupons
                .Include(c => c.Category)
                .Include(c => c.Seller)
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == couponCode.ToUpper().Trim());

            if (coupon == null)
            {
                return Json(new { success = false, message = "Invalid coupon code" });
            }

            // Check if coupon is active
            if (!coupon.IsActive)
            {
                return Json(new { success = false, message = "This coupon is no longer active" });
            }

            // Check validity dates
            if (coupon.StartDate.HasValue && coupon.StartDate > DateTime.UtcNow)
            {
                return Json(new { success = false, message = "This coupon is not yet valid" });
            }

            if (coupon.EndDate.HasValue && coupon.EndDate < DateTime.UtcNow)
            {
                return Json(new { success = false, message = "This coupon has expired" });
            }

            // Check usage limit
            if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit)
            {
                return Json(new { success = false, message = "This coupon has reached its usage limit" });
            }

            // Check per-user usage limit
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId) && coupon.UsagePerUserLimit.HasValue)
            {
                var userUsageCount = await _db.CouponUsages
                    .CountAsync(cu => cu.CouponId == coupon.Id && cu.UserId == userId);

                if (userUsageCount >= coupon.UsagePerUserLimit)
                {
                    return Json(new { success = false, message = "You have already used this coupon the maximum number of times" });
                }
            }

            // Check first order only
            if (coupon.IsFirstOrderOnly && !string.IsNullOrEmpty(userId))
            {
                var hasOrders = await _db.Orders.AnyAsync(o => o.UserId == userId);
                if (hasOrders)
                {
                    return Json(new { success = false, message = "This coupon is only valid for first-time orders" });
                }
            }

            // Get cart items and check minimum order amount
            var cartItems = await _cartService.GetCartItemsAsync(userId);
            var subtotal = cartItems.Sum(i => i.EffectivePrice * i.Quantity);

            if (coupon.MinimumOrderAmount.HasValue && subtotal < coupon.MinimumOrderAmount)
            {
                return Json(new { success = false, message = $"Minimum order amount of ৳{coupon.MinimumOrderAmount:N0} required for this coupon" });
            }

            // Check category restriction
            if (coupon.CategoryId.HasValue)
            {
                var hasEligibleProduct = cartItems.Any(i => i.Product.CategoryId == coupon.CategoryId);
                if (!hasEligibleProduct)
                {
                    return Json(new { success = false, message = $"This coupon is only valid for {coupon.Category?.Name ?? "specific"} category products" });
                }
            }

            // Check seller restriction
            if (coupon.SellerId.HasValue)
            {
                var hasSellerProduct = cartItems.Any(i => i.Product.SellerId == coupon.SellerId);
                if (!hasSellerProduct)
                {
                    return Json(new { success = false, message = $"This coupon is only valid for products from {coupon.Seller?.ShopName ?? "specific seller"}" });
                }
            }

            // Calculate discount
            var discountAmount = CalculateCouponDiscount(coupon, subtotal);

            // Store coupon in session
            HttpContext.Session.SetInt32("AppliedCouponId", coupon.Id);

            return Json(new { success = true, message = $"Coupon applied! You save ৳{discountAmount:N0}", discountAmount });
        }

        // POST: Customer/Home/RemoveCoupon
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove("AppliedCouponId");
            return Json(new { success = true });
        }

        // Helper method to calculate coupon discount
        private decimal CalculateCouponDiscount(Coupon coupon, decimal subtotal)
        {
            decimal discount = 0;

            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discount = subtotal * (coupon.DiscountValue / 100);
            }
            else // FixedAmount
            {
                discount = coupon.DiscountValue;
            }

            // Apply maximum discount cap if set
            if (coupon.MaximumDiscountAmount.HasValue && discount > coupon.MaximumDiscountAmount.Value)
            {
                discount = coupon.MaximumDiscountAmount.Value;
            }

            // Discount cannot exceed subtotal
            if (discount > subtotal)
            {
                discount = subtotal;
            }

            return discount;
        }

        #region User Address Management

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserAddresses()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var addresses = await _db.UserAddresses
                .Include(a => a.Division)
                .Include(a => a.District)
                .Include(a => a.Upazila)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            var addressList = addresses.Select(a => new
            {
                a.Id,
                a.Label,
                a.AddressType,
                AddressTypeName = a.AddressType.ToString(),
                a.RecipientName,
                a.Phone,
                a.DivisionId,
                DivisionName = a.Division?.Name,
                a.DistrictId,
                DistrictName = a.District?.Name,
                a.UpazilaId,
                UpazilaName = a.Upazila?.Name,
                a.FullAddress,
                a.PostalCode,
                a.IsDefault,
                FormattedAddress = FormatAddress(a)
            }).ToList();

            return Json(new { success = true, addresses = addressList });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAddress(int id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var address = await _db.UserAddresses
                .Include(a => a.Division)
                .Include(a => a.District)
                .Include(a => a.Upazila)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return Json(new { success = false, message = "Address not found" });
            }

            return Json(new
            {
                success = true,
                address = new
                {
                    address.Id,
                    address.Label,
                    address.AddressType,
                    AddressTypeName = address.AddressType.ToString(),
                    address.RecipientName,
                    address.Phone,
                    address.DivisionId,
                    DivisionName = address.Division?.Name,
                    address.DistrictId,
                    DistrictName = address.District?.Name,
                    address.UpazilaId,
                    UpazilaName = address.Upazila?.Name,
                    address.FullAddress,
                    address.PostalCode,
                    address.IsDefault,
                    FormattedAddress = FormatAddress(address)
                }
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAddress([FromBody] UserAddressDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            try
            {
                UserAddress address;

                if (dto.Id > 0)
                {
                    // Update existing address
                    address = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == dto.Id && a.UserId == userId);
                    if (address == null)
                    {
                        return Json(new { success = false, message = "Address not found" });
                    }
                }
                else
                {
                    // Create new address
                    address = new UserAddress
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.UserAddresses.Add(address);
                }

                // Update properties
                address.Label = dto.Label ?? "Address";
                address.AddressType = dto.AddressType;
                address.RecipientName = dto.RecipientName;
                address.Phone = dto.Phone;
                address.DivisionId = dto.DivisionId;
                address.DistrictId = dto.DistrictId;
                address.UpazilaId = dto.UpazilaId;
                address.FullAddress = dto.FullAddress;
                address.PostalCode = dto.PostalCode;
                address.IsDefault = dto.IsDefault;
                address.UpdatedAt = DateTime.UtcNow;

                // If this is set as default, unset other defaults
                if (dto.IsDefault)
                {
                    var otherAddresses = await _db.UserAddresses
                        .Where(a => a.UserId == userId && a.Id != address.Id && a.IsDefault)
                        .ToListAsync();
                    foreach (var other in otherAddresses)
                    {
                        other.IsDefault = false;
                    }
                }

                await _db.SaveChangesAsync();

                return Json(new { success = true, addressId = address.Id, message = dto.Id > 0 ? "Address updated successfully" : "Address added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving address");
                return Json(new { success = false, message = "An error occurred while saving the address" });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var address = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address == null)
            {
                return Json(new { success = false, message = "Address not found" });
            }

            _db.UserAddresses.Remove(address);
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Address deleted successfully" });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var address = await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address == null)
            {
                return Json(new { success = false, message = "Address not found" });
            }

            // Unset all other defaults
            var otherAddresses = await _db.UserAddresses
                .Where(a => a.UserId == userId && a.Id != id && a.IsDefault)
                .ToListAsync();
            foreach (var other in otherAddresses)
            {
                other.IsDefault = false;
            }

            address.IsDefault = true;
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Default address updated" });
        }

        private string FormatAddress(UserAddress address)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(address.FullAddress))
                parts.Add(address.FullAddress);
            if (address.Upazila != null)
                parts.Add(address.Upazila.Name);
            if (address.District != null)
                parts.Add(address.District.Name);
            if (address.Division != null)
                parts.Add(address.Division.Name);
            if (!string.IsNullOrEmpty(address.PostalCode))
                parts.Add($"Postal: {address.PostalCode}");
            return string.Join(", ", parts);
        }

        #endregion

        #region Public Shop Page

        /// <summary>
        /// Public shop page - displays seller's shop with products, about, policies, and reviews
        /// </summary>
        [Route("shop/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> Shop(string slug, int page = 1, string? sort = null, int? category = null)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            // Find the seller by slug (any status)
            var seller = await _db.Sellers
                .Include(s => s.User)
                .Include(s => s.BusinessTypeEntity)
                .FirstOrDefaultAsync(s => s.ShopSlug == slug);

            if (seller == null)
            {
                return NotFound();
            }

            // Show "Shop Unavailable" for suspended sellers
            if (seller.Status == SellerStatus.Suspended)
            {
                ViewBag.Seller = seller;
                return View("ShopUnavailable", seller);
            }

            // Only show approved sellers
            if (seller.Status != SellerStatus.Approved)
            {
                return NotFound();
            }

            // Get products query
            var productsQuery = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Where(p => p.SellerId == seller.Id && p.IsAvailable && p.Status == ProductStatus.Active);

            // Apply category filter
            if (category.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == category.Value);
            }

            // Apply sorting
            productsQuery = sort switch
            {
                "price-low" => productsQuery.OrderBy(p => p.Price),
                "price-high" => productsQuery.OrderByDescending(p => p.Price),
                "popular" => productsQuery.OrderByDescending(p => p.SoldCount),
                "rating" => productsQuery.OrderByDescending(p => p.SoldCount), // Sort by sales as proxy for rating
                _ => productsQuery.OrderByDescending(p => p.CreatedAt) // newest first
            };

            // Pagination
            var pageSize = 12;
            var totalProducts = await productsQuery.CountAsync();
            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get categories from this seller's products
            var shopCategories = await _db.Products
                .Where(p => p.SellerId == seller.Id && p.IsAvailable && p.Status == ProductStatus.Active)
                .Select(p => p.Category)
                .Distinct()
                .Where(c => c != null)
                .ToListAsync();

            // Get reviews for this seller's products
            var reviews = await _db.ProductReviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Where(r => r.IsApproved && r.Product.SellerId == seller.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Calculate rating distribution
            var allReviews = await _db.ProductReviews
                .Where(r => r.IsApproved && r.Product.SellerId == seller.Id)
                .Select(r => r.Rating)
                .ToListAsync();

            var ratingDistribution = new Dictionary<int, int>
            {
                { 5, allReviews.Count(r => r == 5) },
                { 4, allReviews.Count(r => r == 4) },
                { 3, allReviews.Count(r => r == 3) },
                { 2, allReviews.Count(r => r == 2) },
                { 1, allReviews.Count(r => r == 1) }
            };

            // Check if current user is following this shop
            var userId = GetCurrentUserId();
            var isFollowing = !string.IsNullOrEmpty(userId) && await _shopFollowService.IsFollowingAsync(userId, seller.Id);

            // Prepare view model
            ViewBag.Seller = seller;
            ViewBag.Products = products;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            ViewBag.CurrentSort = sort;
            ViewBag.CurrentCategory = category;
            ViewBag.ShopCategories = shopCategories;
            ViewBag.Reviews = reviews;
            ViewBag.RatingDistribution = ratingDistribution;
            ViewBag.TotalReviews = allReviews.Count;
            ViewBag.IsFollowing = isFollowing;
            ViewBag.FollowerCount = seller.FollowerCount;

            return View("Shop", seller);
        }

        // POST: Customer/Home/ToggleFollowShop
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFollowShop(int sellerId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login to follow shops." });
            }

            var result = await _shopFollowService.ToggleFollowAsync(userId, sellerId);
            return Json(new
            {
                success = result.Success,
                isFollowing = result.IsFollowing,
                followerCount = result.FollowerCount,
                message = result.Message
            });
        }

        // POST: Customer/Home/FollowShop
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FollowShop(int sellerId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login to follow shops." });
            }

            var result = await _shopFollowService.FollowShopAsync(userId, sellerId);
            return Json(new
            {
                success = result.Success,
                isFollowing = result.IsFollowing,
                followerCount = result.FollowerCount,
                message = result.Message
            });
        }

        // POST: Customer/Home/UnfollowShop
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnfollowShop(int sellerId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login to manage shop follows." });
            }

            var result = await _shopFollowService.UnfollowShopAsync(userId, sellerId);
            var followerCount = await _shopFollowService.GetFollowerCountAsync(sellerId);

            return Json(new
            {
                success = result,
                isFollowing = false,
                followerCount = followerCount,
                message = result ? "You have unfollowed this shop." : "Failed to unfollow."
            });
        }

        // GET: Customer/Home/IsFollowingShop
        [HttpGet]
        public async Task<IActionResult> IsFollowingShop(int sellerId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { isFollowing = false });
            }

            var isFollowing = await _shopFollowService.IsFollowingAsync(userId, sellerId);
            var followerCount = await _shopFollowService.GetFollowerCountAsync(sellerId);

            return Json(new { isFollowing, followerCount });
        }

        // POST: Customer/Home/UpdateFollowSettings
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFollowSettings(int sellerId, bool notifyOnNewProducts, bool notifyOnSales)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login to update settings." });
            }

            var result = await _shopFollowService.UpdateNotificationSettingsAsync(userId, sellerId, notifyOnNewProducts, notifyOnSales);

            return Json(new
            {
                success = result,
                message = result ? "Notification settings updated." : "Failed to update settings."
            });
        }

        // GET: Customer/Home/FollowedShops
        [Authorize]
        public async Task<IActionResult> FollowedShops(int page = 1)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            const int pageSize = 12;
            var followedShops = await _shopFollowService.GetFollowedShopsAsync(userId, page, pageSize);
            var totalCount = await _shopFollowService.GetFollowedShopsCountAsync(userId);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(followedShops);
        }

        #endregion

        #region Helper Methods

        private async Task<List<int>> GetDescendantCategoryIds(int categoryId)
        {
            var ids = new List<int>();
            var children = await _db.Categories
                .Where(c => c.ParentId == categoryId && c.IsActive)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var childId in children)
            {
                ids.Add(childId);
                ids.AddRange(await GetDescendantCategoryIds(childId));
            }

            return ids;
        }

        #endregion

        #region Restock Subscription

        /// <summary>
        /// Subscribe to restock notifications for an out-of-stock product
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubscribeRestock(int productId, int? variantId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login to subscribe for notifications" });
            }

            var success = await _stockAlertService.SubscribeToRestockAsync(userId, productId, variantId);

            if (success)
            {
                return Json(new { success = true, message = "You will be notified when this product is back in stock!" });
            }

            return Json(new { success = false, message = "You are already subscribed for this product" });
        }

        /// <summary>
        /// Unsubscribe from restock notifications
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UnsubscribeRestock(int productId, int? variantId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var success = await _stockAlertService.UnsubscribeFromRestockAsync(userId, productId, variantId);

            if (success)
            {
                return Json(new { success = true, message = "Unsubscribed from restock notifications" });
            }

            return Json(new { success = false, message = "You were not subscribed for this product" });
        }

        /// <summary>
        /// Check if user is subscribed to restock notifications
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> IsSubscribedRestock(int productId, int? variantId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { subscribed = false });
            }

            var isSubscribed = await _stockAlertService.IsSubscribedAsync(userId, productId, variantId);
            return Json(new { subscribed = isSubscribed });
        }

        #endregion
    }

    // DTO for address save
    public class UserAddressDto
    {
        public int Id { get; set; }
        public string? Label { get; set; }
        public AddressType AddressType { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int? DivisionId { get; set; }
        public int? DistrictId { get; set; }
        public int? UpazilaId { get; set; }
        public string FullAddress { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public bool IsDefault { get; set; }
    }
}