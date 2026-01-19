using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class Category
    {
        public int Id { get; set; }

        // Self-referencing hierarchy
        [Display(Name = "Parent Category")]
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }

        // Hierarchy level (0 = root, 1 = child, 2 = grandchild, etc.)
        [Display(Name = "Level")]
        public int Level { get; set; } = 0;

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Slug")]
        public string? Slug { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [StringLength(100)]
        [Display(Name = "Icon Class")]
        public string? IconClass { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Category> Children { get; set; } = new List<Category>();
        public virtual ICollection<Products> Products { get; set; } = new List<Products>();

        // Helper properties
        [NotMapped]
        public bool HasChildren => Children?.Any() == true;

        [NotMapped]
        public bool IsRoot => ParentId == null;

        [NotMapped]
        public string FullPath { get; set; } = string.Empty;
    }
}
