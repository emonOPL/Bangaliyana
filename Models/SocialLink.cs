using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Stores social media links for footer and other sections
    /// </summary>
    public class SocialLink
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Platform Name")]
        public string PlatformName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Url]
        [Display(Name = "URL")]
        public string Url { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Icon Class (Font Awesome)")]
        public string IconClass { get; set; } = "fas fa-link";

        [StringLength(50)]
        [Display(Name = "Icon Color")]
        public string? IconColor { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Open in New Tab")]
        public bool OpenInNewTab { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
