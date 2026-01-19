using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class CampaignRecipient
    {
        public int Id { get; set; }

        [Required]
        public int CampaignId { get; set; }

        [ForeignKey("CampaignId")]
        public virtual PromotionalCampaign? Campaign { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Link to the notification created for this recipient
        public int? NotificationId { get; set; }

        [ForeignKey("NotificationId")]
        public virtual UserNotification? Notification { get; set; }

        public bool EmailSent { get; set; } = false;

        public DateTime? EmailSentAt { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        public DateTime? ClickedAt { get; set; }

        // Computed
        [NotMapped]
        public bool IsRead => ReadAt.HasValue;

        [NotMapped]
        public bool IsClicked => ClickedAt.HasValue;
    }
}
