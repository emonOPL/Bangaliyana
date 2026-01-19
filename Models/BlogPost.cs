using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Blog post / Fashion tips article
    /// </summary>
    public class BlogPost
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
        [Display(Name = "Excerpt")]
        public string? Excerpt { get; set; }

        [Display(Name = "Content")]
        public string? Content { get; set; }

        [StringLength(255)]
        [Display(Name = "Featured Image URL")]
        public string? FeaturedImageUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Thumbnail Image URL")]
        public string? ThumbnailImageUrl { get; set; }

        [StringLength(100)]
        [Display(Name = "Author Name")]
        public string? AuthorName { get; set; }

        [StringLength(255)]
        [Display(Name = "Author Image URL")]
        public string? AuthorImageUrl { get; set; }

        // Category for organizing blog posts
        [Display(Name = "Blog Category")]
        public int? BlogCategoryId { get; set; }

        [ForeignKey("BlogCategoryId")]
        public virtual BlogCategory? Category { get; set; }

        // Tags for better searchability
        [StringLength(500)]
        [Display(Name = "Tags")]
        public string? Tags { get; set; }

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

        [Display(Name = "Published Date")]
        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Helper properties
        [NotMapped]
        public bool IsPublished => IsActive && PublishedAt.HasValue && PublishedAt.Value <= DateTime.UtcNow;

        [NotMapped]
        public List<string> TagList => string.IsNullOrEmpty(Tags)
            ? new List<string>()
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
    }

    /// <summary>
    /// Categories for blog posts
    /// </summary>
    public class BlogCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Slug")]
        public string? Slug { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(100)]
        [Display(Name = "Icon Class")]
        public string? IconClass { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<BlogPost> Posts { get; set; } = new List<BlogPost>();

        [NotMapped]
        public int PostCount => Posts?.Count(p => p.IsPublished) ?? 0;
    }
}
