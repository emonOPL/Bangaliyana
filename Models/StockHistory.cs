using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Reason for stock change - used for audit trail
    /// </summary>
    public enum StockChangeReason
    {
        [Display(Name = "Order Placed")]
        OrderPlaced,

        [Display(Name = "Order Cancelled")]
        OrderCancelled,

        [Display(Name = "Order Refunded")]
        OrderRefunded,

        [Display(Name = "Manual Adjustment")]
        ManualAdjustment,

        [Display(Name = "Restock")]
        Restock,

        [Display(Name = "Inventory Correction")]
        InventoryCorrection,

        [Display(Name = "Bulk Import")]
        BulkImport,

        [Display(Name = "Return Received")]
        ReturnReceived
    }

    /// <summary>
    /// Tracks all stock changes for audit trail and analytics.
    /// </summary>
    public class StockHistory
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        // For variant-specific stock changes
        public int? ProductVariantId { get; set; }

        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }

        public int OldStock { get; set; }

        public int NewStock { get; set; }

        // Positive for increase, negative for decrease
        public int QuantityChanged { get; set; }

        public StockChangeReason Reason { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Reference to related order if applicable
        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        // Who made the change (admin, seller, or system)
        public string? ChangedByUserId { get; set; }

        [ForeignKey("ChangedByUserId")]
        public virtual ApplicationUser? ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
