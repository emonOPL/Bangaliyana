using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class ReviewReply
    {
        public int Id { get; set; }

        [Required]
        public int ProductReviewId { get; set; }

        [ForeignKey("ProductReviewId")]
        public virtual ProductReview? ProductReview { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(2000)]
        [Display(Name = "Reply")]
        public string Reply { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
