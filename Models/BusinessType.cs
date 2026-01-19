using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    public class BusinessType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(50)]
        [Display(Name = "Icon Class")]
        public string? IconClass { get; set; }  // FontAwesome icon class

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Seller> Sellers { get; set; } = new List<Seller>();
    }
}
