using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class Wishlist
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Price tracking for price drop alerts
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceWhenAdded { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? LastNotifiedPrice { get; set; }

        public DateTime? LastNotifiedAt { get; set; }

        public bool NotifyOnPriceDrop { get; set; } = true;

        // Computed properties
        [NotMapped]
        public decimal? CurrentPrice => Product?.EffectivePrice;

        [NotMapped]
        public bool HasPriceDropped => Product != null && Product.EffectivePrice < PriceWhenAdded;

        [NotMapped]
        public decimal PriceDropAmount => Product != null && HasPriceDropped ? PriceWhenAdded - Product.EffectivePrice : 0;

        [NotMapped]
        public decimal PriceDropPercentage => PriceWhenAdded > 0 && HasPriceDropped ?
            Math.Round((PriceWhenAdded - Product!.EffectivePrice) / PriceWhenAdded * 100, 1) : 0;
    }
}
