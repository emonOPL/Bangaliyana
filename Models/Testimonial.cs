using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Stores customer testimonials and reviews (both admin-added and user-submitted)
    /// </summary>
    public class Testimonial
    {
        public int Id { get; set; }

        // User-submitted review - nullable for admin-added testimonials
        [StringLength(450)]
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Customer Title/Role")]
        public string? CustomerTitle { get; set; }

        [StringLength(100)]
        [Display(Name = "Customer Location")]
        public string? CustomerLocation { get; set; }

        [StringLength(255)]
        [Display(Name = "Customer Photo URL")]
        public string? PhotoUrl { get; set; }

        [Required]
        [StringLength(1000)]
        [Display(Name = "Testimonial Text")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Rating (1-5)")]
        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Approved")]
        public bool IsApproved { get; set; } = true;

        // Indicates if this is a user-submitted review vs admin-added
        [Display(Name = "Is User Submitted")]
        public bool IsUserSubmitted { get; set; } = false;

        // Cached user verification status
        public bool IsVerifiedUser { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Reward Points tracking for platform reviews
        [Display(Name = "Points Awarded")]
        public bool PointsAwarded { get; set; } = false;

        [Display(Name = "Points Awarded At")]
        public DateTime? PointsAwardedAt { get; set; }
    }
}
