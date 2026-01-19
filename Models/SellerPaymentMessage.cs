using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class SellerPaymentMessage
    {
        public int Id { get; set; }

        [Required]
        public int SellerPaymentId { get; set; }

        [ForeignKey("SellerPaymentId")]
        public virtual SellerPayment? SellerPayment { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [ForeignKey("SenderId")]
        public virtual ApplicationUser? Sender { get; set; }

        [Required]
        [StringLength(2000)]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Is Staff Reply")]
        public bool IsStaffReply { get; set; } = false;

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Attachments
        public virtual ICollection<SellerPaymentMessageAttachment> Attachments { get; set; } = new List<SellerPaymentMessageAttachment>();
    }

    public class SellerPaymentMessageAttachment
    {
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }

        [ForeignKey("MessageId")]
        public virtual SellerPaymentMessage? Message { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "File Name")]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "File URL")]
        public string FileUrl { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Content Type")]
        public string? ContentType { get; set; }

        [Display(Name = "File Size")]
        public long? FileSize { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
