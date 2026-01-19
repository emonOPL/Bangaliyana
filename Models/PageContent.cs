using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Stores dynamic page content (Privacy Policy, Terms, About, etc.)
    /// </summary>
    public class PageContent
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Page Slug")]
        public string Slug { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Page Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Page Content")]
        public string? Content { get; set; }

        [StringLength(500)]
        [Display(Name = "Meta Description")]
        public string? MetaDescription { get; set; }

        [StringLength(500)]
        [Display(Name = "Meta Keywords")]
        public string? MetaKeywords { get; set; }

        [StringLength(255)]
        [Display(Name = "Featured Image URL")]
        public string? FeaturedImageUrl { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Show in Footer")]
        public bool ShowInFooter { get; set; } = false;

        [Display(Name = "Show in Header")]
        public bool ShowInHeader { get; set; } = false;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
