using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum NotificationType
    {
        Order,
        Support,
        Promo,
        Welcome,
        Festival,
        General,
        Seller,      // Seller message notifications
        PriceDrop    // Price drop alerts
    }

    public class UserNotification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "general";

        [Required]
        [StringLength(50)]
        public string Icon { get; set; } = "fa-bell";

        [StringLength(50)]
        public string IconColor { get; set; } = "text-primary";

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        // Related entity IDs for navigation
        public int? OrderId { get; set; }
        public int? TicketId { get; set; }
        public int? CouponId { get; set; }
        public int? ConversationId { get; set; }  // Seller message conversation
        public int? CampaignId { get; set; }      // Promotional campaign
        public int? ProductId { get; set; }       // For price drop alerts

        // Festival/Promo specific
        [StringLength(50)]
        public string? PromoCode { get; set; }

        [StringLength(100)]
        public string? FestivalName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }

        [ForeignKey("ConversationId")]
        public virtual SellerConversation? Conversation { get; set; }

        [ForeignKey("CampaignId")]
        public virtual PromotionalCampaign? Campaign { get; set; }

        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }
    }

    public class NotificationPreference
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Master email toggle - when false, no promotional/marketing emails are sent
        public bool ReceiveEmails { get; set; } = true;

        public bool OrderUpdates { get; set; } = true;
        public bool DeliveryAlerts { get; set; } = true;
        public bool Promotions { get; set; } = true;
        public bool SupportReplies { get; set; } = true;
        public bool PriceDrops { get; set; } = true;
        public bool RestockAlerts { get; set; } = true;
        public bool Newsletter { get; set; } = false;
        public bool FestivalOffers { get; set; } = true;
        public bool SellerMessages { get; set; } = true;

        // Followed Shops notifications (global for all followed shops)
        public bool FollowedShopNewProducts { get; set; } = true;
        public bool FollowedShopPriceDrops { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // Festival dates configuration
    public class FestivalDate
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? NameBangla { get; set; }

        // For fixed date festivals (English New Year, Bangla New Year)
        public int? FixedMonth { get; set; }
        public int? FixedDay { get; set; }

        // For lunar/variable date festivals (Eid, Puja)
        // These need to be updated yearly
        public DateTime? NextOccurrence { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, 30)]
        public int NotifyDaysBefore { get; set; } = 7;

        [Range(1, 20)]
        public int DiscountPercentage { get; set; } = 5;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
