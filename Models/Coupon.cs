using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum DiscountType
    {
        Percentage,
        FixedAmount
    }

    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Coupon Code")]
        public string Code { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Discount Type")]
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        [Required]
        [Display(Name = "Discount Value")]
        [Range(0.01, double.MaxValue)]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Minimum Order Amount")]
        [Range(0, double.MaxValue)]
        public decimal? MinimumOrderAmount { get; set; }

        [Display(Name = "Maximum Discount Amount")]
        [Range(0, double.MaxValue)]
        public decimal? MaximumDiscountAmount { get; set; }

        [Display(Name = "Usage Limit")]
        public int? UsageLimit { get; set; }

        [Display(Name = "Usage Per User Limit")]
        public int? UsagePerUserLimit { get; set; }

        [Display(Name = "Times Used")]
        public int TimesUsed { get; set; } = 0;

        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is First Order Only")]
        public bool IsFirstOrderOnly { get; set; } = false;

        // Seller specific coupon
        public int? SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        // User specific coupon (assigned to specific user only)
        [StringLength(450)]
        public string? AssignedToUserId { get; set; }

        [ForeignKey("AssignedToUserId")]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        // Category restriction
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();

        // Computed properties
        public bool IsValid => IsActive &&
                              (StartDate == null || StartDate <= DateTime.UtcNow) &&
                              (EndDate == null || EndDate >= DateTime.UtcNow) &&
                              (UsageLimit == null || TimesUsed < UsageLimit);
    }

    public class CouponUsage
    {
        public int Id { get; set; }

        [Required]
        public int CouponId { get; set; }

        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Display(Name = "Discount Amount")]
        public decimal DiscountAmount { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}
