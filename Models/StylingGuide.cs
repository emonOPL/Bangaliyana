using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Fashion styling guide / Tips for styling
    /// </summary>
    public class StylingGuide
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Slug")]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Subtitle")]
        public string? Subtitle { get; set; }

        [Display(Name = "Content")]
        public string? Content { get; set; }

        [StringLength(255)]
        [Display(Name = "Cover Image URL")]
        public string? CoverImageUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Thumbnail Image URL")]
        public string? ThumbnailImageUrl { get; set; }

        [Display(Name = "Guide Type")]
        public StylingGuideType GuideType { get; set; } = StylingGuideType.General;

        // Target audience
        [StringLength(50)]
        [Display(Name = "Target Gender")]
        public string? TargetGender { get; set; }

        [StringLength(100)]
        [Display(Name = "Occasion")]
        public string? Occasion { get; set; }

        [StringLength(100)]
        [Display(Name = "Season")]
        public string? Season { get; set; }

        // Related products for cross-selling
        [StringLength(500)]
        [Display(Name = "Related Product IDs")]
        public string? RelatedProductIds { get; set; }

        [StringLength(500)]
        [Display(Name = "Meta Description")]
        public string? MetaDescription { get; set; }

        [StringLength(500)]
        [Display(Name = "Meta Keywords")]
        public string? MetaKeywords { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "View Count")]
        public int ViewCount { get; set; } = 0;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Helper properties
        [NotMapped]
        public List<int> RelatedProductIdList => string.IsNullOrEmpty(RelatedProductIds)
            ? new List<int>()
            : RelatedProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id.Trim(), out int result) ? result : 0)
                .Where(id => id > 0)
                .ToList();
    }

    /// <summary>
    /// Type of styling guide
    /// </summary>
    public enum StylingGuideType
    {
        [Display(Name = "General")]
        General = 0,

        [Display(Name = "Color Matching")]
        ColorMatching = 1,

        [Display(Name = "Body Type")]
        BodyType = 2,

        [Display(Name = "Occasion Based")]
        OccasionBased = 3,

        [Display(Name = "Season Based")]
        SeasonBased = 4,

        [Display(Name = "Trend Alert")]
        TrendAlert = 5,

        [Display(Name = "Celebrity Style")]
        CelebrityStyle = 6,

        [Display(Name = "DIY Styling")]
        DIYStyling = 7
    }
}
