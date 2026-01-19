using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum RewardType
    {
        FreeShipping,
        DiscountVoucher,
        PremiumMembership
    }

    public enum RewardStatus
    {
        Available,
        Used,
        Expired
    }

    public class UserRedeemedReward
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Display(Name = "Reward Type")]
        public RewardType RewardType { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Reward Name")]
        public string RewardName { get; set; } = null!;

        [Display(Name = "Discount Amount")]
        [DataType(DataType.Currency)]
        public decimal? DiscountAmount { get; set; }

        [Display(Name = "Minimum Order Amount")]
        [DataType(DataType.Currency)]
        public decimal? MinimumOrderAmount { get; set; }

        [Display(Name = "Points Spent")]
        public int PointsSpent { get; set; }

        [Display(Name = "Status")]
        public RewardStatus Status { get; set; } = RewardStatus.Available;

        [Display(Name = "Used On Order")]
        public int? UsedOnOrderId { get; set; }

        [ForeignKey("UsedOnOrderId")]
        public virtual Order? UsedOnOrder { get; set; }

        [Display(Name = "Redeemed At")]
        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Expires At")]
        public DateTime ExpiresAt { get; set; }

        [Display(Name = "Used At")]
        public DateTime? UsedAt { get; set; }

        // Helper properties
        public bool IsExpired => Status != RewardStatus.Used && DateTime.UtcNow > ExpiresAt;
        public bool IsAvailable => Status == RewardStatus.Available && !IsExpired;
    }
}
