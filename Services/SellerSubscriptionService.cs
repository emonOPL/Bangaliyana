using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public class SellerSubscriptionService : ISellerSubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SellerSubscriptionService> _logger;

        public SellerSubscriptionService(ApplicationDbContext context, ILogger<SellerSubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Plan Management

        public async Task<List<SellerSubscriptionPlan>> GetAllPlansAsync(bool includeInactive = false)
        {
            var query = _context.SellerSubscriptionPlans.AsQueryable();

            if (!includeInactive)
                query = query.Where(p => p.IsActive);

            return await query.OrderBy(p => p.SortOrder).ThenBy(p => p.MonthlyPrice).ToListAsync();
        }

        public async Task<SellerSubscriptionPlan?> GetPlanByIdAsync(int planId)
        {
            return await _context.SellerSubscriptionPlans
                .Include(p => p.Subscriptions)
                .FirstOrDefaultAsync(p => p.Id == planId);
        }

        public async Task<SellerSubscriptionPlan> CreatePlanAsync(SellerSubscriptionPlan plan)
        {
            plan.CreatedAt = DateTime.UtcNow;
            plan.UpdatedAt = DateTime.UtcNow;

            _context.SellerSubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created subscription plan: {PlanName}", plan.Name);
            return plan;
        }

        public async Task<SellerSubscriptionPlan> UpdatePlanAsync(SellerSubscriptionPlan plan)
        {
            var existing = await _context.SellerSubscriptionPlans.FindAsync(plan.Id);
            if (existing == null)
                throw new InvalidOperationException("Plan not found");

            existing.Name = plan.Name;
            existing.Description = plan.Description;
            existing.MonthlyPrice = plan.MonthlyPrice;
            existing.MaxProducts = plan.MaxProducts;
            existing.CommissionRate = plan.CommissionRate;
            existing.HasPrioritySupport = plan.HasPrioritySupport;
            existing.HasFeaturedListing = plan.HasFeaturedListing;
            existing.HasAdvancedAnalytics = plan.HasAdvancedAnalytics;
            existing.HasPromotionalTools = plan.HasPromotionalTools;
            existing.SortOrder = plan.SortOrder;
            existing.BadgeColor = plan.BadgeColor;
            existing.IconClass = plan.IconClass;
            existing.IsActive = plan.IsActive;
            existing.IsFeatured = plan.IsFeatured;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated subscription plan: {PlanId}", plan.Id);
            return existing;
        }

        public async Task<bool> DeletePlanAsync(int planId)
        {
            var plan = await _context.SellerSubscriptionPlans
                .Include(p => p.Subscriptions)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null)
                return false;

            // Check if there are active subscriptions
            if (plan.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active))
            {
                _logger.LogWarning("Cannot delete plan {PlanId} - has active subscriptions", planId);
                return false;
            }

            _context.SellerSubscriptionPlans.Remove(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted subscription plan: {PlanId}", planId);
            return true;
        }

        public async Task<bool> TogglePlanStatusAsync(int planId)
        {
            var plan = await _context.SellerSubscriptionPlans.FindAsync(planId);
            if (plan == null)
                return false;

            plan.IsActive = !plan.IsActive;
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Subscription Management

        public async Task<SellerSubscription?> GetActiveSubscriptionAsync(int sellerId)
        {
            return await _context.SellerSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.SellerId == sellerId && s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<SellerSubscription>> GetSellerSubscriptionHistoryAsync(int sellerId)
        {
            return await _context.SellerSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.SellerId == sellerId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<SellerSubscription> CreateSubscriptionAsync(int sellerId, int planId, string? paymentMethod = null, string? transactionId = null)
        {
            var plan = await _context.SellerSubscriptionPlans.FindAsync(planId);
            if (plan == null || !plan.IsActive)
                throw new InvalidOperationException("Plan not found or inactive");

            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller == null)
                throw new InvalidOperationException("Seller not found");

            // Cancel any existing active subscriptions
            var existingSubscription = await GetActiveSubscriptionAsync(sellerId);
            if (existingSubscription != null)
            {
                existingSubscription.Status = SubscriptionStatus.Cancelled;
                existingSubscription.CancelledAt = DateTime.UtcNow;
                existingSubscription.CancellationReason = "Upgraded to new plan";
                existingSubscription.UpdatedAt = DateTime.UtcNow;
            }

            var subscription = new SellerSubscription
            {
                SellerId = sellerId,
                PlanId = planId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                Status = plan.MonthlyPrice == 0 ? SubscriptionStatus.Active : SubscriptionStatus.Active, // For free plan, auto-activate
                AmountPaid = plan.MonthlyPrice,
                PaymentMethod = paymentMethod,
                TransactionId = transactionId,
                IsTrial = plan.MonthlyPrice == 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SellerSubscriptions.Add(subscription);

            // Update seller's current subscription and commission rate
            seller.CurrentSubscriptionId = null; // Will be set after save
            seller.CommissionRate = plan.CommissionRate;
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update seller's current subscription ID
            seller.CurrentSubscriptionId = subscription.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created subscription for seller {SellerId} with plan {PlanId}", sellerId, planId);
            return subscription;
        }

        public async Task<bool> CancelSubscriptionAsync(int subscriptionId, string? reason = null)
        {
            var subscription = await _context.SellerSubscriptions
                .Include(s => s.Seller)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
                return false;

            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.CancellationReason = reason;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Reset seller to default commission rate
            if (subscription.Seller != null)
            {
                subscription.Seller.CurrentSubscriptionId = null;
                subscription.Seller.CommissionRate = 5m; // Default commission rate
                subscription.Seller.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cancelled subscription {SubscriptionId}", subscriptionId);
            return true;
        }

        public async Task<bool> RenewSubscriptionAsync(int subscriptionId, string? paymentMethod = null, string? transactionId = null)
        {
            var subscription = await _context.SellerSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null || subscription.Plan == null)
                return false;

            subscription.StartDate = DateTime.UtcNow;
            subscription.EndDate = DateTime.UtcNow.AddMonths(1);
            subscription.Status = SubscriptionStatus.Active;
            subscription.AmountPaid = subscription.Plan.MonthlyPrice;
            subscription.PaymentMethod = paymentMethod;
            subscription.TransactionId = transactionId;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Renewed subscription {SubscriptionId}", subscriptionId);
            return true;
        }

        #endregion

        #region Subscription Checks

        public async Task<bool> IsSubscriptionActiveAsync(int sellerId)
        {
            var subscription = await GetActiveSubscriptionAsync(sellerId);
            return subscription != null;
        }

        public async Task<bool> CanAddProductAsync(int sellerId)
        {
            var subscription = await GetActiveSubscriptionAsync(sellerId);
            if (subscription?.Plan == null)
            {
                // No subscription - check if under free tier limit
                var productCount = await _context.Products.CountAsync(p => p.SellerId == sellerId);
                return productCount < 10; // Default free tier limit
            }

            if (subscription.Plan.MaxProducts == -1)
                return true; // Unlimited

            var currentProductCount = await _context.Products.CountAsync(p => p.SellerId == sellerId);
            return currentProductCount < subscription.Plan.MaxProducts;
        }

        public async Task<int> GetRemainingProductSlotsAsync(int sellerId)
        {
            var subscription = await GetActiveSubscriptionAsync(sellerId);
            var currentProductCount = await _context.Products.CountAsync(p => p.SellerId == sellerId);

            if (subscription?.Plan == null)
            {
                return Math.Max(0, 10 - currentProductCount); // Default free tier limit
            }

            if (subscription.Plan.MaxProducts == -1)
                return int.MaxValue; // Unlimited

            return Math.Max(0, subscription.Plan.MaxProducts - currentProductCount);
        }

        public async Task<decimal> GetCurrentCommissionRateAsync(int sellerId)
        {
            var subscription = await GetActiveSubscriptionAsync(sellerId);
            if (subscription?.Plan != null)
                return subscription.Plan.CommissionRate;

            // Default commission rate if no active subscription
            var seller = await _context.Sellers.FindAsync(sellerId);
            return seller?.CommissionRate ?? 5m;
        }

        public async Task<SellerSubscriptionPlan?> GetCurrentPlanAsync(int sellerId)
        {
            var subscription = await GetActiveSubscriptionAsync(sellerId);
            return subscription?.Plan;
        }

        #endregion

        #region Statistics

        public async Task<int> GetTotalSubscribersCountAsync()
        {
            return await _context.SellerSubscriptions
                .Select(s => s.SellerId)
                .Distinct()
                .CountAsync();
        }

        public async Task<int> GetActiveSubscribersCountAsync()
        {
            return await _context.SellerSubscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow)
                .Select(s => s.SellerId)
                .Distinct()
                .CountAsync();
        }

        public async Task<decimal> GetTotalSubscriptionRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.SellerSubscriptions.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(s => s.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.CreatedAt <= toDate.Value);

            return await query.SumAsync(s => s.AmountPaid);
        }

        public async Task<Dictionary<string, int>> GetSubscriptionsByPlanAsync()
        {
            return await _context.SellerSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow)
                .GroupBy(s => s.Plan!.Name)
                .Select(g => new { PlanName = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PlanName, x => x.Count);
        }

        #endregion
    }
}
