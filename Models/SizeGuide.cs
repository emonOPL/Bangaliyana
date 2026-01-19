using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    public class SizeGuide
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Guide Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<SizeGuideEntry> Entries { get; set; } = new List<SizeGuideEntry>();
        public virtual ICollection<Products> Products { get; set; } = new List<Products>();
    }

    public class SizeGuideEntry
    {
        public int Id { get; set; }

        [Required]
        public int SizeGuideId { get; set; }

        public virtual SizeGuide? SizeGuide { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Size")]
        public string Size { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Chest (inches)")]
        public string? Chest { get; set; }

        [StringLength(50)]
        [Display(Name = "Waist (inches)")]
        public string? Waist { get; set; }

        [StringLength(50)]
        [Display(Name = "Hip (inches)")]
        public string? Hip { get; set; }

        [StringLength(50)]
        [Display(Name = "Length (inches)")]
        public string? Length { get; set; }

        [StringLength(50)]
        [Display(Name = "Shoulder (inches)")]
        public string? Shoulder { get; set; }

        [StringLength(50)]
        [Display(Name = "Sleeve Length (inches)")]
        public string? SleeveLength { get; set; }

        [StringLength(50)]
        [Display(Name = "Inseam (inches)")]
        public string? Inseam { get; set; }

        [StringLength(50)]
        [Display(Name = "Neck (inches)")]
        public string? Neck { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;
    }
}
