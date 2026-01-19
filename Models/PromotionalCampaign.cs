using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum TargetAudience
    {
        All,
        NewUsers,           // Users registered in last 30 days
        ReturningUsers,     // Users with at least MinOrderCount orders
        PremiumMembers,     // Users with IsPremiumMember = true
        InactiveUsers,      // Users who haven't ordered in 60+ days
        HighSpenders        // Users who spent more than MinTotalSpent
    }

    public enum CampaignStatus
    {
        Draft,
        Scheduled,
        Sending,
        Sent,
        Cancelled
    }

    public class PromotionalCampaign
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(50)]
        public string IconClass { get; set; } = "fa-bullhorn";

        [StringLength(50)]
        public string IconColor { get; set; } = "text-primary";

        [StringLength(500)]
        public string? ActionUrl { get; set; }

        [StringLength(50)]
        public string? ActionText { get; set; }

        // Targeting
        public TargetAudience TargetAudience { get; set; } = TargetAudience.All;

        // For ReturningUsers filter
        public int? MinOrderCount { get; set; }

        // For HighSpenders filter
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinTotalSpent { get; set; }

        // Promo Integration
        public int? CouponId { get; set; }

        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }

        [StringLength(50)]
        public string? PromoCode { get; set; }

        [StringLength(200)]
        public string? DiscountDescription { get; set; }

        // Email Options
        public bool SendEmail { get; set; } = false;

        [StringLength(200)]
        public string? EmailSubject { get; set; }

        public bool EmailSent { get; set; } = false;

        // Scheduling
        public DateTime? ScheduledAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool IsImmediate { get; set; } = true;

        // Status & Stats
        public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

        public DateTime? SentAt { get; set; }

        public int TotalRecipients { get; set; } = 0;

        public int DeliveredCount { get; set; } = 0;

        public int ReadCount { get; set; } = 0;

        public int ClickCount { get; set; } = 0;

        // Audit
        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        [ForeignKey("CreatedByUserId")]
        public virtual ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<CampaignRecipient> Recipients { get; set; } = new List<CampaignRecipient>();

        // Computed properties
        [NotMapped]
        public decimal ReadRate => TotalRecipients > 0 ? (decimal)ReadCount / TotalRecipients * 100 : 0;

        [NotMapped]
        public decimal ClickRate => TotalRecipients > 0 ? (decimal)ClickCount / TotalRecipients * 100 : 0;

        [NotMapped]
        public bool IsActive => Status == CampaignStatus.Sent &&
                               (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
    }
}
