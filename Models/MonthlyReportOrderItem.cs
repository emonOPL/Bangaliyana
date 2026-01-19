using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum ReportItemStatus
    {
        [Display(Name = "Delivered")]
        Delivered,          // Order delivered, included in report

        [Display(Name = "Returned")]
        Returned,           // Order returned after inclusion

        [Display(Name = "Refunded")]
        Refunded,           // Order refunded after inclusion

        [Display(Name = "Adjusted")]
        Adjusted            // Manual adjustment applied
    }

    public class MonthlyReportOrderItem
    {
        public int Id { get; set; }

        // ============ Report Reference ============
        [Required]
        public int MonthlyReportId { get; set; }

        [ForeignKey("MonthlyReportId")]
        public virtual SellerMonthlyReport? MonthlyReport { get; set; }

        // ============ Order Reference ============
        [Required]
        public int OrderItemId { get; set; }

        [ForeignKey("OrderItemId")]
        public virtual OrderItem? OrderItem { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        // ============ Snapshot Values (at time of inclusion) ============
        [Display(Name = "Item Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ItemAmount { get; set; }

        [Display(Name = "Commission Rate")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; }

        [Display(Name = "Commission Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Display(Name = "Net Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }

        // ============ Status ============
        [Display(Name = "Status")]
        public ReportItemStatus Status { get; set; } = ReportItemStatus.Delivered;

        // ============ Deduction Info (for returns/refunds) ============
        [Display(Name = "Deduction Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DeductionAmount { get; set; }

        // Which report the deduction was applied to (may be different from inclusion report)
        public int? DeductionReportId { get; set; }

        [ForeignKey("DeductionReportId")]
        public virtual SellerMonthlyReport? DeductionReport { get; set; }

        [StringLength(500)]
        [Display(Name = "Deduction Reason")]
        public string? DeductionReason { get; set; }

        // ============ Timestamps ============
        [Display(Name = "Delivered At")]
        public DateTime? DeliveredAt { get; set; }

        [Display(Name = "Returned At")]
        public DateTime? ReturnedAt { get; set; }

        [Display(Name = "Refunded At")]
        public DateTime? RefundedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ============ Helper Properties ============
        [NotMapped]
        public string StatusBadgeClass => Status switch
        {
            ReportItemStatus.Delivered => "bg-success",
            ReportItemStatus.Returned => "bg-warning text-dark",
            ReportItemStatus.Refunded => "bg-danger",
            ReportItemStatus.Adjusted => "bg-info",
            _ => "bg-secondary"
        };

        [NotMapped]
        public string StatusDisplayName => Status switch
        {
            ReportItemStatus.Delivered => "Delivered",
            ReportItemStatus.Returned => "Returned",
            ReportItemStatus.Refunded => "Refunded",
            ReportItemStatus.Adjusted => "Adjusted",
            _ => Status.ToString()
        };

        [NotMapped]
        public bool IsDeducted => Status == ReportItemStatus.Returned || Status == ReportItemStatus.Refunded;

        [NotMapped]
        public decimal EffectiveAmount => IsDeducted ? 0 : NetAmount;

        [NotMapped]
        public string OrderNumber => Order?.OrderNumber ?? $"#{OrderId}";
    }
}
