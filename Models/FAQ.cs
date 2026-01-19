using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Stores frequently asked questions
    /// </summary>
    public class FAQ
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Question")]
        public string Question { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Answer")]
        public string Answer { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Helpful Count")]
        public int HelpfulCount { get; set; } = 0;

        [Display(Name = "Not Helpful Count")]
        public int NotHelpfulCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<FAQFeedback> Feedbacks { get; set; } = new List<FAQFeedback>();
    }

    /// <summary>
    /// Tracks user feedback on FAQs to prevent duplicate voting
    /// </summary>
    public class FAQFeedback
    {
        public int Id { get; set; }

        public int FAQId { get; set; }
        public virtual FAQ FAQ { get; set; } = null!;

        // UserId for logged-in users, null for guests
        public string? UserId { get; set; }

        // Session/Cookie identifier for guests
        [StringLength(100)]
        public string? SessionId { get; set; }

        // IP address as additional tracking
        [StringLength(50)]
        public string? IpAddress { get; set; }

        public bool IsHelpful { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
