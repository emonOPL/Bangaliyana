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
using Bangaliyana.Services;
using Bangaliyana.Extensions;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public class WalletRewardsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IRewardService _rewardService;

        public WalletRewardsModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IRewardService rewardService)
        {
            _userManager = userManager;
            _context = context;
            _rewardService = rewardService;
        }

        public decimal WalletBalance { get; set; } = 0;
        public int RewardPoints { get; set; } = 0;
        public int TotalOrders { get; set; } = 0;
        public decimal TotalSpent { get; set; } = 0;
        public List<TransactionViewModel> RecentTransactions { get; set; } = new();
        public List<RewardViewModel> AvailableRewards { get; set; } = new();

        // New properties for vouchers and premium
        public List<RedeemedVoucherViewModel> RedeemedVouchers { get; set; } = new();
        public bool IsPremiumMember { get; set; } = false;
        public DateTime? PremiumExpiresAt { get; set; }
        public bool HasActivePremiumDiscount { get; set; } = false;
        public DateTime? PremiumDiscountExpiresAt { get; set; }

        // Wallet conversion rates
        public int ConversionRateGeneral { get; set; } = 100; // 100 points = ৳1
        public int ConversionRatePremium { get; set; } = 50;  // 50 points = ৳1 for premium

        // Pagination for Recent Activity
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalPages { get; set; }
        public int TotalTransactions { get; set; }

        // Pagination for My Vouchers
        public int VoucherCurrentPage { get; set; } = 1;
        public int VoucherPageSize { get; set; } = 5;
        public int VoucherTotalPages { get; set; }
        public int TotalVouchers { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public class TransactionViewModel
        {
            public int Id { get; set; }
            public string Type { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string Status { get; set; } = string.Empty;
            public bool IsCredit { get; set; }
        }

        public class RewardViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int PointsRequired { get; set; }
            public string Icon { get; set; } = string.Empty;
            public bool CanRedeem { get; set; }
        }

        public class RedeemedVoucherViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public decimal? DiscountAmount { get; set; }
            public int PointsSpent { get; set; }
            public string Status { get; set; } = string.Empty;
            public string StatusClass { get; set; } = string.Empty;
            public DateTime RedeemedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public DateTime? UsedAt { get; set; }
            public string Icon { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(int tpage = 1, int vpage = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Set pagination for transactions
            CurrentPage = tpage < 1 ? 1 : tpage;
            // Set pagination for vouchers
            VoucherCurrentPage = vpage < 1 ? 1 : vpage;

            // Get user's order stats
            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id && o.Status == OrderStatus.Delivered)
                .ToListAsync();

            TotalOrders = orders.Count;
            TotalSpent = orders.Sum(o => o.TotalAmount);

            // Get real reward points from user model
            RewardPoints = user.TotalRewardPoints;

            // Get wallet balance from user model
            WalletBalance = user.WalletBalance;

            // Check and update premium status
            IsPremiumMember = await _rewardService.CheckPremiumStatusAsync(user.Id);
            if (IsPremiumMember)
            {
                PremiumExpiresAt = user.PremiumExpiresAt;
                HasActivePremiumDiscount = await _rewardService.HasActivePremiumDiscountAsync(user.Id);
                PremiumDiscountExpiresAt = user.PremiumDiscountExpiresAt;
            }

            // Get all redeemed rewards (vouchers) with pagination
            var allRedeemedRewards = await _rewardService.GetAllRedeemedRewardsAsync(user.Id);
            var allVouchers = allRedeemedRewards.Select(r => new RedeemedVoucherViewModel
            {
                Id = r.Id,
                Name = r.RewardName,
                Type = r.RewardType.GetDisplayName(),
                DiscountAmount = r.DiscountAmount,
                PointsSpent = r.PointsSpent,
                Status = r.Status.GetDisplayName(),
                StatusClass = r.Status == RewardStatus.Available ? "success" : (r.Status == RewardStatus.Used ? "secondary" : "danger"),
                RedeemedAt = r.RedeemedAt,
                ExpiresAt = r.ExpiresAt,
                UsedAt = r.UsedAt,
                Icon = r.RewardType == RewardType.FreeShipping ? "fas fa-truck" :
                       r.RewardType == RewardType.DiscountVoucher ? "fas fa-tag" :
                       r.RewardType == RewardType.PremiumMembership ? "fas fa-crown" : "fas fa-gift"
            }).OrderByDescending(r => r.RedeemedAt).ToList();

            // Paginate vouchers
            TotalVouchers = allVouchers.Count;
            VoucherTotalPages = (int)Math.Ceiling((double)TotalVouchers / VoucherPageSize);

            // Adjust voucher current page if it exceeds total pages
            if (VoucherCurrentPage > VoucherTotalPages && VoucherTotalPages > 0)
            {
                VoucherCurrentPage = VoucherTotalPages;
            }

            RedeemedVouchers = allVouchers
                .Skip((VoucherCurrentPage - 1) * VoucherPageSize)
                .Take(VoucherPageSize)
                .ToList();

            // Get paginated transactions from reward service
            var (transactions, totalCount) = await _rewardService.GetUserTransactionsPaginatedAsync(user.Id, CurrentPage, PageSize);
            TotalTransactions = totalCount;
            TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

            // Adjust current page if it exceeds total pages
            if (CurrentPage > TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }

            RecentTransactions = transactions.Select(t => new TransactionViewModel
            {
                Id = t.Id,
                Type = GetTransactionTypeName(t.TransactionType),
                Description = t.Description ?? GetDefaultDescription(t.TransactionType, t.Points),
                Amount = Math.Abs(t.Points),
                Date = t.CreatedAt,
                Status = "Completed",
                IsCredit = t.Points > 0
            }).ToList();

            // Available rewards (sorted by points ascending)
            AvailableRewards = new List<RewardViewModel>
            {
                new RewardViewModel { Id = 3, Name = "Free Shipping", Description = "Free delivery on your next order", PointsRequired = 300, Icon = "fas fa-truck", CanRedeem = RewardPoints >= 300 },
                new RewardViewModel { Id = 1, Name = "৳50 Discount", Description = "Get ৳50 off on your next order", PointsRequired = 500, Icon = "fas fa-tag", CanRedeem = RewardPoints >= 500 },
                new RewardViewModel { Id = 2, Name = "৳100 Discount", Description = "Get ৳100 off on your next order", PointsRequired = 900, Icon = "fas fa-gift", CanRedeem = RewardPoints >= 900 },
                new RewardViewModel { Id = 4, Name = "৳200 Discount", Description = "Get ৳200 off on orders above ৳2000", PointsRequired = 1600, Icon = "fas fa-percent", CanRedeem = RewardPoints >= 1600 },
                new RewardViewModel { Id = 6, Name = "৳500 Discount", Description = "Get ৳500 off on orders above ৳5000", PointsRequired = 3500, Icon = "fas fa-money-bill-wave", CanRedeem = RewardPoints >= 3500 },
                new RewardViewModel { Id = 5, Name = "Premium Member", Description = "1 month premium benefits", PointsRequired = 5000, Icon = "fas fa-crown", CanRedeem = RewardPoints >= 5000 },
            };

            return Page();
        }

        private static string GetTransactionTypeName(RewardTransactionType type)
        {
            return type switch
            {
                RewardTransactionType.OrderDelivery => "Order Delivery",
                RewardTransactionType.ProductReview => "Product Review",
                RewardTransactionType.PlatformReview => "Platform Review",
                RewardTransactionType.ReferralBonus => "Referral Bonus",
                RewardTransactionType.ReferredBonus => "Welcome Bonus",
                RewardTransactionType.Redemption => "Points Redeemed",
                _ => "Points"
            };
        }

        private static string GetDefaultDescription(RewardTransactionType type, int points)
        {
            return type switch
            {
                RewardTransactionType.OrderDelivery => $"Earned {points} points from order delivery",
                RewardTransactionType.ProductReview => $"Earned {points} points for product review",
                RewardTransactionType.PlatformReview => $"Earned {points} points for platform review",
                RewardTransactionType.ReferralBonus => $"Earned {points} points for referral",
                RewardTransactionType.ReferredBonus => $"Earned {points} points as welcome bonus",
                RewardTransactionType.Redemption => $"Redeemed {Math.Abs(points)} points",
                _ => $"{(points > 0 ? "Earned" : "Used")} {Math.Abs(points)} points"
            };
        }

        public async Task<IActionResult> OnPostRedeemAsync(int rewardId, int pointsRequired, string rewardName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var (success, message) = await _rewardService.RedeemPointsAsync(user.Id, rewardId, pointsRequired, rewardName);

            if (success)
            {
                StatusMessage = message;
            }
            else
            {
                StatusMessage = $"Error: {message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConvertToWalletAsync(int points)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (points <= 0)
            {
                StatusMessage = "Error: Please enter a valid number of points to convert.";
                return RedirectToPage();
            }

            var (success, message) = await _rewardService.ConvertPointsToWalletAsync(user.Id, points);

            if (success)
            {
                StatusMessage = message;
            }
            else
            {
                StatusMessage = $"Error: {message}";
            }

            return RedirectToPage();
        }
    }
}
