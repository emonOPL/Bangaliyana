using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum MonthlyReportStatus
    {
        [Display(Name = "Open")]
        Open,                  // Current month, still accepting transactions

        [Display(Name = "Finalized")]
        Finalized,             // Month ended, calculations frozen

        [Display(Name = "Pending Payout")]
        PendingPayout,         // Eligible for payout (>= 500 BDT)

        [Display(Name = "Payout Created")]
        PayoutCreated,         // Payout record created

        [Display(Name = "Paid")]
        Paid,                  // Payment completed

        [Display(Name = "Carried Forward")]
        CarriedForward         // Amount < 500, moved to next month
    }

    public class SellerMonthlyReport
    {
        public int Id { get; set; }

        [Required]
        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        // Period identification
        [Required]
        [Display(Name = "Year")]
        public int Year { get; set; }

        [Required]
        [Range(1, 12)]
        [Display(Name = "Month")]
        public int Month { get; set; }

        // Unique identifier for the report
        [Required]
        [StringLength(30)]
        [Display(Name = "Report Number")]
        public string ReportNumber { get; set; } = string.Empty; // e.g., "RPT-2026-01-001"

        // ============ Calculated Totals (frozen when finalized) ============
        [Display(Name = "Total Sales")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSales { get; set; }

        [Display(Name = "Total Items Delivered")]
        public int TotalItemsDelivered { get; set; }

        [Display(Name = "Total Orders")]
        public int TotalOrders { get; set; }

        [Display(Name = "Commission Rate")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; }

        [Display(Name = "Commission Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Display(Name = "Refund Deductions")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundDeductions { get; set; }

        [Display(Name = "Return Deductions")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReturnDeductions { get; set; }

        [Display(Name = "Adjustments")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Adjustments { get; set; }

        // ============ Carryforward ============
        [Display(Name = "Carry Forward From Previous")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CarryForwardFromPrevious { get; set; }

        // Net Payable = TotalSales - CommissionAmount - RefundDeductions - ReturnDeductions + CarryForwardFromPrevious + Adjustments
        [Display(Name = "Net Payable")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetPayable { get; set; }

        // Amount carried forward to next month (if NetPayable < MinimumPayout)
        [Display(Name = "Carry Forward To Next")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CarryForwardToNext { get; set; }

        // Final amount to pay (0 if carried forward)
        [Display(Name = "Payout Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PayoutAmount { get; set; }

        // ============ Status ============
        [Display(Name = "Status")]
        public MonthlyReportStatus Status { get; set; } = MonthlyReportStatus.Open;

        // ============ Linked Payout Record ============
        public int? ManualSellerPayoutId { get; set; }

        [ForeignKey("ManualSellerPayoutId")]
        public virtual ManualSellerPayout? ManualSellerPayout { get; set; }

        // ============ Period Info ============
        [Display(Name = "Period Start")]
        public DateTime PeriodStart { get; set; }

        [Display(Name = "Period End")]
        public DateTime PeriodEnd { get; set; }

        // ============ Finalization Info ============
        [Display(Name = "Finalized At")]
        public DateTime? FinalizedAt { get; set; }

        [StringLength(450)]
        public string? FinalizedByUserId { get; set; }

        [ForeignKey("FinalizedByUserId")]
        public virtual ApplicationUser? FinalizedBy { get; set; }

        // ============ Notes ============
        [StringLength(1000)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [StringLength(1000)]
        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        // ============ Timestamps ============
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ============ Navigation Properties ============
        public virtual ICollection<MonthlyReportOrderItem> ReportOrderItems { get; set; } = new List<MonthlyReportOrderItem>();

        // ============ Helper Properties ============
        [NotMapped]
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

        [NotMapped]
        public string PeriodDisplay => $"{PeriodStart:dd MMM} - {PeriodEnd:dd MMM yyyy}";

        [NotMapped]
        public string StatusBadgeClass => Status switch
        {
            MonthlyReportStatus.Open => "bg-primary",
            MonthlyReportStatus.Finalized => "bg-info",
            MonthlyReportStatus.PendingPayout => "bg-warning text-dark",
            MonthlyReportStatus.PayoutCreated => "bg-info",
            MonthlyReportStatus.Paid => "bg-success",
            MonthlyReportStatus.CarriedForward => "bg-secondary",
            _ => "bg-secondary"
        };

        [NotMapped]
        public string StatusDisplayName => Status switch
        {
            MonthlyReportStatus.Open => "Open",
            MonthlyReportStatus.Finalized => "Finalized",
            MonthlyReportStatus.PendingPayout => "Pending Payout",
            MonthlyReportStatus.PayoutCreated => "Payout Created",
            MonthlyReportStatus.Paid => "Paid",
            MonthlyReportStatus.CarriedForward => "Carried Forward",
            _ => Status.ToString()
        };

        [NotMapped]
        public bool IsEditable => Status == MonthlyReportStatus.Open;

        [NotMapped]
        public bool CanCreatePayout => Status == MonthlyReportStatus.PendingPayout;

        [NotMapped]
        public decimal GrossPayable => TotalSales - CommissionAmount;

        [NotMapped]
        public decimal TotalDeductions => RefundDeductions + ReturnDeductions;
    }
}
