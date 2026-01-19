using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum SellerPaymentStatus
    {
        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "Under Review")]
        UnderReview,

        [Display(Name = "Processing")]
        Processing,

        [Display(Name = "Paid")]
        Paid,

        [Display(Name = "Failed")]
        Failed
    }

    public enum SellerPaymentMethod
    {
        [Display(Name = "Bank Transfer")]
        BankTransfer,

        [Display(Name = "Mobile Banking")]
        MobileBanking
    }

    public class SellerPayment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Payment Number")]
        public string PaymentNumber { get; set; } = string.Empty;

        [Required]
        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        [Display(Name = "Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Display(Name = "Payment Method")]
        public SellerPaymentMethod PaymentMethod { get; set; }

        [Display(Name = "Status")]
        public SellerPaymentStatus Status { get; set; } = SellerPaymentStatus.Pending;

        // Bank Account Details (snapshot at time of payment)
        [StringLength(100)]
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [StringLength(100)]
        [Display(Name = "Branch Name")]
        public string? BranchName { get; set; }

        [StringLength(100)]
        [Display(Name = "Account Holder Name")]
        public string? AccountHolderName { get; set; }

        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "Routing Number")]
        public string? RoutingNumber { get; set; }

        // Mobile Banking Details (snapshot)
        [Display(Name = "Mobile Banking Provider")]
        public MobileBankingProvider? MobileBankingProvider { get; set; }

        [StringLength(20)]
        [Display(Name = "Mobile Number")]
        public string? MobileNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Mobile Account Name")]
        public string? MobileAccountName { get; set; }

        // Transaction reference
        [StringLength(100)]
        [Display(Name = "Transaction Reference")]
        public string? TransactionReference { get; set; }

        [StringLength(500)]
        [Display(Name = "Failure Reason")]
        public string? FailureReason { get; set; }

        [StringLength(1000)]
        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        // Audit fields
        [Display(Name = "Processed By")]
        public string? ProcessedByUserId { get; set; }

        [ForeignKey("ProcessedByUserId")]
        public virtual ApplicationUser? ProcessedBy { get; set; }

        [Display(Name = "Period Start")]
        public DateTime PeriodStart { get; set; }

        [Display(Name = "Period End")]
        public DateTime PeriodEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation property
        public virtual ICollection<SellerPaymentMessage> Messages { get; set; } = new List<SellerPaymentMessage>();
    }
}
