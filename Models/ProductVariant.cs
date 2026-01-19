using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        [StringLength(50)]
        [Display(Name = "Size")]
        public string? Size { get; set; }

        [StringLength(50)]
        [Display(Name = "Color")]
        public string? Color { get; set; }

        [StringLength(100)]
        [Display(Name = "Color Code (Hex)")]
        public string? ColorCode { get; set; }

        [StringLength(100)]
        [Display(Name = "SKU")]
        public string? SKU { get; set; }

        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue)]
        public int Stock { get; set; } = 0;

        [Display(Name = "Additional Price")]
        [Range(0, double.MaxValue)]
        public decimal AdditionalPrice { get; set; } = 0;

        [Display(Name = "Is Available")]
        public bool IsAvailable { get; set; } = true;

        [StringLength(500)]
        [Display(Name = "Variant Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed property for display
        public string VariantName => $"{Size ?? ""} {Color ?? ""}".Trim();

        // Total stock across all variants for the same color
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int TotalColorStock { get; set; }
    }
}
