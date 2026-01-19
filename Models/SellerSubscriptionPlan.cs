using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class SellerSubscriptionPlan
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Plan Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Monthly Price")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000)]
        public decimal MonthlyPrice { get; set; }

        [Display(Name = "Max Products")]
        [Range(-1, 10000)]
        public int MaxProducts { get; set; } = 10; // -1 for unlimited

        [Required]
        [Display(Name = "Commission Rate (%)")]
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal CommissionRate { get; set; } = 5m;

        [Display(Name = "Has Priority Support")]
        public bool HasPrioritySupport { get; set; } = false;

        [Display(Name = "Has Featured Listing")]
        public bool HasFeaturedListing { get; set; } = false;

        [Display(Name = "Has Advanced Analytics")]
        public bool HasAdvancedAnalytics { get; set; } = false;

        [Display(Name = "Has Promotional Tools")]
        public bool HasPromotionalTools { get; set; } = false;

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "Badge Color")]
        [StringLength(50)]
        public string? BadgeColor { get; set; } // CSS class like "bg-primary", "bg-success", etc.

        [Display(Name = "Icon Class")]
        [StringLength(100)]
        public string? IconClass { get; set; } // FontAwesome icon class

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; } = false; // Show as recommended plan

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<SellerSubscription> Subscriptions { get; set; } = new List<SellerSubscription>();
    }
}
