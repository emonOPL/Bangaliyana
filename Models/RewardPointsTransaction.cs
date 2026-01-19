using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum RewardTransactionType
    {
        OrderDelivery,      // Points earned from delivered orders (1 pt per ৳100)
        ProductReview,      // Points from product reviews (10 pts)
        PlatformReview,     // Points from platform review (50 pts)
        ReferralBonus,      // Points for referring someone (100 pts)
        ReferredBonus,      // Points for being referred (200 pts)
        Redemption,         // Points spent (negative)
        Adjustment,         // Manual admin adjustment
        DailyLogin          // Daily login reward (10 pts per day)
    }

    public class RewardPointsTransaction
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Display(Name = "Transaction Type")]
        public RewardTransactionType TransactionType { get; set; }

        [Display(Name = "Points")]
        public int Points { get; set; }  // Positive for earned, negative for spent

        [Display(Name = "Balance After")]
        public int BalanceAfter { get; set; }  // Running balance after this transaction

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Reference IDs for traceability
        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public int? ProductReviewId { get; set; }

        [ForeignKey("ProductReviewId")]
        public virtual ProductReview? ProductReview { get; set; }

        public int? TestimonialId { get; set; }

        [ForeignKey("TestimonialId")]
        public virtual Testimonial? Testimonial { get; set; }

        [StringLength(450)]
        [Display(Name = "Referred User ID")]
        public string? ReferredUserId { get; set; }

        [ForeignKey("ReferredUserId")]
        public virtual ApplicationUser? ReferredUser { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
