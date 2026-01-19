using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Type of audit action performed
    /// </summary>
    public enum AuditActionType
    {
        Create,
        Update,
        Delete,
        View,
        Login,
        Logout,
        Export,
        Import,
        Approve,
        Reject,
        Assign,
        [Display(Name = "Status Change")]
        StatusChange,
        Other
    }

    /// <summary>
    /// Module/Area where the action was performed
    /// </summary>
    public enum AuditModule
    {
        Dashboard,
        Products,
        Categories,
        Orders,
        Users,
        Roles,
        Sellers,
        Payments,
        Coupons,
        Campaigns,
        Support,
        Settings,
        Blog,
        Reports,
        Inventory,
        Notifications,
        Authentication,
        Other
    }

    /// <summary>
    /// Audit log entry to track admin/staff actions
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }

        /// <summary>
        /// User who performed the action
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        /// <summary>
        /// Email of the user (stored for quick reference even if user is deleted)
        /// </summary>
        [Required]
        [StringLength(256)]
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Name of the user
        /// </summary>
        [StringLength(200)]
        public string? UserName { get; set; }

        /// <summary>
        /// Role of the user at the time of action
        /// </summary>
        [StringLength(100)]
        public string? UserRole { get; set; }

        /// <summary>
        /// Type of action performed
        /// </summary>
        public AuditActionType ActionType { get; set; }

        /// <summary>
        /// Module where action was performed
        /// </summary>
        public AuditModule Module { get; set; }

        /// <summary>
        /// Controller name
        /// </summary>
        [StringLength(100)]
        public string? Controller { get; set; }

        /// <summary>
        /// Action method name
        /// </summary>
        [StringLength(100)]
        public string? Action { get; set; }

        /// <summary>
        /// Human-readable description of the action
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Entity type that was affected (e.g., "Product", "Order", "User")
        /// </summary>
        [StringLength(100)]
        public string? EntityType { get; set; }

        /// <summary>
        /// ID of the affected entity
        /// </summary>
        [StringLength(100)]
        public string? EntityId { get; set; }

        /// <summary>
        /// Name/Title of the affected entity for display
        /// </summary>
        [StringLength(200)]
        public string? EntityName { get; set; }

        /// <summary>
        /// Old values before change (JSON)
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// New values after change (JSON)
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// IP address of the user
        /// </summary>
        [StringLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent/browser info
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Full URL that was accessed
        /// </summary>
        [StringLength(1000)]
        public string? RequestUrl { get; set; }

        /// <summary>
        /// HTTP method (GET, POST, PUT, DELETE)
        /// </summary>
        [StringLength(10)]
        public string? HttpMethod { get; set; }

        /// <summary>
        /// Was the action successful?
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// Error message if action failed
        /// </summary>
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional metadata (JSON)
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Timestamp of the action
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
