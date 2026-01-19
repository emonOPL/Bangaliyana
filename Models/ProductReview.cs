using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required]
        [Range(1, 5)]
        [Display(Name = "Rating")]
        public int Rating { get; set; }

        [StringLength(100)]
        [Display(Name = "Review Title")]
        public string? Title { get; set; }

        [StringLength(2000)]
        [Display(Name = "Review Comment")]
        public string? Comment { get; set; }

        [StringLength(500)]
        [Display(Name = "Pros")]
        public string? Pros { get; set; }

        [StringLength(500)]
        [Display(Name = "Cons")]
        public string? Cons { get; set; }

        [Display(Name = "Is Verified Purchase")]
        public bool IsVerifiedPurchase { get; set; } = false;

        [Display(Name = "Is Approved")]
        public bool IsApproved { get; set; } = false;

        [Display(Name = "Helpful Count")]
        public int HelpfulCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Reward Points tracking
        [Display(Name = "Points Awarded")]
        public bool PointsAwarded { get; set; } = false;

        [Display(Name = "Points Awarded At")]
        public DateTime? PointsAwardedAt { get; set; }

        // Navigation for review images
        public virtual ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();

        // Navigation for reply
        public virtual ReviewReply? Reply { get; set; }
    }

    public class ReviewImage
    {
        public int Id { get; set; }

        [Required]
        public int ReviewId { get; set; }

        [ForeignKey("ReviewId")]
        public virtual ProductReview? Review { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
