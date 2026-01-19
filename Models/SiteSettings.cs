using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Stores global website configuration and branding
    /// </summary>
    public class SiteSettings
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Site Name")]
        public string SiteName { get; set; } = "Bangaliyana";

        [StringLength(200)]
        [Display(Name = "Site Tagline")]
        public string? SiteTagline { get; set; }

        [StringLength(500)]
        [Display(Name = "Site Description (SEO)")]
        public string? SiteDescription { get; set; }

        [StringLength(500)]
        [Display(Name = "Site Keywords (SEO)")]
        public string? SiteKeywords { get; set; }

        [StringLength(100)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Display(Name = "Founding Year")]
        public int FoundingYear { get; set; } = 2020;

        [StringLength(255)]
        [Display(Name = "Logo URL")]
        public string? LogoUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Dark Mode Logo URL")]
        public string? DarkModeLogoUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Favicon URL")]
        public string? FaviconUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Footer Logo URL")]
        public string? FooterLogoUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Dark Mode Footer Logo URL")]
        public string? DarkModeFooterLogoUrl { get; set; }

        // Contact Information
        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Contact Email")]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        [Phone]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        [Display(Name = "Contact Address")]
        public string? ContactAddress { get; set; }

        // Business Settings
        [Display(Name = "Default Delivery Charge")]
        [Range(0, 10000)]
        public decimal DefaultDeliveryCharge { get; set; } = 100m;

        [Display(Name = "Free Delivery Threshold")]
        [Range(0, 100000)]
        public decimal? FreeDeliveryThreshold { get; set; }

        [StringLength(10)]
        [Display(Name = "Currency Symbol")]
        public string CurrencySymbol { get; set; } = "৳";

        [StringLength(10)]
        [Display(Name = "Currency Code")]
        public string CurrencyCode { get; set; } = "BDT";

        // Mobile Financial Services (MFS) Settings
        [StringLength(20)]
        [Phone]
        [Display(Name = "bKash Number")]
        public string? BkashNumber { get; set; }

        [StringLength(20)]
        [Phone]
        [Display(Name = "Nagad Number")]
        public string? NagadNumber { get; set; }

        [StringLength(20)]
        [Phone]
        [Display(Name = "Rocket Number")]
        public string? RocketNumber { get; set; }

        [StringLength(20)]
        [Phone]
        [Display(Name = "Upay Number")]
        public string? UpayNumber { get; set; }

        // Theme Settings
        [StringLength(50)]
        [Display(Name = "Primary Color")]
        public string PrimaryColor { get; set; } = "#6366f1";

        [StringLength(50)]
        [Display(Name = "Secondary Color")]
        public string SecondaryColor { get; set; } = "#8b5cf6";

        [StringLength(50)]
        [Display(Name = "Accent Color")]
        public string AccentColor { get; set; } = "#06b6d4";

        [Display(Name = "Enable Dark Mode Toggle")]
        public bool EnableDarkMode { get; set; } = true;

        // Footer Content
        [StringLength(1000)]
        [Display(Name = "Footer About Text")]
        public string? FooterAboutText { get; set; }

        [StringLength(500)]
        [Display(Name = "Copyright Text")]
        public string? CopyrightText { get; set; }

        // Maintenance
        [Display(Name = "Maintenance Mode")]
        public bool IsMaintenanceMode { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "Maintenance Message")]
        public string? MaintenanceMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
