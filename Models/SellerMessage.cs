using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class SellerMessage
    {
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [ForeignKey("ConversationId")]
        public virtual SellerConversation? Conversation { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [ForeignKey("SenderId")]
        public virtual ApplicationUser? Sender { get; set; }

        // True if sender is seller, false if sender is buyer
        public bool IsSentBySeller { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        // Optional attachment
        [StringLength(500)]
        public string? AttachmentUrl { get; set; }

        [StringLength(100)]
        public string? AttachmentName { get; set; }

        [StringLength(50)]
        public string? AttachmentType { get; set; }  // image, document, etc.

        // Shared content
        public int? SharedOrderId { get; set; }

        [ForeignKey("SharedOrderId")]
        public virtual Order? SharedOrder { get; set; }

        public int? SharedProductId { get; set; }

        [ForeignKey("SharedProductId")]
        public virtual Products? SharedProduct { get; set; }

        // Message type
        [StringLength(20)]
        public string MessageType { get; set; } = "text";  // text, shared_order, shared_product, escalation, rating_request

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Edit/Delete tracking
        public bool IsEdited { get; set; } = false;

        public DateTime? EditedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
    }
}
