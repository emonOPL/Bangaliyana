using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class SellerConversation
    {
        public int Id { get; set; }

        [Required]
        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        [Required]
        public string BuyerId { get; set; } = string.Empty;

        [ForeignKey("BuyerId")]
        public virtual ApplicationUser? Buyer { get; set; }

        // Optional references for context
        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public int? ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }

        [StringLength(200)]
        public string? Subject { get; set; }

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        public int UnreadCountBuyer { get; set; } = 0;

        public int UnreadCountSeller { get; set; } = 0;

        public bool IsClosedByBuyer { get; set; } = false;

        public bool IsClosedBySeller { get; set; } = false;

        public bool IsClosedByStaff { get; set; } = false;

        // Track who closed the conversation
        public string? ClosedByUserId { get; set; }

        [ForeignKey("ClosedByUserId")]
        public virtual ApplicationUser? ClosedByUser { get; set; }

        public bool IsArchived { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<SellerMessage> Messages { get; set; } = new List<SellerMessage>();
    }
}
