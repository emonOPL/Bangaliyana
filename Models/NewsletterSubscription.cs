using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    public class NewsletterSubscription
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Name { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UnsubscribedAt { get; set; }

        [StringLength(100)]
        public string? Source { get; set; } // e.g., "Homepage", "Footer", "Popup"
    }
}
