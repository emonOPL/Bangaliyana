using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum TransactionType
    {
        Payment,
        Refund,
        SellerPayout,
        Commission
    }

    public enum TransactionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public class Transaction
    {
        public int Id { get; set; }

        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public int? SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        [Display(Name = "Transaction Type")]
        public TransactionType Type { get; set; } = TransactionType.Payment;

        [Required]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }

        [StringLength(100)]
        [Display(Name = "Gateway Transaction ID")]
        public string? GatewayTransactionId { get; set; }

        [StringLength(100)]
        [Display(Name = "Gateway Reference")]
        public string? GatewayReference { get; set; }

        [Display(Name = "Status")]
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [StringLength(500)]
        [Display(Name = "Failure Reason")]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
