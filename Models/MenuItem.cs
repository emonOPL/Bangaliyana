using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;  // Display name

        [MaxLength(100)]
        public string? Icon { get; set; }  // FontAwesome icon class (e.g., "fas fa-home")

        [MaxLength(500)]
        public string? Url { get; set; }  // Direct URL or route

        [MaxLength(50)]
        public string? Area { get; set; }  // MVC Area (e.g., "Admin", "Customer")

        [MaxLength(100)]
        public string? Controller { get; set; }  // Controller name

        [MaxLength(100)]
        public string? Action { get; set; }  // Action name

        public int? ParentId { get; set; }  // For nested/dropdown menus

        public int DisplayOrder { get; set; }  // Order in menu

        public bool IsActive { get; set; } = true;

        public MenuLocation Location { get; set; }  // Where to display this menu

        [MaxLength(200)]
        public string? RequiredRoles { get; set; }  // Comma-separated roles: "Admin,Seller"

        public bool IsDefault { get; set; }  // Show to all users by default (including anonymous)

        public bool RequiresAuthentication { get; set; }  // Only show to logged-in users

        /// <summary>
        /// Permission type for automatic permission assignment
        /// General/Required = auto-granted on role change
        /// Special/Sensitive = only SuperAdmin can grant/revoke
        /// </summary>
        public PermissionType PermissionType { get; set; } = PermissionType.General;

        /// <summary>
        /// Target role for this menu item's automatic permission
        /// E.g., "User", "Seller", "Admin"
        /// </summary>
        [MaxLength(50)]
        public string? TargetRole { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ParentId")]
        public virtual MenuItem? Parent { get; set; }

        public virtual ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();

        public virtual ICollection<MenuPermission> Permissions { get; set; } = new List<MenuPermission>();
    }

    public class MenuPermission
    {
        public int Id { get; set; }

        public int MenuItemId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RoleName { get; set; } = string.Empty;  // "Admin", "Seller", "User"

        public bool CanAccess { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("MenuItemId")]
        public virtual MenuItem MenuItem { get; set; } = null!;
    }

    public enum MenuLocation
    {
        Navbar = 0,           // Top navigation bar
        AdminSidebar = 1,     // Admin panel sidebar
        CustomerSidebar = 2,  // Customer dashboard sidebar
        Footer = 3,           // Footer links
        UserDropdown = 4      // User profile dropdown menu
    }

    /// <summary>
    /// Permission type categorizes menu items for automatic permission assignment
    /// </summary>
    public enum PermissionType
    {
        /// <summary>
        /// General permissions - automatically granted to all users of a role
        /// </summary>
        General = 0,

        /// <summary>
        /// Required permissions - essential for role functionality, auto-granted
        /// </summary>
        Required = 1,

        /// <summary>
        /// Special permissions - only SuperAdmin can grant/revoke
        /// </summary>
        Special = 2,

        /// <summary>
        /// Sensitive permissions - only SuperAdmin can grant/revoke, requires extra caution
        /// </summary>
        Sensitive = 3
    }

    /// <summary>
    /// Individual user-level menu permission override
    /// This allows admins to grant or revoke specific menu access for individual users
    /// </summary>
    public class UserMenuPermission
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int MenuItemId { get; set; }

        public bool CanAccess { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey("MenuItemId")]
        public virtual MenuItem MenuItem { get; set; } = null!;
    }
}
