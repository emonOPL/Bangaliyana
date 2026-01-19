using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum PromotionType
    {
        [Display(Name = "Percentage Discount")]
        PercentageDiscount = 0,

        [Display(Name = "Fixed Amount Discount")]
        FixedAmountDiscount = 1,

        [Display(Name = "Banner Only")]
        BannerOnly = 2
    }

    public class CategoryPromotion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Promotion Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(100)]
        [Display(Name = "Tagline")]
        public string? Tagline { get; set; }

        // Target Category
        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;

        // Promotion Type & Discount
        [Display(Name = "Promotion Type")]
        public PromotionType PromotionType { get; set; } = PromotionType.BannerOnly;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Discount Value")]
        public decimal? DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Maximum Discount Amount")]
        public decimal? MaxDiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Minimum Order Amount")]
        public decimal? MinOrderAmount { get; set; }

        // Visual Settings
        [StringLength(255)]
        [Display(Name = "Banner Image URL")]
        public string? BannerImageUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Mobile Banner Image URL")]
        public string? MobileBannerImageUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Thumbnail Image URL")]
        public string? ThumbnailImageUrl { get; set; }

        [StringLength(50)]
        [Display(Name = "Background Color")]
        public string? BackgroundColor { get; set; }

        [StringLength(50)]
        [Display(Name = "Text Color")]
        public string? TextColor { get; set; }

        [StringLength(100)]
        [Display(Name = "Icon Class")]
        public string? IconClass { get; set; }

        // Call to Action
        [StringLength(100)]
        [Display(Name = "Button Text")]
        public string? ButtonText { get; set; } = "Shop Now";

        [StringLength(500)]
        [Url]
        [Display(Name = "Custom URL")]
        public string? CustomUrl { get; set; }

        // Validity
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Show on Homepage")]
        public bool ShowOnHomepage { get; set; } = true;

        [Display(Name = "Show Countdown")]
        public bool ShowCountdown { get; set; } = false;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Helper properties
        [NotMapped]
        public bool IsCurrentlyActive
        {
            get
            {
                if (!IsActive) return false;
                var now = DateTime.UtcNow;
                if (StartDate.HasValue && now < StartDate.Value) return false;
                if (EndDate.HasValue && now > EndDate.Value) return false;
                return true;
            }
        }

        [NotMapped]
        public bool IsExpired => EndDate.HasValue && DateTime.UtcNow > EndDate.Value;

        [NotMapped]
        public string DiscountDisplay
        {
            get
            {
                if (PromotionType == PromotionType.PercentageDiscount && DiscountValue.HasValue)
                    return $"{DiscountValue:0}% OFF";
                if (PromotionType == PromotionType.FixedAmountDiscount && DiscountValue.HasValue)
                    return $"৳{DiscountValue:N0} OFF";
                return string.Empty;
            }
        }
    }
}
