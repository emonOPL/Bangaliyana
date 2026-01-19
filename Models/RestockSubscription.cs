using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Represents a customer's subscription to be notified when an out-of-stock product becomes available again.
    /// </summary>
    public class RestockSubscription
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        // For variant-specific subscriptions (e.g., specific size/color)
        public int? ProductVariantId { get; set; }

        [ForeignKey("ProductVariantId")]
        public virtual ProductVariant? ProductVariant { get; set; }

        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

        // Whether the user has been notified about restock
        public bool IsNotified { get; set; } = false;

        public DateTime? NotifiedAt { get; set; }

        // Email for guest subscriptions (optional future feature)
        [StringLength(255)]
        public string? GuestEmail { get; set; }
    }
}
