using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum PaymentLogType
    {
        [Display(Name = "Processing Started")]
        ProcessingStarted,

        [Display(Name = "Processing Completed")]
        ProcessingCompleted,

        [Display(Name = "Payment Created")]
        PaymentCreated,

        [Display(Name = "Payment Failed")]
        PaymentFailed,

        [Display(Name = "Status Changed")]
        StatusChanged,

        [Display(Name = "Manual Trigger")]
        ManualTrigger,

        [Display(Name = "Error")]
        Error
    }

    public class SellerPaymentLog
    {
        public int Id { get; set; }

        [Display(Name = "Log Type")]
        public PaymentLogType LogType { get; set; }

        [StringLength(500)]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Details")]
        public string? Details { get; set; }

        // Related payment (optional)
        public int? SellerPaymentId { get; set; }

        [ForeignKey("SellerPaymentId")]
        public virtual SellerPayment? SellerPayment { get; set; }

        // Related seller (optional)
        public int? SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        // Who triggered the action (for manual triggers)
        public string? TriggeredByUserId { get; set; }

        [ForeignKey("TriggeredByUserId")]
        public virtual ApplicationUser? TriggeredBy { get; set; }

        // Processing stats (for batch processing logs)
        public int? TotalSellersProcessed { get; set; }
        public int? SuccessCount { get; set; }
        public int? FailCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmountProcessed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
