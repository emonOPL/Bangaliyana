using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum TicketStatus
    {
        [Display(Name = "Open")]
        Open,
        [Display(Name = "In Progress")]
        InProgress,
        [Display(Name = "Waiting For Customer")]
        WaitingForCustomer,
        [Display(Name = "Resolved")]
        Resolved,
        [Display(Name = "Closed")]
        Closed
    }

    public enum TicketPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum TicketCategory
    {
        General,
        Order,
        Payment,
        Delivery,
        Product,
        Refund,
        Account,
        Technical,
        Other
    }

    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Ticket Number")]
        public string TicketNumber { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public TicketCategory Category { get; set; } = TicketCategory.General;

        [Display(Name = "Priority")]
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        [Display(Name = "Status")]
        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public string? AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        public virtual ApplicationUser? AssignedTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        // Navigation
        public virtual ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    }

    public class TicketMessage
    {
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [ForeignKey("SenderId")]
        public virtual ApplicationUser? Sender { get; set; }

        [Required]
        [StringLength(5000)]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Is Staff Reply")]
        public bool IsStaffReply { get; set; } = false;

        [Display(Name = "Is Internal Note")]
        public bool IsInternalNote { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TicketAttachment
    {
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        public int? MessageId { get; set; }

        [ForeignKey("MessageId")]
        public virtual TicketMessage? TicketMessage { get; set; }

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

        [Display(Name = "File Size (bytes)")]
        public long? FileSize { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
