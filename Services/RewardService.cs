using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public class RewardService : IRewardService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RewardService> _logger;

        // Points configuration
        private const int PointsPerHundredBDT = 1;
        private const int ProductReviewPoints = 10;
        private const int PlatformReviewPoints = 50;
        private const int ReferrerBonusPoints = 100;
        private const int ReferredBonusPoints = 200;
        private const int MinRatingForPoints = 3;
        private const int DailyLoginPoints = 10;

        public RewardService(ApplicationDbContext db, ILogger<RewardService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> GetUserPointsAsync(string userId)
        {
            var user = await _db.Users.FindAsync(userId);
            return user?.TotalRewardPoints ?? 0;
        }

        public async Task<List<RewardPointsTransaction>> GetUserTransactionsAsync(string userId, int? limit = null)
        {
            var query = _db.RewardPointsTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .AsQueryable();

            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<(List<RewardPointsTransaction> Transactions, int TotalCount)> GetUserTransactionsPaginatedAsync(string userId, int page, int pageSize)
        {
            var query = _db.RewardPointsTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync();
            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (transactions, totalCount);
        }

        public async Task<bool> AwardOrderDeliveryPointsAsync(int orderId)
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for delivery points", orderId);
                    return false;
                }

                if (string.IsNullOrEmpty(order.UserId))
                {
                    _logger.LogInformation("Order {OrderId} is a guest order, no points awarded", orderId);
                    return false;
                }

                if (order.DeliveryPointsAwarded)
                {
                    _logger.LogInformation("Delivery points already awarded for order {OrderId}", orderId);
                    return false;
                }

                // Calculate points: 1 point per 100 BDT
                var pointsToAward = (int)(order.TotalAmount / 100) * PointsPerHundredBDT;
                if (pointsToAward <= 0)
                {
                    _logger.LogInformation("Order {OrderId} total {Total} too low for points", orderId, order.TotalAmount);
                    return false;
                }

                var user = order.User;
                if (user == null)
                {
                    user = await _db.Users.FindAsync(order.UserId);
                    if (user == null)
                    {
                        _logger.LogWarning("User {UserId} not found for order {OrderId}", order.UserId, orderId);
                        return false;
                    }
                }

                // Update user balance
                user.TotalRewardPoints += pointsToAward;

                // Create transaction record
                var transaction = new RewardPointsTransaction
                {
                    UserId = order.UserId,
                    TransactionType = RewardTransactionType.OrderDelivery,
                    Points = pointsToAward,
                    BalanceAfter = user.TotalRewardPoints,
                    Description = $"Order #{order.OrderNumber} delivery bonus",
                    OrderId = order.Id,
                    CreatedAt = DateTime.UtcNow
                };

                // Update order
                order.DeliveryPointsAwarded = true;
                order.DeliveryPointsAwardedAt = DateTime.UtcNow;
                order.DeliveryPointsAmount = pointsToAward;

                _db.RewardPointsTransactions.Add(transaction);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Awarded {Points} delivery points to user {UserId} for order {OrderId}",
                    pointsToAward, order.UserId, orderId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding delivery points for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> AwardProductReviewPointsAsync(int reviewId)
        {
            try
            {
                var review = await _db.ProductReviews
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == reviewId);

                if (review == null)
                {
                    _logger.LogWarning("Review {ReviewId} not found", reviewId);
                    return false;
                }

                if (review.PointsAwarded)
                {
                    _logger.LogInformation("Points already awarded for review {ReviewId}", reviewId);
                    return false;
                }

                // Only award points for 3+ star reviews
                if (review.Rating < MinRatingForPoints)
                {
                    _logger.LogInformation("Review {ReviewId} rating {Rating} below minimum for points",
                        reviewId, review.Rating);
                    return false;
                }

                var user = review.User;
                if (user == null)
                {
                    user = await _db.Users.FindAsync(review.UserId);
                    if (user == null)
                    {
                        _logger.LogWarning("User {UserId} not found for review {ReviewId}", review.UserId, reviewId);
                        return false;
                    }
                }

                // Update user balance
                user.TotalRewardPoints += ProductReviewPoints;

                // Create transaction record
                var transaction = new RewardPointsTransaction
                {
                    UserId = review.UserId,
                    TransactionType = RewardTransactionType.ProductReview,
                    Points = ProductReviewPoints,
                    BalanceAfter = user.TotalRewardPoints,
                    Description = "Product review bonus",
                    ProductReviewId = review.Id,
                    CreatedAt = DateTime.UtcNow
                };

                // Update review
                review.PointsAwarded = true;
                review.PointsAwardedAt = DateTime.UtcNow;

                _db.RewardPointsTransactions.Add(transaction);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Awarded {Points} product review points to user {UserId} for review {ReviewId}",
                    ProductReviewPoints, review.UserId, reviewId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding product review points for review {ReviewId}", reviewId);
                return false;
            }
        }

        public async Task<bool> AwardPlatformReviewPointsAsync(int testimonialId)
        {
            try
            {
                var testimonial = await _db.Testimonials
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == testimonialId);

                if (testimonial == null)
                {
                    _logger.LogWarning("Testimonial {TestimonialId} not found", testimonialId);
                    return false;
                }

                if (string.IsNullOrEmpty(testimonial.UserId))
                {
                    _logger.LogInformation("Testimonial {TestimonialId} is admin-added, no points awarded", testimonialId);
                    return false;
                }

                if (testimonial.PointsAwarded)
                {
                    _logger.LogInformation("Points already awarded for testimonial {TestimonialId}", testimonialId);
                    return false;
                }

                // Only award points for 3+ star reviews
                if (testimonial.Rating < MinRatingForPoints)
                {
                    _logger.LogInformation("Testimonial {TestimonialId} rating {Rating} below minimum for points",
                        testimonialId, testimonial.Rating);
                    return false;
                }

                var user = testimonial.User;
                if (user == null)
                {
                    user = await _db.Users.FindAsync(testimonial.UserId);
                    if (user == null)
                    {
                        _logger.LogWarning("User {UserId} not found for testimonial {TestimonialId}",
                            testimonial.UserId, testimonialId);
                        return false;
                    }
                }

                // Update user balance
                user.TotalRewardPoints += PlatformReviewPoints;

                // Create transaction record
                var transaction = new RewardPointsTransaction
                {
                    UserId = testimonial.UserId,
                    TransactionType = RewardTransactionType.PlatformReview,
                    Points = PlatformReviewPoints,
                    BalanceAfter = user.TotalRewardPoints,
                    Description = "Platform review bonus",
                    TestimonialId = testimonial.Id,
                    CreatedAt = DateTime.UtcNow
                };

                // Update testimonial
                testimonial.PointsAwarded = true;
                testimonial.PointsAwardedAt = DateTime.UtcNow;

                _db.RewardPointsTransactions.Add(transaction);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Awarded {Points} platform review points to user {UserId} for testimonial {TestimonialId}",
                    PlatformReviewPoints, testimonial.UserId, testimonialId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding platform review points for testimonial {TestimonialId}", testimonialId);
                return false;
            }
        }

        public async Task<string> GenerateReferralCodeAsync(string userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for referral code generation", userId);
                    return string.Empty;
                }

                // If user already has a code, return it
                if (!string.IsNullOrEmpty(user.ReferralCode))
                {
                    return user.ReferralCode;
                }

                // Generate unique 8-digit code
                string code;
                int attempts = 0;
                const int maxAttempts = 10;

                do
                {
                    code = GenerateRandomDigits(8);
                    attempts++;
                } while (await _db.Users.AnyAsync(u => u.ReferralCode == code) && attempts < maxAttempts);

                if (attempts >= maxAttempts)
                {
                    _logger.LogError("Failed to generate unique referral code after {Attempts} attempts", maxAttempts);
                    return string.Empty;
                }

                user.ReferralCode = code;
                user.ReferralCodeGeneratedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Generated referral code {Code} for user {UserId}", code, userId);
                return code;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating referral code for user {UserId}", userId);
                return string.Empty;
            }
        }

        public async Task<(bool Success, string Message)> ApplyReferralCodeAsync(string userId, string referralCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(referralCode))
                {
                    return (false, "Referral code is required");
                }

                referralCode = referralCode.Trim();

                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "User not found");
                }

                // Check if user already used a referral code
                if (user.HasUsedReferralCode)
                {
                    return (false, "You have already used a referral code");
                }

                // Find the referrer
                var referrer = await _db.Users.FirstOrDefaultAsync(u => u.ReferralCode == referralCode);
                if (referrer == null)
                {
                    return (false, "Invalid referral code");
                }

                // Prevent self-referral
                if (referrer.Id == userId)
                {
                    return (false, "You cannot use your own referral code");
                }

                // Award points to new user (200 points)
                user.TotalRewardPoints += ReferredBonusPoints;
                user.HasUsedReferralCode = true;
                user.ReferredByUserId = referrer.Id;

                var newUserTransaction = new RewardPointsTransaction
                {
                    UserId = userId,
                    TransactionType = RewardTransactionType.ReferredBonus,
                    Points = ReferredBonusPoints,
                    BalanceAfter = user.TotalRewardPoints,
                    Description = "Referral signup bonus",
                    ReferredUserId = referrer.Id,
                    CreatedAt = DateTime.UtcNow
                };

                // Award points to referrer (100 points)
                referrer.TotalRewardPoints += ReferrerBonusPoints;

                var referrerTransaction = new RewardPointsTransaction
                {
                    UserId = referrer.Id,
                    TransactionType = RewardTransactionType.ReferralBonus,
                    Points = ReferrerBonusPoints,
                    BalanceAfter = referrer.TotalRewardPoints,
                    Description = "Referral bonus",
                    ReferredUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.RewardPointsTransactions.Add(newUserTransaction);
                _db.RewardPointsTransactions.Add(referrerTransaction);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Applied referral code {Code}. New user {NewUserId} got {NewUserPoints} points, Referrer {ReferrerId} got {ReferrerPoints} points",
                    referralCode, userId, ReferredBonusPoints, referrer.Id, ReferrerBonusPoints);

                return (true, $"Referral code applied! You received {ReferredBonusPoints} bonus points.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying referral code {Code} for user {UserId}", referralCode, userId);
                return (false, "An error occurred while applying the referral code");
            }
        }

        public async Task<bool> HasUserReviewedProductAsync(string userId, int productId)
        {
            return await _db.ProductReviews
                .AnyAsync(r => r.UserId == userId && r.ProductId == productId);
        }

        public async Task<bool> HasUserSubmittedPlatformReviewAsync(string userId)
        {
            return await _db.Testimonials
                .AnyAsync(t => t.UserId == userId && t.IsUserSubmitted);
        }

        public async Task<(int TotalReferrals, int TotalPointsEarned)> GetReferralStatsAsync(string userId)
        {
            var referralTransactions = await _db.RewardPointsTransactions
                .Where(t => t.UserId == userId && t.TransactionType == RewardTransactionType.ReferralBonus)
                .ToListAsync();

            return (referralTransactions.Count, referralTransactions.Sum(t => t.Points));
        }

        public async Task<Dictionary<RewardTransactionType, int>> GetPointsBreakdownAsync(string userId)
        {
            var transactions = await _db.RewardPointsTransactions
                .Where(t => t.UserId == userId && t.Points > 0)
                .GroupBy(t => t.TransactionType)
                .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Points) })
                .ToListAsync();

            return transactions.ToDictionary(t => t.Type, t => t.Total);
        }

        public async Task<(bool Success, string Message)> RedeemPointsAsync(string userId, int rewardId, int pointsRequired, string rewardName)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "User not found");
                }

                if (user.TotalRewardPoints < pointsRequired)
                {
                    return (false, $"Insufficient points. You need {pointsRequired} points but only have {user.TotalRewardPoints}.");
                }

                // Determine reward type and value based on rewardId
                var rewardType = rewardId switch
                {
                    3 => RewardType.FreeShipping,
                    5 => RewardType.PremiumMembership,
                    _ => RewardType.DiscountVoucher
                };

                decimal? discountAmount = rewardId switch
                {
                    1 => 50m,
                    2 => 100m,
                    4 => 200m,
                    6 => 500m,
                    _ => null
                };

                decimal? minimumOrderAmount = rewardId switch
                {
                    1 => 500m,
                    2 => 1000m,
                    4 => 2000m,
                    6 => 5000m,
                    _ => null
                };

                // Deduct points
                user.TotalRewardPoints -= pointsRequired;

                string successMessage;

                // For Premium Membership, update user status directly
                if (rewardType == RewardType.PremiumMembership)
                {
                    user.IsPremiumMember = true;

                    // Stack premium duration: if already premium, add 30 days to existing expiry
                    if (user.PremiumExpiresAt.HasValue && user.PremiumExpiresAt.Value > DateTime.UtcNow)
                    {
                        // Already premium - extend the expiry by 30 days
                        user.PremiumExpiresAt = user.PremiumExpiresAt.Value.AddDays(30);
                        // Also reset the 5% discount to 7 days from now (new activation benefit)
                        user.PremiumDiscountExpiresAt = DateTime.UtcNow.AddDays(7);
                        successMessage = $"Premium Membership extended! Your premium now expires on {user.PremiumExpiresAt.Value:MMM dd, yyyy}. 5% discount reset for 1 week.";
                    }
                    else
                    {
                        // New premium activation
                        user.PremiumExpiresAt = DateTime.UtcNow.AddDays(30);
                        user.PremiumDiscountExpiresAt = DateTime.UtcNow.AddDays(7); // 5% discount for 1 week
                        successMessage = $"Premium Member activated! You now have free shipping, 2x points for 30 days, and 5% discount for 1 week.";
                    }
                }
                else
                {
                    // Create UserRedeemedReward record for Free Shipping and Discount vouchers
                    var redeemedReward = new UserRedeemedReward
                    {
                        UserId = userId,
                        RewardType = rewardType,
                        RewardName = rewardName,
                        DiscountAmount = discountAmount,
                        MinimumOrderAmount = minimumOrderAmount,
                        PointsSpent = pointsRequired,
                        Status = RewardStatus.Available,
                        RedeemedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(30)
                    };

                    _db.UserRedeemedRewards.Add(redeemedReward);
                    successMessage = $"Successfully redeemed {rewardName}! Valid for 30 days.";
                }

                // Create transaction record
                var transaction = new RewardPointsTransaction
                {
                    UserId = userId,
                    TransactionType = RewardTransactionType.Redemption,
                    Points = -pointsRequired,
                    BalanceAfter = user.TotalRewardPoints,
                    Description = $"Redeemed: {rewardName}",
                    CreatedAt = DateTime.UtcNow
                };

                _db.RewardPointsTransactions.Add(transaction);
                await _db.SaveChangesAsync();

                _logger.LogInformation("User {UserId} redeemed {Points} points for reward {RewardName}",
                    userId, pointsRequired, rewardName);

                return (true, successMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redeeming points for user {UserId}", userId);
                return (false, "An error occurred while redeeming points");
            }
        }

        public async Task<List<UserRedeemedReward>> GetAvailableRewardsAsync(string userId)
        {
            var now = DateTime.UtcNow;
            return await _db.UserRedeemedRewards
                .Where(r => r.UserId == userId
                    && r.Status == RewardStatus.Available
                    && r.ExpiresAt > now)
                .OrderBy(r => r.ExpiresAt)
                .ToListAsync();
        }

        public async Task<List<UserRedeemedReward>> GetAllRedeemedRewardsAsync(string userId)
        {
            return await _db.UserRedeemedRewards
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RedeemedAt)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> ConvertPointsToWalletAsync(string userId, int points)
        {
            try
            {
                if (points < 100)
                {
                    return (false, "Minimum 100 points required for conversion");
                }

                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "User not found");
                }

                if (user.TotalRewardPoints < points)
                {
                    return (false, $"Insufficient points. You have {user.TotalRewardPoints} points.");
                }

                // Check premium status
                await CheckPremiumStatusAsync(userId);

                // Calculate wallet amount: 100 points = ৳1 for general, 50 points = ৳1 for premium
                decimal pointsPerTaka = user.IsPremiumMember ? 50m : 100m;
                decimal walletAmount = Math.Round(points / pointsPerTaka); // Round to nearest integer

                // Deduct points and add wallet balance
                user.TotalRewardPoints -= points;
                user.WalletBalance += walletAmount;

                // Create transaction record
                var transaction = new RewardPointsTransaction
                {
                    UserId = userId,
                    TransactionType = RewardTransactionType.Redemption,
                    Points = -points,
                    BalanceAfter = user.TotalRewardPoints,
                    Description = $"Converted to wallet: ৳{walletAmount:N0}",
                    CreatedAt = DateTime.UtcNow
                };

                _db.RewardPointsTransactions.Add(transaction);
                await _db.SaveChangesAsync();

                var rateInfo = user.IsPremiumMember ? "50 points = ৳1 (Premium Rate)" : "100 points = ৳1";
                _logger.LogInformation("User {UserId} converted {Points} points to ৳{Amount} wallet ({Rate})",
                    userId, points, walletAmount, rateInfo);

                return (true, $"Successfully converted {points} points to ৳{walletAmount:N0} wallet balance!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting points to wallet for user {UserId}", userId);
                return (false, "An error occurred while converting points");
            }
        }

        public async Task<bool> CheckPremiumStatusAsync(string userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return false;

                if (!user.IsPremiumMember) return false;

                // Check if premium has expired
                if (user.PremiumExpiresAt.HasValue && user.PremiumExpiresAt.Value < DateTime.UtcNow)
                {
                    user.IsPremiumMember = false;
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Premium membership expired for user {UserId}", userId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking premium status for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> HasActivePremiumDiscountAsync(string userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return false;

                // Must be premium and within 5% discount period
                if (!user.IsPremiumMember) return false;

                if (user.PremiumDiscountExpiresAt.HasValue && user.PremiumDiscountExpiresAt.Value > DateTime.UtcNow)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking premium discount for user {UserId}", userId);
                return false;
            }
        }

        public async Task UpdateExpiredRewardsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredRewards = await _db.UserRedeemedRewards
                    .Where(r => r.Status == RewardStatus.Available && r.ExpiresAt < now)
                    .ToListAsync();

                foreach (var reward in expiredRewards)
                {
                    reward.Status = RewardStatus.Expired;
                }

                if (expiredRewards.Any())
                {
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Updated {Count} expired rewards", expiredRewards.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expired rewards");
            }
        }

        private static string GenerateRandomDigits(int length)
        {
            var random = new Random();
            var digits = new char[length];
            for (int i = 0; i < length; i++)
            {
                digits[i] = (char)('0' + random.Next(10));
            }
            return new string(digits);
        }

        public async Task<(bool Awarded, string Message)> AwardDailyLoginPointsAsync(string userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                {
                    return (false, "User not found");
                }

                // Get today's date in Bangladesh Standard Time (UTC+6)
                var bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
                var nowInBD = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bdTimeZone);
                var todayInBD = nowInBD.Date;

                // Check if user already received daily reward today
                if (user.LastDailyRewardDate.HasValue)
                {
                    var lastRewardDateInBD = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(user.LastDailyRewardDate.Value, DateTimeKind.Utc),
                        bdTimeZone).Date;

                    if (lastRewardDateInBD >= todayInBD)
                    {
                        return (false, "Daily reward already claimed today");
                    }
                }

                // Award daily login points
                user.TotalRewardPoints += DailyLoginPoints;
                user.LastDailyRewardDate = DateTime.UtcNow;

                // Create transaction record
                var transaction = new RewardPointsTransaction
                {
                    UserId = userId,
                    TransactionType = RewardTransactionType.DailyLogin,
                    Points = DailyLoginPoints,
                    BalanceAfter = user.TotalRewardPoints,
                    Description = $"Daily login reward - {todayInBD:dd MMM yyyy}",
                    CreatedAt = DateTime.UtcNow
                };

                _db.RewardPointsTransactions.Add(transaction);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Awarded {Points} daily login points to user {UserId}",
                    DailyLoginPoints, userId);

                return (true, $"You earned {DailyLoginPoints} reward points for today's visit!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding daily login points for user {UserId}", userId);
                return (false, "An error occurred");
            }
        }
    }
}
