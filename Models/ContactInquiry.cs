using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Contact form inquiry status
    /// </summary>
    public enum InquiryStatus
    {
        New,
        InProgress,
        Responded,
        Closed
    }

    /// <summary>
    /// Contact form inquiry category
    /// </summary>
    public enum InquiryCategory
    {
        General,
        Sales,
        Support,
        Partnership,
        Feedback,
        Complaint,
        Other
    }

    /// <summary>
    /// Stores contact form submissions from customers
    /// </summary>
    public class ContactInquiry
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public InquiryCategory Category { get; set; } = InquiryCategory.General;

        [Display(Name = "Status")]
        public InquiryStatus Status { get; set; } = InquiryStatus.New;

        // Optional: Logged in user
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // Admin response
        [Display(Name = "Admin Response")]
        public string? AdminResponse { get; set; }

        public string? RespondedById { get; set; }
        public ApplicationUser? RespondedBy { get; set; }

        public DateTime? RespondedAt { get; set; }

        // Tracking
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
