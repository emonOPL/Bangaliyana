using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface IRewardService
    {
        /// <summary>
        /// Get user's current reward points balance
        /// </summary>
        Task<int> GetUserPointsAsync(string userId);

        /// <summary>
        /// Get user's reward points transaction history
        /// </summary>
        Task<List<RewardPointsTransaction>> GetUserTransactionsAsync(string userId, int? limit = null);

        /// <summary>
        /// Get user's reward points transactions with pagination
        /// </summary>
        Task<(List<RewardPointsTransaction> Transactions, int TotalCount)> GetUserTransactionsPaginatedAsync(string userId, int page, int pageSize);

        /// <summary>
        /// Award points for order delivery (1 point per 100 BDT)
        /// </summary>
        Task<bool> AwardOrderDeliveryPointsAsync(int orderId);

        /// <summary>
        /// Award points for product review (10 points for first review with 3+ stars)
        /// </summary>
        Task<bool> AwardProductReviewPointsAsync(int reviewId);

        /// <summary>
        /// Award points for platform review (50 points for first review with 3+ stars)
        /// </summary>
        Task<bool> AwardPlatformReviewPointsAsync(int testimonialId);

        /// <summary>
        /// Generate unique 8-digit referral code for user
        /// </summary>
        Task<string> GenerateReferralCodeAsync(string userId);

        /// <summary>
        /// Apply referral code - gives 200 points to new user and 100 points to referrer
        /// </summary>
        Task<(bool Success, string Message)> ApplyReferralCodeAsync(string userId, string referralCode);

        /// <summary>
        /// Check if user has already reviewed a product
        /// </summary>
        Task<bool> HasUserReviewedProductAsync(string userId, int productId);

        /// <summary>
        /// Check if user has already submitted a platform review
        /// </summary>
        Task<bool> HasUserSubmittedPlatformReviewAsync(string userId);

        /// <summary>
        /// Get referral statistics for a user
        /// </summary>
        Task<(int TotalReferrals, int TotalPointsEarned)> GetReferralStatsAsync(string userId);

        /// <summary>
        /// Get points breakdown by type
        /// </summary>
        Task<Dictionary<RewardTransactionType, int>> GetPointsBreakdownAsync(string userId);

        /// <summary>
        /// Redeem points for a reward - creates a UserRedeemedReward record
        /// </summary>
        Task<(bool Success, string Message)> RedeemPointsAsync(string userId, int rewardId, int pointsRequired, string rewardName);

        /// <summary>
        /// Get all available (not used, not expired) redeemed rewards for a user
        /// </summary>
        Task<List<UserRedeemedReward>> GetAvailableRewardsAsync(string userId);

        /// <summary>
        /// Get all redeemed rewards for a user (including used/expired)
        /// </summary>
        Task<List<UserRedeemedReward>> GetAllRedeemedRewardsAsync(string userId);

        /// <summary>
        /// Convert reward points to wallet balance
        /// Rate: 100 points = ৳1 for general, 50 points = ৳1 for premium
        /// </summary>
        Task<(bool Success, string Message)> ConvertPointsToWalletAsync(string userId, int points);

        /// <summary>
        /// Check and update premium membership status (expire if necessary)
        /// Returns true if user is currently premium
        /// </summary>
        Task<bool> CheckPremiumStatusAsync(string userId);

        /// <summary>
        /// Check if user has active premium 5% discount (within 1 week of activation)
        /// </summary>
        Task<bool> HasActivePremiumDiscountAsync(string userId);

        /// <summary>
        /// Update expired rewards to Expired status
        /// </summary>
        Task UpdateExpiredRewardsAsync();

        /// <summary>
        /// Award daily login points (10 points) - once per day (12:00 AM to 11:59 PM)
        /// </summary>
        Task<(bool Awarded, string Message)> AwardDailyLoginPointsAsync(string userId);
    }
}
