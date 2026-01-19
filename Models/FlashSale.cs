using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class FlashSale
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Flash Sale Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(255)]
        [Display(Name = "Banner Image URL")]
        public string? BannerImageUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Mobile Banner Image URL")]
        public string? MobileBannerImageUrl { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Show Countdown")]
        public bool ShowCountdown { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        // Color theming
        [StringLength(50)]
        [Display(Name = "Primary Color")]
        public string? PrimaryColor { get; set; } = "#ef4444";

        [StringLength(50)]
        [Display(Name = "Secondary Color")]
        public string? SecondaryColor { get; set; } = "#fef2f2";

        [StringLength(50)]
        [Display(Name = "Text Color")]
        public string? TextColor { get; set; } = "#ffffff";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<FlashSaleProduct> Products { get; set; } = new List<FlashSaleProduct>();

        // Helper properties
        [NotMapped]
        public bool IsCurrentlyActive
        {
            get
            {
                if (!IsActive) return false;
                var now = DateTime.UtcNow;
                return now >= StartDate && now <= EndDate;
            }
        }

        [NotMapped]
        public bool IsUpcoming => IsActive && DateTime.UtcNow < StartDate;

        [NotMapped]
        public bool IsExpired => DateTime.UtcNow > EndDate;

        [NotMapped]
        public TimeSpan TimeRemaining => EndDate > DateTime.UtcNow ? EndDate - DateTime.UtcNow : TimeSpan.Zero;
    }

    public class FlashSaleProduct
    {
        public int Id { get; set; }

        public int FlashSaleId { get; set; }

        [ForeignKey("FlashSaleId")]
        public virtual FlashSale FlashSale { get; set; } = null!;

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products Product { get; set; } = null!;

        // Flash sale specific discount (overrides product's default discount)
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Flash Sale Price")]
        public decimal FlashSalePrice { get; set; }

        [Display(Name = "Discount Percentage")]
        public int DiscountPercentage { get; set; }

        // Stock limit for flash sale
        [Display(Name = "Flash Sale Stock")]
        public int? FlashSaleStock { get; set; }

        [Display(Name = "Sold Count")]
        public int SoldCount { get; set; } = 0;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Helper properties
        [NotMapped]
        public int? RemainingStock => FlashSaleStock.HasValue ? FlashSaleStock.Value - SoldCount : null;

        [NotMapped]
        public bool IsSoldOut => FlashSaleStock.HasValue && SoldCount >= FlashSaleStock.Value;
    }
}
