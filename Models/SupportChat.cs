using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Live support chat session status
    /// </summary>
    public enum ChatSessionStatus
    {
        Waiting,        // Customer waiting for agent
        Active,         // Agent connected, chat in progress
        OnHold,         // Chat temporarily on hold
        Closed,         // Chat ended
        Abandoned       // Customer left before agent connected
    }

    /// <summary>
    /// Live support chat session
    /// </summary>
    public class SupportChatSession
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SessionCode { get; set; } = string.Empty;

        // Customer info (can be guest or logged in user)
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Visitor Name")]
        public string VisitorName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Visitor Email")]
        public string VisitorEmail { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Initial Query")]
        public string? InitialQuery { get; set; }

        [Display(Name = "Department")]
        public InquiryCategory Department { get; set; } = InquiryCategory.Support;

        // Support agent
        public string? AgentId { get; set; }
        public ApplicationUser? Agent { get; set; }

        [Display(Name = "Status")]
        public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Waiting;

        // Rating after chat ends
        [Range(1, 5)]
        public int? Rating { get; set; }

        [StringLength(500)]
        public string? Feedback { get; set; }

        // Tracking
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? PageUrl { get; set; }

        // SignalR connection tracking
        public string? CustomerConnectionId { get; set; }
        public string? AgentConnectionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AgentJoinedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<SupportChatMessage> Messages { get; set; } = new List<SupportChatMessage>();
    }

    /// <summary>
    /// Individual message in a support chat session
    /// </summary>
    public class SupportChatMessage
    {
        public int Id { get; set; }

        public int SessionId { get; set; }

        [ForeignKey("SessionId")]
        public SupportChatSession Session { get; set; } = null!;

        [Required]
        public string Content { get; set; } = string.Empty;

        // Who sent the message
        public string? SenderId { get; set; }
        public ApplicationUser? Sender { get; set; }

        [Display(Name = "Is Agent Message")]
        public bool IsAgentMessage { get; set; }

        [Display(Name = "Is System Message")]
        public bool IsSystemMessage { get; set; }

        // Attachments (optional)
        [StringLength(500)]
        public string? AttachmentUrl { get; set; }

        [StringLength(100)]
        public string? AttachmentName { get; set; }

        [StringLength(50)]
        public string? AttachmentType { get; set; }

        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        // User feedback on this specific message
        public bool? WasHelpful { get; set; }

        [StringLength(500)]
        public string? UserFeedback { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
