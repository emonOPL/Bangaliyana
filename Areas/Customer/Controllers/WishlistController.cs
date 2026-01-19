using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Bangaliyana.Data;
using Bangaliyana.Models;

namespace Bangaliyana.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public WishlistController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _userManager = userManager;
            _localizer = localizer;
        }

        // GET: Customer/Wishlist
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var wishlistItems = await _db.Wishlists
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Category)
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Reviews)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();

            return View(wishlistItems);
        }

        // POST: Customer/Wishlist/Toggle
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = _localizer["PleaseLoginToAddToWishlist"].Value, requireLogin = true });
            }

            var product = await _db.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
            }

            var existingItem = await _db.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            bool isInWishlist;
            string message;

            if (existingItem != null)
            {
                _db.Wishlists.Remove(existingItem);
                isInWishlist = false;
                message = _localizer["RemovedFromWishlist"].Value;
            }
            else
            {
                var wishlistItem = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow,
                    PriceWhenAdded = product.DiscountPrice ?? product.Price,
                    NotifyOnPriceDrop = true
                };
                _db.Wishlists.Add(wishlistItem);
                isInWishlist = true;
                message = _localizer["AddedToWishlist"].Value;
            }

            await _db.SaveChangesAsync();

            var wishlistCount = await _db.Wishlists.CountAsync(w => w.UserId == userId);

            return Json(new { success = true, isInWishlist, message, wishlistCount });
        }

        // POST: Customer/Wishlist/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = _localizer["PleaseLoginToAddToWishlist"].Value, requireLogin = true });
            }

            var product = await _db.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
            }

            var existingItem = await _db.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem != null)
            {
                return Json(new { success = true, message = _localizer["AlreadyInWishlist"].Value, isInWishlist = true });
            }

            var wishlistItem = new Wishlist
            {
                UserId = userId,
                ProductId = productId,
                AddedAt = DateTime.UtcNow,
                PriceWhenAdded = product.DiscountPrice ?? product.Price,
                NotifyOnPriceDrop = true
            };
            _db.Wishlists.Add(wishlistItem);
            await _db.SaveChangesAsync();

            var wishlistCount = await _db.Wishlists.CountAsync(w => w.UserId == userId);

            return Json(new { success = true, message = _localizer["AddedToWishlist"].Value, isInWishlist = true, wishlistCount });
        }

        // POST: Customer/Wishlist/TogglePriceNotification
        [HttpPost]
        public async Task<IActionResult> TogglePriceNotification(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = _localizer["PleaseLoginFirst"].Value, requireLogin = true });
            }

            var wishlistItem = await _db.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (wishlistItem == null)
            {
                return Json(new { success = false, message = _localizer["ItemNotInWishlist"].Value });
            }

            wishlistItem.NotifyOnPriceDrop = !wishlistItem.NotifyOnPriceDrop;
            await _db.SaveChangesAsync();

            var message = wishlistItem.NotifyOnPriceDrop
                ? _localizer["PriceDropNotificationsEnabled"].Value
                : _localizer["PriceDropNotificationsDisabled"].Value;

            return Json(new { success = true, message, notifyOnPriceDrop = wishlistItem.NotifyOnPriceDrop });
        }

        // POST: Customer/Wishlist/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = _localizer["PleaseLoginFirst"].Value, requireLogin = true });
            }

            var wishlistItem = await _db.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (wishlistItem == null)
            {
                return Json(new { success = true, message = _localizer["ItemNotInWishlist"].Value, isInWishlist = false });
            }

            _db.Wishlists.Remove(wishlistItem);
            await _db.SaveChangesAsync();

            var wishlistCount = await _db.Wishlists.CountAsync(w => w.UserId == userId);

            return Json(new { success = true, message = _localizer["RemovedFromWishlist"].Value, isInWishlist = false, wishlistCount });
        }

        // GET: Customer/Wishlist/Check
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Check(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { isInWishlist = false });
            }

            var isInWishlist = await _db.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            return Json(new { isInWishlist });
        }

        // GET: Customer/Wishlist/Count
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Count()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { count = 0 });
            }

            var count = await _db.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { count });
        }
    }
}
