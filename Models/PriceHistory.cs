using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum PriceChangeType
    {
        PriceIncrease,
        PriceDecrease,
        DiscountAdded,
        DiscountRemoved,
        DiscountIncreased,
        DiscountDecreased
    }

    public class PriceHistory
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OldPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NewPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldDiscountPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NewDiscountPrice { get; set; }

        // The effective price change (considers discount if available)
        [Column(TypeName = "decimal(18,2)")]
        public decimal OldEffectivePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NewEffectivePrice { get; set; }

        public PriceChangeType ChangeType { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PercentageChange { get; set; }

        // Who made the change (for audit)
        public string? ChangedByUserId { get; set; }

        [ForeignKey("ChangedByUserId")]
        public virtual ApplicationUser? ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
