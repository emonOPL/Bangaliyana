using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public interface IShopFollowService
    {
        /// <summary>
        /// Follow a shop
        /// </summary>
        Task<ShopFollowResult> FollowShopAsync(string userId, int sellerId);

        /// <summary>
        /// Unfollow a shop
        /// </summary>
        Task<bool> UnfollowShopAsync(string userId, int sellerId);

        /// <summary>
        /// Check if user is following a shop
        /// </summary>
        Task<bool> IsFollowingAsync(string userId, int sellerId);

        /// <summary>
        /// Get all shops followed by a user
        /// </summary>
        Task<List<FollowedShopDto>> GetFollowedShopsAsync(string userId, int page = 1, int pageSize = 12);

        /// <summary>
        /// Get total count of followed shops
        /// </summary>
        Task<int> GetFollowedShopsCountAsync(string userId);

        /// <summary>
        /// Get follower count for a seller
        /// </summary>
        Task<int> GetFollowerCountAsync(int sellerId);

        /// <summary>
        /// Update notification settings for a followed shop
        /// </summary>
        Task<bool> UpdateNotificationSettingsAsync(string userId, int sellerId, bool notifyOnNewProducts, bool notifyOnSales);

        /// <summary>
        /// Toggle follow status (follow if not following, unfollow if following)
        /// </summary>
        Task<ShopFollowResult> ToggleFollowAsync(string userId, int sellerId);

        /// <summary>
        /// Notify followers when a new product is added
        /// </summary>
        Task NotifyFollowersNewProductAsync(int sellerId, int productId, string productName);

        /// <summary>
        /// Notify followers when a product's price drops
        /// </summary>
        Task NotifyFollowersPriceDropAsync(int sellerId, int productId, string productName, decimal oldPrice, decimal newPrice);
    }

    public class ShopFollowResult
    {
        public bool Success { get; set; }
        public bool IsFollowing { get; set; }
        public int FollowerCount { get; set; }
        public string? Message { get; set; }
    }

    public class FollowedShopDto
    {
        public int SellerId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string? ShopSlug { get; set; }
        public string? ShopLogoUrl { get; set; }
        public string? ShopDescription { get; set; }
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public int ProductCount { get; set; }
        public int FollowerCount { get; set; }
        public DateTime FollowedAt { get; set; }
        public bool NotifyOnNewProducts { get; set; }
        public bool NotifyOnSales { get; set; }
    }

    public class ShopFollowService : IShopFollowService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public ShopFollowService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ShopFollowResult> FollowShopAsync(string userId, int sellerId)
        {
            // Check if seller exists and is approved
            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller == null)
            {
                return new ShopFollowResult
                {
                    Success = false,
                    Message = "Shop not found."
                };
            }

            // Block following suspended sellers
            if (seller.Status == SellerStatus.Suspended)
            {
                return new ShopFollowResult
                {
                    Success = false,
                    Message = "This shop is currently unavailable."
                };
            }

            // Only allow following approved sellers
            if (seller.Status != SellerStatus.Approved)
            {
                return new ShopFollowResult
                {
                    Success = false,
                    Message = "This shop is not available for following."
                };
            }

            // Check if already following
            var existingFollow = await _context.ShopFollowers
                .FirstOrDefaultAsync(sf => sf.UserId == userId && sf.SellerId == sellerId);

            if (existingFollow != null)
            {
                return new ShopFollowResult
                {
                    Success = true,
                    IsFollowing = true,
                    FollowerCount = seller.FollowerCount,
                    Message = "You are already following this shop."
                };
            }

            // Create new follow
            var shopFollower = new ShopFollower
            {
                UserId = userId,
                SellerId = sellerId,
                FollowedAt = DateTime.UtcNow,
                NotifyOnNewProducts = true,
                NotifyOnSales = true
            };

            _context.ShopFollowers.Add(shopFollower);

            // Increment follower count
            seller.FollowerCount++;

            await _context.SaveChangesAsync();

            // Notify seller about new follower
            if (!string.IsNullOrEmpty(seller.UserId))
            {
                var follower = await _context.Users.FindAsync(userId);
                var followerName = follower?.FullName ?? follower?.Email ?? "A user";

                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = seller.UserId,
                    Type = "seller",
                    Icon = "fa-user-plus",
                    IconColor = "text-success",
                    Title = "New Follower!",
                    Message = $"{followerName} is now following your shop.",
                    Link = "/Seller/Dashboard",
                    CreatedAt = DateTime.UtcNow
                });
            }

            return new ShopFollowResult
            {
                Success = true,
                IsFollowing = true,
                FollowerCount = seller.FollowerCount,
                Message = "You are now following this shop."
            };
        }

        public async Task<bool> UnfollowShopAsync(string userId, int sellerId)
        {
            var shopFollower = await _context.ShopFollowers
                .FirstOrDefaultAsync(sf => sf.UserId == userId && sf.SellerId == sellerId);

            if (shopFollower == null)
            {
                return false;
            }

            _context.ShopFollowers.Remove(shopFollower);

            // Decrement follower count
            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller != null && seller.FollowerCount > 0)
            {
                seller.FollowerCount--;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFollowingAsync(string userId, int sellerId)
        {
            return await _context.ShopFollowers
                .AnyAsync(sf => sf.UserId == userId && sf.SellerId == sellerId);
        }

        public async Task<List<FollowedShopDto>> GetFollowedShopsAsync(string userId, int page = 1, int pageSize = 12)
        {
            var followedShops = await _context.ShopFollowers
                .Include(sf => sf.Seller)
                .Where(sf => sf.UserId == userId && sf.Seller != null && sf.Seller.Status == SellerStatus.Approved)
                .OrderByDescending(sf => sf.FollowedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sf => new FollowedShopDto
                {
                    SellerId = sf.SellerId,
                    ShopName = sf.Seller!.ShopName,
                    ShopSlug = sf.Seller.ShopSlug,
                    ShopLogoUrl = sf.Seller.ShopLogoUrl,
                    ShopDescription = sf.Seller.ShopDescription,
                    Rating = sf.Seller.Rating,
                    ReviewCount = sf.Seller.ReviewCount,
                    ProductCount = _context.Products.Count(p => p.SellerId == sf.SellerId && p.IsAvailable && p.Status == ProductStatus.Active),
                    FollowerCount = sf.Seller.FollowerCount,
                    FollowedAt = sf.FollowedAt,
                    NotifyOnNewProducts = sf.NotifyOnNewProducts,
                    NotifyOnSales = sf.NotifyOnSales
                })
                .ToListAsync();

            return followedShops;
        }

        public async Task<int> GetFollowedShopsCountAsync(string userId)
        {
            return await _context.ShopFollowers
                .Include(sf => sf.Seller)
                .CountAsync(sf => sf.UserId == userId && sf.Seller != null && sf.Seller.Status == SellerStatus.Approved);
        }

        public async Task<int> GetFollowerCountAsync(int sellerId)
        {
            var seller = await _context.Sellers.FindAsync(sellerId);
            return seller?.FollowerCount ?? 0;
        }

        public async Task<bool> UpdateNotificationSettingsAsync(string userId, int sellerId, bool notifyOnNewProducts, bool notifyOnSales)
        {
            var shopFollower = await _context.ShopFollowers
                .FirstOrDefaultAsync(sf => sf.UserId == userId && sf.SellerId == sellerId);

            if (shopFollower == null)
            {
                return false;
            }

            shopFollower.NotifyOnNewProducts = notifyOnNewProducts;
            shopFollower.NotifyOnSales = notifyOnSales;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ShopFollowResult> ToggleFollowAsync(string userId, int sellerId)
        {
            var isFollowing = await IsFollowingAsync(userId, sellerId);

            if (isFollowing)
            {
                var result = await UnfollowShopAsync(userId, sellerId);
                var seller = await _context.Sellers.FindAsync(sellerId);

                return new ShopFollowResult
                {
                    Success = result,
                    IsFollowing = false,
                    FollowerCount = seller?.FollowerCount ?? 0,
                    Message = result ? "You have unfollowed this shop." : "Failed to unfollow."
                };
            }
            else
            {
                return await FollowShopAsync(userId, sellerId);
            }
        }

        public async Task NotifyFollowersNewProductAsync(int sellerId, int productId, string productName)
        {
            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller == null) return;

            // Get all followers who have global "new products from followed shops" notification enabled
            var followers = await _context.ShopFollowers
                .Where(sf => sf.SellerId == sellerId)
                .Join(_context.NotificationPreferences,
                    sf => sf.UserId,
                    np => np.UserId,
                    (sf, np) => new { sf.UserId, np.FollowedShopNewProducts })
                .Where(x => x.FollowedShopNewProducts)
                .Select(x => x.UserId)
                .ToListAsync();

            // Also include users who don't have preferences yet (defaults to enabled)
            var usersWithoutPreferences = await _context.ShopFollowers
                .Where(sf => sf.SellerId == sellerId)
                .Where(sf => !_context.NotificationPreferences.Any(np => np.UserId == sf.UserId))
                .Select(sf => sf.UserId)
                .ToListAsync();

            followers.AddRange(usersWithoutPreferences);
            followers = followers.Distinct().ToList();

            if (!followers.Any()) return;

            var productSlug = await _context.Products
                .Where(p => p.Id == productId)
                .Select(p => p.Slug)
                .FirstOrDefaultAsync();

            foreach (var userId in followers)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = userId,
                    Type = "product",
                    Icon = "fa-box-open",
                    IconColor = "text-primary",
                    Title = "New Product Alert!",
                    Message = $"{seller.ShopName} just added a new product: {productName}",
                    Link = $"/Customer/Home/Details/{productSlug}",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        public async Task NotifyFollowersPriceDropAsync(int sellerId, int productId, string productName, decimal oldPrice, decimal newPrice)
        {
            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller == null) return;

            // Get all followers who have global "price drops from followed shops" notification enabled
            var followers = await _context.ShopFollowers
                .Where(sf => sf.SellerId == sellerId)
                .Join(_context.NotificationPreferences,
                    sf => sf.UserId,
                    np => np.UserId,
                    (sf, np) => new { sf.UserId, np.FollowedShopPriceDrops })
                .Where(x => x.FollowedShopPriceDrops)
                .Select(x => x.UserId)
                .ToListAsync();

            // Also include users who don't have preferences yet (defaults to enabled)
            var usersWithoutPreferences = await _context.ShopFollowers
                .Where(sf => sf.SellerId == sellerId)
                .Where(sf => !_context.NotificationPreferences.Any(np => np.UserId == sf.UserId))
                .Select(sf => sf.UserId)
                .ToListAsync();

            followers.AddRange(usersWithoutPreferences);
            followers = followers.Distinct().ToList();

            if (!followers.Any()) return;

            var productSlug = await _context.Products
                .Where(p => p.Id == productId)
                .Select(p => p.Slug)
                .FirstOrDefaultAsync();

            var discountPercentage = ((oldPrice - newPrice) / oldPrice * 100).ToString("F0");

            foreach (var userId in followers)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = userId,
                    Type = "product",
                    Icon = "fa-tags",
                    IconColor = "text-success",
                    Title = "Price Drop Alert!",
                    Message = $"{productName} from {seller.ShopName} is now {discountPercentage}% off! Was ৳{oldPrice:N0}, now ৳{newPrice:N0}",
                    Link = $"/Customer/Home/Details/{productSlug}",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }
}
