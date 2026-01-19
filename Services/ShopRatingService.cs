using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Service for calculating and managing shop ratings based on product reviews
    /// </summary>
    public interface IShopRatingService
    {
        /// <summary>
        /// Calculate the shop rating from all approved product reviews
        /// </summary>
        Task<decimal> CalculateShopRatingAsync(int sellerId);

        /// <summary>
        /// Update the seller's Rating and ReviewCount fields based on all product reviews
        /// </summary>
        Task UpdateShopRatingAsync(int sellerId);

        /// <summary>
        /// Get the current shop rating and review count
        /// </summary>
        Task<(decimal Rating, int ReviewCount)> GetShopRatingAsync(int sellerId);

        /// <summary>
        /// Recalculate ratings for all sellers (useful for initial setup or data fix)
        /// </summary>
        Task RecalculateAllShopRatingsAsync();
    }

    public class ShopRatingService : IShopRatingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ShopRatingService> _logger;

        public ShopRatingService(ApplicationDbContext context, ILogger<ShopRatingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<decimal> CalculateShopRatingAsync(int sellerId)
        {
            try
            {
                // Get all approved reviews for all products of this seller
                var reviews = await _context.ProductReviews
                    .Where(r => r.IsApproved && r.Product.SellerId == sellerId)
                    .Select(r => r.Rating)
                    .ToListAsync();

                if (!reviews.Any())
                {
                    return 0;
                }

                return Math.Round((decimal)reviews.Average(), 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shop rating for seller {SellerId}", sellerId);
                return 0;
            }
        }

        public async Task UpdateShopRatingAsync(int sellerId)
        {
            try
            {
                var seller = await _context.Sellers.FindAsync(sellerId);
                if (seller == null)
                {
                    _logger.LogWarning("Seller {SellerId} not found when updating shop rating", sellerId);
                    return;
                }

                // Calculate rating stats
                var reviewStats = await _context.ProductReviews
                    .Where(r => r.IsApproved && r.Product.SellerId == sellerId)
                    .GroupBy(r => 1)
                    .Select(g => new
                    {
                        Average = g.Average(r => r.Rating),
                        Count = g.Count()
                    })
                    .FirstOrDefaultAsync();

                if (reviewStats != null)
                {
                    seller.Rating = Math.Round((decimal)reviewStats.Average, 2);
                    seller.ReviewCount = reviewStats.Count;
                }
                else
                {
                    seller.Rating = 0;
                    seller.ReviewCount = 0;
                }

                seller.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated shop rating for seller {SellerId}: Rating={Rating}, ReviewCount={ReviewCount}",
                    sellerId, seller.Rating, seller.ReviewCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shop rating for seller {SellerId}", sellerId);
                throw;
            }
        }

        public async Task<(decimal Rating, int ReviewCount)> GetShopRatingAsync(int sellerId)
        {
            try
            {
                var seller = await _context.Sellers
                    .AsNoTracking()
                    .Where(s => s.Id == sellerId)
                    .Select(s => new { s.Rating, s.ReviewCount })
                    .FirstOrDefaultAsync();

                if (seller == null)
                {
                    return (0, 0);
                }

                return (seller.Rating, seller.ReviewCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shop rating for seller {SellerId}", sellerId);
                return (0, 0);
            }
        }

        public async Task RecalculateAllShopRatingsAsync()
        {
            try
            {
                _logger.LogInformation("Starting recalculation of all shop ratings");

                var sellers = await _context.Sellers.ToListAsync();

                foreach (var seller in sellers)
                {
                    var reviewStats = await _context.ProductReviews
                        .Where(r => r.IsApproved && r.Product.SellerId == seller.Id)
                        .GroupBy(r => 1)
                        .Select(g => new
                        {
                            Average = g.Average(r => r.Rating),
                            Count = g.Count()
                        })
                        .FirstOrDefaultAsync();

                    if (reviewStats != null)
                    {
                        seller.Rating = Math.Round((decimal)reviewStats.Average, 2);
                        seller.ReviewCount = reviewStats.Count;
                    }
                    else
                    {
                        seller.Rating = 0;
                        seller.ReviewCount = 0;
                    }

                    seller.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Completed recalculation of {Count} shop ratings", sellers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating all shop ratings");
                throw;
            }
        }
    }
}
