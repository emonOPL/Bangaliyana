using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum ManualPayoutStatus
    {
        [Display(Name = "Pending")]
        Pending,
        [Display(Name = "Processing")]
        Processing,
        [Display(Name = "Completed")]
        Completed,
        [Display(Name = "Failed")]
        Failed,
        [Display(Name = "Cancelled")]
        Cancelled
    }

    public enum ManualPayoutMethod
    {
        [Display(Name = "Bank Transfer")]
        BankTransfer,
        [Display(Name = "bKash")]
        bKash,
        [Display(Name = "Nagad")]
        Nagad,
        [Display(Name = "Rocket")]
        Rocket,
        [Display(Name = "Upay")]
        Upay,
        [Display(Name = "Cash")]
        Cash
    }

    public class ManualSellerPayout
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Payout Number")]
        public string PayoutNumber { get; set; } = string.Empty;

        [Required]
        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        [Required]
        [Display(Name = "Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public ManualPayoutMethod PaymentMethod { get; set; }

        [Display(Name = "Status")]
        public ManualPayoutStatus Status { get; set; } = ManualPayoutStatus.Pending;

        // ============ Bank Transfer Details (Receiver - Seller's Bank) ============
        [StringLength(100)]
        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string? BankAccountNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Account Holder Name")]
        public string? BankAccountHolderName { get; set; }

        [StringLength(100)]
        [Display(Name = "Branch Name")]
        public string? BankBranchName { get; set; }

        [StringLength(50)]
        [Display(Name = "Routing Number")]
        public string? BankRoutingNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Bank Transaction Reference")]
        public string? BankTransactionReference { get; set; }

        // ============ Bank Transfer Details (Sender - Company's Bank) ============
        [StringLength(100)]
        [Display(Name = "Sender Bank Name")]
        public string? SenderBankName { get; set; }

        [StringLength(50)]
        [Display(Name = "Sender Account Number")]
        public string? SenderAccountNumber { get; set; }

        // ============ Mobile Banking Details ============
        [StringLength(50)]
        [Display(Name = "Mobile Banking Provider")]
        public string? MobileBankingProvider { get; set; }

        [StringLength(20)]
        [Display(Name = "Sender Number")]
        public string? SenderMobileNumber { get; set; }

        [StringLength(20)]
        [Display(Name = "Receiver Number")]
        public string? ReceiverMobileNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Transaction ID")]
        public string? MobileTransactionId { get; set; }

        // ============ Payment Completion Details ============
        [Display(Name = "Payment Date")]
        public DateTime? PaymentDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [StringLength(500)]
        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        // ============ Processing Info ============
        [StringLength(450)]
        public string? ProcessedById { get; set; }

        [ForeignKey("ProcessedById")]
        public virtual ApplicationUser? ProcessedBy { get; set; }

        [StringLength(100)]
        [Display(Name = "Processed By Name")]
        public string? ProcessedByName { get; set; }

        // ============ Period Info (for which period this payout is) ============
        [Display(Name = "Period Start")]
        public DateTime? PeriodStart { get; set; }

        [Display(Name = "Period End")]
        public DateTime? PeriodEnd { get; set; }

        // ============ Calculated Fields ============
        [Display(Name = "Total Sales")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSales { get; set; }

        [Display(Name = "Commission Rate")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; }

        [Display(Name = "Commission Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        // ============ Timestamps ============
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // ============ Helper Properties ============
        [NotMapped]
        public bool IsBankTransfer => PaymentMethod == ManualPayoutMethod.BankTransfer;

        [NotMapped]
        public bool IsMobileBanking => PaymentMethod == ManualPayoutMethod.bKash ||
                                       PaymentMethod == ManualPayoutMethod.Nagad ||
                                       PaymentMethod == ManualPayoutMethod.Rocket ||
                                       PaymentMethod == ManualPayoutMethod.Upay;

        [NotMapped]
        public bool IsCash => PaymentMethod == ManualPayoutMethod.Cash;

        [NotMapped]
        public string PaymentMethodDisplay => PaymentMethod switch
        {
            ManualPayoutMethod.BankTransfer => "Bank Transfer",
            ManualPayoutMethod.bKash => "bKash",
            ManualPayoutMethod.Nagad => "Nagad",
            ManualPayoutMethod.Rocket => "Rocket",
            ManualPayoutMethod.Upay => "Upay",
            ManualPayoutMethod.Cash => "Cash",
            _ => PaymentMethod.ToString()
        };

        [NotMapped]
        public string StatusBadgeClass => Status switch
        {
            ManualPayoutStatus.Pending => "bg-warning text-dark",
            ManualPayoutStatus.Processing => "bg-info",
            ManualPayoutStatus.Completed => "bg-success",
            ManualPayoutStatus.Failed => "bg-danger",
            ManualPayoutStatus.Cancelled => "bg-secondary",
            _ => "bg-secondary"
        };

        [NotMapped]
        public string MobileBankingProviderDisplay => PaymentMethod switch
        {
            ManualPayoutMethod.bKash => "bKash",
            ManualPayoutMethod.Nagad => "Nagad",
            ManualPayoutMethod.Rocket => "Rocket",
            ManualPayoutMethod.Upay => "Upay",
            _ => MobileBankingProvider ?? ""
        };
    }
}
