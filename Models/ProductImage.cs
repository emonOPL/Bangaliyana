using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Alt Text")]
        public string? AltText { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Primary")]
        public bool IsPrimary { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
