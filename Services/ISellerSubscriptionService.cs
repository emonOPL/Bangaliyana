using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface ISellerSubscriptionService
    {
        // Plan Management
        Task<List<SellerSubscriptionPlan>> GetAllPlansAsync(bool includeInactive = false);
        Task<SellerSubscriptionPlan?> GetPlanByIdAsync(int planId);
        Task<SellerSubscriptionPlan> CreatePlanAsync(SellerSubscriptionPlan plan);
        Task<SellerSubscriptionPlan> UpdatePlanAsync(SellerSubscriptionPlan plan);
        Task<bool> DeletePlanAsync(int planId);
        Task<bool> TogglePlanStatusAsync(int planId);

        // Subscription Management
        Task<SellerSubscription?> GetActiveSubscriptionAsync(int sellerId);
        Task<List<SellerSubscription>> GetSellerSubscriptionHistoryAsync(int sellerId);
        Task<SellerSubscription> CreateSubscriptionAsync(int sellerId, int planId, string? paymentMethod = null, string? transactionId = null);
        Task<bool> CancelSubscriptionAsync(int subscriptionId, string? reason = null);
        Task<bool> RenewSubscriptionAsync(int subscriptionId, string? paymentMethod = null, string? transactionId = null);

        // Subscription Checks
        Task<bool> IsSubscriptionActiveAsync(int sellerId);
        Task<bool> CanAddProductAsync(int sellerId);
        Task<int> GetRemainingProductSlotsAsync(int sellerId);
        Task<decimal> GetCurrentCommissionRateAsync(int sellerId);
        Task<SellerSubscriptionPlan?> GetCurrentPlanAsync(int sellerId);

        // Statistics
        Task<int> GetTotalSubscribersCountAsync();
        Task<int> GetActiveSubscribersCountAsync();
        Task<decimal> GetTotalSubscriptionRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<Dictionary<string, int>> GetSubscriptionsByPlanAsync();
    }
}
