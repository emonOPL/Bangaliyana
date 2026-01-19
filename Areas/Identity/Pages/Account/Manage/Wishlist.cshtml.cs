using System;
using System.Collections.Generic;
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
    public class WishlistModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public WishlistModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<WishlistItemViewModel> WishlistItems { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class WishlistItemViewModel
        {
            public int Id { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string? ProductImage { get; set; }
            public decimal Price { get; set; }
            public decimal? DiscountPrice { get; set; }
            public bool HasDiscount { get; set; }
            public bool IsAvailable { get; set; }
            public int Stock { get; set; }
            public string? CategoryName { get; set; }
            public DateTime AddedAt { get; set; }
            public string? Slug { get; set; }

            // Price tracking fields
            public decimal PriceWhenAdded { get; set; }
            public bool NotifyOnPriceDrop { get; set; }

            // Computed properties for price comparison
            public decimal CurrentPrice => DiscountPrice ?? Price;
            public bool HasPriceDropped => CurrentPrice < PriceWhenAdded;
            public decimal PriceDropAmount => HasPriceDropped ? PriceWhenAdded - CurrentPrice : 0;
            public decimal PriceDropPercentage => HasPriceDropped && PriceWhenAdded > 0
                ? Math.Round((PriceDropAmount / PriceWhenAdded) * 100, 1)
                : 0;
            public bool HasPriceIncreased => CurrentPrice > PriceWhenAdded;
            public decimal PriceIncreaseAmount => HasPriceIncreased ? CurrentPrice - PriceWhenAdded : 0;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            WishlistItems = await _context.Wishlists
                .Where(w => w.UserId == user.Id)
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Category)
                .OrderByDescending(w => w.AddedAt)
                .Select(w => new WishlistItemViewModel
                {
                    Id = w.Id,
                    ProductId = w.ProductId,
                    ProductName = w.Product!.Name,
                    ProductImage = w.Product.ImageUrl,
                    Price = w.Product.Price,
                    DiscountPrice = w.Product.DiscountPrice,
                    HasDiscount = w.Product.HasDiscount,
                    IsAvailable = w.Product.IsAvailable && w.Product.Stock > 0,
                    Stock = w.Product.Stock,
                    CategoryName = w.Product.Category != null ? w.Product.Category.Name : null,
                    AddedAt = w.AddedAt,
                    Slug = w.Product.Slug,
                    PriceWhenAdded = w.PriceWhenAdded,
                    NotifyOnPriceDrop = w.NotifyOnPriceDrop
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.Id == id && w.UserId == user.Id);

            if (wishlistItem != null)
            {
                _context.Wishlists.Remove(wishlistItem);
                await _context.SaveChangesAsync();
                StatusMessage = "Item removed from wishlist.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearAllAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var items = await _context.Wishlists
                .Where(w => w.UserId == user.Id)
                .ToListAsync();

            if (items.Any())
            {
                _context.Wishlists.RemoveRange(items);
                await _context.SaveChangesAsync();
                StatusMessage = "Wishlist cleared successfully.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMoveToCartAsync(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // This would integrate with your cart service
            // For now, redirect to product page
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                return Redirect($"/product/{product.Slug}");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostTogglePriceNotificationAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.Id == id && w.UserId == user.Id);

            if (wishlistItem != null)
            {
                wishlistItem.NotifyOnPriceDrop = !wishlistItem.NotifyOnPriceDrop;
                await _context.SaveChangesAsync();
                StatusMessage = wishlistItem.NotifyOnPriceDrop
                    ? "Price drop notifications enabled for this item."
                    : "Price drop notifications disabled for this item.";
            }

            return RedirectToPage();
        }
    }
}
