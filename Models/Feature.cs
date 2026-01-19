using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Defines the section where a feature is displayed
    /// </summary>
    public enum FeatureSection
    {
        [Display(Name = "Home Page")]
        HomePage = 0,

        [Display(Name = "Login Page")]
        LoginPage = 1,

        [Display(Name = "Register Page")]
        RegisterPage = 2,

        [Display(Name = "About Page")]
        AboutPage = 3,

        [Display(Name = "Footer")]
        Footer = 4
    }

    /// <summary>
    /// Stores feature highlights shown on various pages
    /// </summary>
    public class Feature
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(100)]
        [Display(Name = "Icon Class (Font Awesome)")]
        public string IconClass { get; set; } = "fas fa-star";

        [StringLength(50)]
        [Display(Name = "Icon Color")]
        public string? IconColor { get; set; }

        [StringLength(255)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Link URL")]
        public string? LinkUrl { get; set; }

        [StringLength(50)]
        [Display(Name = "Link Text")]
        public string? LinkText { get; set; }

        [Display(Name = "Section")]
        public FeatureSection Section { get; set; } = FeatureSection.HomePage;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
