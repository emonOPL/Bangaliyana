using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Service for managing automatic menu permissions based on role changes
    /// </summary>
    public interface IMenuPermissionService
    {
        /// <summary>
        /// SuperAdmin emails that have full control over all permissions
        /// </summary>
        string[] SuperAdminEmails { get; }

        /// <summary>
        /// Check if a user is a SuperAdmin
        /// </summary>
        bool IsSuperAdmin(string email);

        /// <summary>
        /// Assign all general and required permissions for a specific role to a user
        /// Called when user is promoted to a role
        /// </summary>
        Task AssignRolePermissionsAsync(string userId, string roleName);

        /// <summary>
        /// Remove all permissions for a specific role from a user
        /// Called when user is demoted from a role
        /// </summary>
        Task RemoveRolePermissionsAsync(string userId, string roleName);

        /// <summary>
        /// Grant a specific special/sensitive permission to a user
        /// Only SuperAdmin can call this
        /// </summary>
        Task GrantSpecialPermissionAsync(string userId, int menuItemId, string grantedByEmail);

        /// <summary>
        /// Revoke a specific special/sensitive permission from a user
        /// Only SuperAdmin can call this
        /// </summary>
        Task RevokeSpecialPermissionAsync(string userId, int menuItemId, string revokedByEmail);

        /// <summary>
        /// Get all menu items grouped by permission type for a specific role
        /// </summary>
        Task<Dictionary<PermissionType, List<MenuItem>>> GetMenuItemsByPermissionTypeAsync(string roleName);

        /// <summary>
        /// Get user's current permissions with details
        /// </summary>
        Task<List<UserMenuPermissionDetail>> GetUserPermissionDetailsAsync(string userId);

        /// <summary>
        /// Check if a user can manage permissions for another user
        /// Returns true only for SuperAdmins
        /// </summary>
        bool CanManageSpecialPermissions(string currentUserEmail);

        /// <summary>
        /// Assign ALL permissions to a SuperAdmin user
        /// SuperAdmin has access to every single menu item regardless of role or permission type
        /// </summary>
        Task AssignAllPermissionsToSuperAdminAsync(string userId);
    }

    /// <summary>
    /// Detailed permission information for display
    /// </summary>
    public class UserMenuPermissionDetail
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public string? MenuItemIcon { get; set; }
        public MenuLocation Location { get; set; }
        public PermissionType PermissionType { get; set; }
        public string? TargetRole { get; set; }
        public bool CanAccess { get; set; }
        public bool IsAutoGranted { get; set; }
        public DateTime? GrantedAt { get; set; }
    }
}
