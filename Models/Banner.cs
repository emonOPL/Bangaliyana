using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Defines the location where a banner is displayed
    /// </summary>
    public enum BannerLocation
    {
        [Display(Name = "Hero Section")]
        Hero = 0,

        [Display(Name = "Home Page Top")]
        HomeTop = 1,

        [Display(Name = "Home Page Middle")]
        HomeMiddle = 2,

        [Display(Name = "Home Page Bottom")]
        HomeBottom = 3,

        [Display(Name = "Sidebar")]
        Sidebar = 4,

        [Display(Name = "Announcement Bar")]
        Announcement = 5,

        [Display(Name = "Popup")]
        Popup = 6
    }

    /// <summary>
    /// Stores banners and promotional content for the website
    /// </summary>
    public class Banner
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Subtitle")]
        public string? Subtitle { get; set; }

        [StringLength(2000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(255)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Mobile Image URL")]
        public string? MobileImageUrl { get; set; }

        [StringLength(255)]
        [Display(Name = "Background Color")]
        public string? BackgroundColor { get; set; }

        [StringLength(255)]
        [Display(Name = "Text Color")]
        public string? TextColor { get; set; }

        [StringLength(500)]
        [Url]
        [Display(Name = "Link URL")]
        public string? LinkUrl { get; set; }

        [StringLength(100)]
        [Display(Name = "Button Text")]
        public string? ButtonText { get; set; }

        [StringLength(50)]
        [Display(Name = "Button Style")]
        public string ButtonStyle { get; set; } = "primary";

        [Display(Name = "Location")]
        public BannerLocation Location { get; set; } = BannerLocation.Hero;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Show on Mobile")]
        public bool ShowOnMobile { get; set; } = true;

        [Display(Name = "Show on Desktop")]
        public bool ShowOnDesktop { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Check if banner should be displayed based on date range
        /// </summary>
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
    }
}
