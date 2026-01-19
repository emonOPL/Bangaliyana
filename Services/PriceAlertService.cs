using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public interface IPriceAlertService
    {
        Task RecordPriceChangeAsync(int productId, decimal oldPrice, decimal newPrice, decimal? oldDiscountPrice, decimal? newDiscountPrice, string? changedByUserId);
        Task ProcessPriceDropNotificationsAsync(int productId);
        Task<List<PriceHistory>> GetPriceHistoryAsync(int productId, int count = 10);
        Task TogglePriceNotificationAsync(int wishlistId, bool notify);
        Task UpdateWishlistPricesOnAddAsync(int wishlistId, decimal currentPrice);
    }

    public class PriceAlertService : IPriceAlertService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PriceAlertService> _logger;

        public PriceAlertService(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<PriceAlertService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task RecordPriceChangeAsync(int productId, decimal oldPrice, decimal newPrice, decimal? oldDiscountPrice, decimal? newDiscountPrice, string? changedByUserId)
        {
            // Calculate effective prices (discount price if available, otherwise regular price)
            var oldEffective = oldDiscountPrice ?? oldPrice;
            var newEffective = newDiscountPrice ?? newPrice;

            // Skip if no change in effective price
            if (oldEffective == newEffective)
            {
                return;
            }

            // Determine change type
            var changeType = DetermineChangeType(oldPrice, newPrice, oldDiscountPrice, newDiscountPrice);

            // Calculate percentage change
            var percentageChange = oldEffective > 0
                ? Math.Round((newEffective - oldEffective) / oldEffective * 100, 2)
                : 0;

            var priceHistory = new PriceHistory
            {
                ProductId = productId,
                OldPrice = oldPrice,
                NewPrice = newPrice,
                OldDiscountPrice = oldDiscountPrice,
                NewDiscountPrice = newDiscountPrice,
                OldEffectivePrice = oldEffective,
                NewEffectivePrice = newEffective,
                ChangeType = changeType,
                PercentageChange = percentageChange,
                ChangedByUserId = changedByUserId,
                ChangedAt = DateTime.UtcNow
            };

            _context.PriceHistories.Add(priceHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Price change recorded for product {ProductId}: {OldPrice} -> {NewPrice} ({PercentageChange}%)",
                productId, oldEffective, newEffective, percentageChange);

            // If price dropped, process notifications
            if (newEffective < oldEffective)
            {
                await ProcessPriceDropNotificationsAsync(productId);
            }
        }

        private PriceChangeType DetermineChangeType(decimal oldPrice, decimal newPrice, decimal? oldDiscount, decimal? newDiscount)
        {
            // Discount added
            if (!oldDiscount.HasValue && newDiscount.HasValue)
            {
                return PriceChangeType.DiscountAdded;
            }

            // Discount removed
            if (oldDiscount.HasValue && !newDiscount.HasValue)
            {
                return PriceChangeType.DiscountRemoved;
            }

            // Both have discounts - compare them
            if (oldDiscount.HasValue && newDiscount.HasValue)
            {
                if (newDiscount < oldDiscount)
                    return PriceChangeType.DiscountIncreased;
                if (newDiscount > oldDiscount)
                    return PriceChangeType.DiscountDecreased;
            }

            // Base price changed (when no discounts)
            var oldEffective = oldDiscount ?? oldPrice;
            var newEffective = newDiscount ?? newPrice;

            return newEffective < oldEffective ? PriceChangeType.PriceDecrease : PriceChangeType.PriceIncrease;
        }

        public async Task ProcessPriceDropNotificationsAsync(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return;

            var currentPrice = product.EffectivePrice;

            // Get wishlist items with price drop notifications enabled
            // and where the current price is lower than when they added it
            var wishlistItems = await _context.Wishlists
                .Include(w => w.User)
                .Where(w => w.ProductId == productId &&
                           w.NotifyOnPriceDrop &&
                           w.PriceWhenAdded > currentPrice &&
                           (w.LastNotifiedPrice == null || w.LastNotifiedPrice > currentPrice))
                .ToListAsync();

            foreach (var item in wishlistItems)
            {
                // Check user preference
                var preference = await _context.NotificationPreferences
                    .FirstOrDefaultAsync(np => np.UserId == item.UserId);

                if (preference != null && !preference.PriceDrops)
                {
                    continue;
                }

                var priceDrop = item.PriceWhenAdded - currentPrice;
                var dropPercentage = Math.Round(priceDrop / item.PriceWhenAdded * 100, 1);

                // Create notification
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = item.UserId,
                    Type = "pricedrop",
                    Icon = "fa-arrow-down",
                    IconColor = "text-success",
                    Title = "Price Drop Alert!",
                    Message = $"Good news! \"{product.Name}\" in your wishlist dropped from ৳{item.PriceWhenAdded:N0} to ৳{currentPrice:N0} ({dropPercentage}% off)",
                    Link = $"/Customer/Home/Detail/{productId}",
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                });

                // Update last notified price
                item.LastNotifiedPrice = currentPrice;
                item.LastNotifiedAt = DateTime.UtcNow;

                _logger.LogInformation("Price drop notification sent to user {UserId} for product {ProductId}",
                    item.UserId, productId);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<PriceHistory>> GetPriceHistoryAsync(int productId, int count = 10)
        {
            return await _context.PriceHistories
                .Where(ph => ph.ProductId == productId)
                .OrderByDescending(ph => ph.ChangedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task TogglePriceNotificationAsync(int wishlistId, bool notify)
        {
            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.Id == wishlistId);

            if (wishlistItem == null) return;

            wishlistItem.NotifyOnPriceDrop = notify;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateWishlistPricesOnAddAsync(int wishlistId, decimal currentPrice)
        {
            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.Id == wishlistId);

            if (wishlistItem == null) return;

            wishlistItem.PriceWhenAdded = currentPrice;
            wishlistItem.NotifyOnPriceDrop = true;
            await _context.SaveChangesAsync();
        }
    }
}
