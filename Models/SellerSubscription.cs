using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum SubscriptionStatus
    {
        [Display(Name = "Active")]
        Active,

        [Display(Name = "Expired")]
        Expired,

        [Display(Name = "Cancelled")]
        Cancelled,

        [Display(Name = "Pending Payment")]
        PendingPayment,

        [Display(Name = "Suspended")]
        Suspended
    }

    public class SellerSubscription
    {
        public int Id { get; set; }

        [Required]
        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        [Required]
        public int PlanId { get; set; }

        [ForeignKey("PlanId")]
        public virtual SellerSubscriptionPlan? Plan { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Status")]
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        [Display(Name = "Amount Paid")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [StringLength(100)]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; }

        [StringLength(100)]
        [Display(Name = "Transaction ID")]
        public string? TransactionId { get; set; }

        [Display(Name = "Auto Renew")]
        public bool AutoRenew { get; set; } = false;

        [Display(Name = "Is Trial")]
        public bool IsTrial { get; set; } = false;

        [Display(Name = "Cancelled At")]
        public DateTime? CancelledAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Cancellation Reason")]
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
