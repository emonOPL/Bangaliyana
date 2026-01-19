using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Seasonal collection for fashion products
    /// </summary>
    public class SeasonalCollection
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Collection Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Slug")]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Tagline")]
        public string? Tagline { get; set; }

        [StringLength(2000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(255)]
        [Display(Name = "Banner Image URL")]
        public string? BannerImageUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Mobile Banner Image URL")]
        public string? MobileBannerImageUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Thumbnail Image URL")]
        public string? ThumbnailImageUrl { get; set; }

        [Display(Name = "Season Type")]
        public SeasonType SeasonType { get; set; } = SeasonType.AllSeason;

        [Display(Name = "Year")]
        public int Year { get; set; } = DateTime.UtcNow.Year;

        // Color theming
        [StringLength(50)]
        [Display(Name = "Primary Color")]
        public string? PrimaryColor { get; set; }

        [StringLength(50)]
        [Display(Name = "Secondary Color")]
        public string? SecondaryColor { get; set; }

        [StringLength(50)]
        [Display(Name = "Text Color")]
        public string? TextColor { get; set; }

        // Link to products
        [StringLength(1000)]
        [Display(Name = "Product IDs")]
        public string? ProductIds { get; set; }

        // Link to categories
        [StringLength(500)]
        [Display(Name = "Category IDs")]
        public string? CategoryIds { get; set; }

        [StringLength(500)]
        [Url]
        [Display(Name = "Collection URL")]
        public string? CollectionUrl { get; set; }

        [StringLength(100)]
        [Display(Name = "Button Text")]
        public string? ButtonText { get; set; }

        [StringLength(500)]
        [Display(Name = "Meta Description")]
        public string? MetaDescription { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Show Countdown")]
        public bool ShowCountdown { get; set; } = false;

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
        public List<int> ProductIdList => string.IsNullOrEmpty(ProductIds)
            ? new List<int>()
            : ProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id.Trim(), out int result) ? result : 0)
                .Where(id => id > 0)
                .ToList();

        [NotMapped]
        public List<int> CategoryIdList => string.IsNullOrEmpty(CategoryIds)
            ? new List<int>()
            : CategoryIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id.Trim(), out int result) ? result : 0)
                .Where(id => id > 0)
                .ToList();
    }

    /// <summary>
    /// Season types for collections
    /// </summary>
    public enum SeasonType
    {
        [Display(Name = "All Season")]
        AllSeason = 0,

        [Display(Name = "Summer")]
        Summer = 1,

        [Display(Name = "Winter")]
        Winter = 2,

        [Display(Name = "Monsoon")]
        Monsoon = 3,

        [Display(Name = "Spring")]
        Spring = 4,

        [Display(Name = "Autumn")]
        Autumn = 5,

        [Display(Name = "Eid Collection")]
        Eid = 6,

        [Display(Name = "Puja Collection")]
        Puja = 7,

        [Display(Name = "Wedding Season")]
        Wedding = 8,

        [Display(Name = "New Year")]
        NewYear = 9,

        [Display(Name = "Pohela Boishakh")]
        PohelaBoishakh = 10,

        [Display(Name = "Valentine's Day")]
        Valentine = 11,

        [Display(Name = "Festival")]
        Festival = 12
    }
}
