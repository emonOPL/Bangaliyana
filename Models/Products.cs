using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum ProductStatus
    {
        [Display(Name = "Draft")]
        Draft,

        [Display(Name = "Active")]
        Active,

        [Display(Name = "Inactive")]
        Inactive,

        [Display(Name = "Out of Stock")]
        OutOfStock,

        [Display(Name = "Discontinued")]
        Discontinued
    }

    public class Products
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "ProductNameIsRequired")]
        [StringLength(200)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Slug")]
        public string? Slug { get; set; }

        [Required(ErrorMessage = "ProductDescriptionIsRequired")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Description (Bengali)")]
        public string? DescriptionBengali { get; set; }

        [Display(Name = "Description (English)")]
        public string? DescriptionEnglish { get; set; }

        [StringLength(500)]
        [Display(Name = "Short Description")]
        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "ProductPriceIsRequired")]
        [Display(Name = "Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "PriceMustBeGreaterThanZero")]
        public decimal Price { get; set; }

        [Display(Name = "Discount Price")]
        [Range(0, double.MaxValue)]
        public decimal? DiscountPrice { get; set; }

        [Display(Name = "Image")]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Display(Name = "Available")]
        public bool IsAvailable { get; set; }

        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue)]
        public int Stock { get; set; } = 0;

        [StringLength(100)]
        [Display(Name = "SKU")]
        public string? SKU { get; set; }

        [StringLength(100)]
        [Display(Name = "Brand")]
        public string? Brand { get; set; }

        [StringLength(100)]
        [Display(Name = "Fabric/Material")]
        public string? Fabric { get; set; }

        [Display(Name = "Weight (grams)")]
        public decimal? Weight { get; set; }

        // Additional Product Details
        [StringLength(1000)]
        [Display(Name = "Care Instructions")]
        public string? CareInstructions { get; set; }

        [StringLength(500)]
        [Display(Name = "Warranty Information")]
        public string? WarrantyInfo { get; set; }

        [StringLength(1000)]
        [Display(Name = "Product Highlights")]
        public string? Highlights { get; set; }

        [StringLength(500)]
        [Display(Name = "Country of Origin")]
        public string? CountryOfOrigin { get; set; }

        [StringLength(500)]
        [Display(Name = "Manufacturer")]
        public string? Manufacturer { get; set; }

        [StringLength(20)]
        [Display(Name = "Estimated Delivery Days")]
        public string? EstimatedDeliveryDays { get; set; }

        [Display(Name = "Is Returnable")]
        public bool IsReturnable { get; set; } = true;

        [Display(Name = "Return Days")]
        public int ReturnDays { get; set; } = 7;

        [Display(Name = "Is COD Available")]
        public bool IsCODAvailable { get; set; } = true;

        [Display(Name = "Enable 360° View")]
        public bool Has360View { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "360° View URL")]
        public string? View360Url { get; set; }

        [StringLength(500)]
        [Display(Name = "Video URL")]
        public string? VideoUrl { get; set; }

        [Display(Name = "Status")]
        public ProductStatus Status { get; set; } = ProductStatus.Active;

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "View Count")]
        public int ViewCount { get; set; } = 0;

        [Display(Name = "Sold Count")]
        public int SoldCount { get; set; } = 0;

        // Category relationship (hierarchical - points to any level of category)
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // Free-form tags (comma-separated, optional)
        [StringLength(500)]
        [Display(Name = "Tags")]
        public string? Tags { get; set; }

        // Seller relationship
        public int? SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
        public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public virtual ICollection<ProductQuestion> Questions { get; set; } = new List<ProductQuestion>();

        // Size Guide reference
        public int? SizeGuideId { get; set; }

        [ForeignKey("SizeGuideId")]
        public virtual SizeGuide? SizeGuide { get; set; }

        // Computed properties
        [NotMapped]
        public decimal EffectivePrice => DiscountPrice ?? Price;
        [NotMapped]
        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice.Value < Price;
        [NotMapped]
        public int DiscountPercentage => HasDiscount ? (int)((Price - DiscountPrice!.Value) / Price * 100) : 0;
    }
}
