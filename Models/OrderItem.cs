using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        // FK to Order
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        // Product reference
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        // Variant reference (optional)
        public int? ProductVariantId { get; set; }

        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }

        // Snapshot product info (stored at time of order)
        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = null!;

        [StringLength(500)]
        public string? ProductImageUrl { get; set; }

        [StringLength(100)]
        public string? VariantInfo { get; set; } // e.g., "Size: M, Color: Red"

        [StringLength(100)]
        public string? SKU { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal? DiscountPrice { get; set; }

        public int Quantity { get; set; }

        public decimal TotalPrice { get; set; }

        // Seller info (for marketplace)
        public int? SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
