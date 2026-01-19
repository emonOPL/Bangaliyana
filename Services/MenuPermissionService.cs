using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Service for managing automatic menu permissions based on role changes
    /// </summary>
    public class MenuPermissionService : IMenuPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MenuPermissionService> _logger;
        private readonly string[] _superAdminEmails;

        /// <summary>
        /// Default fallback SuperAdmin emails if configuration is not set
        /// </summary>
        private static readonly string[] DefaultSuperAdminEmails = new[]
        {
            "admin@bangaliyana.com"
        };

        /// <summary>
        /// SuperAdmin emails that have full control over all permissions
        /// These users can manage Special and Sensitive permissions for others
        /// </summary>
        public string[] SuperAdminEmails => _superAdminEmails;

        public MenuPermissionService(
            ApplicationDbContext context,
            ILogger<MenuPermissionService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _superAdminEmails = configuration.GetSection("SuperAdminSettings:SuperAdminEmails").Get<string[]>() ?? DefaultSuperAdminEmails;
        }

        /// <summary>
        /// Check if a user is a SuperAdmin
        /// </summary>
        public bool IsSuperAdmin(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            return SuperAdminEmails.Any(e =>
                e.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check if the current user can manage special/sensitive permissions
        /// </summary>
        public bool CanManageSpecialPermissions(string currentUserEmail)
        {
            return IsSuperAdmin(currentUserEmail);
        }

        /// <summary>
        /// Assign all general and required permissions for a specific role to a user
        /// Called when user is promoted to a role
        /// </summary>
        public async Task AssignRolePermissionsAsync(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleName))
            {
                _logger.LogWarning("AssignRolePermissionsAsync called with empty userId or roleName");
                return;
            }

            // Get all menu items that:
            // 1. Have this role as target OR have this role in RequiredRoles
            // 2. Are General or Required permission type (auto-granted)
            var menuItems = await _context.MenuItems
                .Where(m => m.IsActive)
                .Where(m =>
                    (m.TargetRole == roleName ||
                     (m.RequiredRoles != null && m.RequiredRoles.Contains(roleName))) &&
                    (m.PermissionType == PermissionType.General || m.PermissionType == PermissionType.Required))
                .ToListAsync();

            // Also include child menu items
            var parentIds = menuItems.Select(m => m.Id).ToList();
            var childMenuItems = await _context.MenuItems
                .Where(m => m.IsActive && m.ParentId != null && parentIds.Contains(m.ParentId.Value))
                .Where(m => m.PermissionType == PermissionType.General || m.PermissionType == PermissionType.Required)
                .ToListAsync();

            menuItems.AddRange(childMenuItems);
            menuItems = menuItems.DistinctBy(m => m.Id).ToList();

            // Get existing permissions for this user
            var existingPermissions = await _context.UserMenuPermissions
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var existingMenuIds = existingPermissions.Select(p => p.MenuItemId).ToHashSet();

            // Add new permissions for menus not already assigned
            var newPermissions = new List<UserMenuPermission>();
            foreach (var menu in menuItems)
            {
                if (!existingMenuIds.Contains(menu.Id))
                {
                    newPermissions.Add(new UserMenuPermission
                    {
                        UserId = userId,
                        MenuItemId = menu.Id,
                        CanAccess = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    // Update existing permission to grant access
                    var existing = existingPermissions.First(p => p.MenuItemId == menu.Id);
                    if (!existing.CanAccess)
                    {
                        existing.CanAccess = true;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            if (newPermissions.Any())
            {
                _context.UserMenuPermissions.AddRange(newPermissions);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Assigned {Count} permissions for role {Role} to user {UserId}",
                menuItems.Count, roleName, userId);
        }

        /// <summary>
        /// Remove all permissions for a specific role from a user
        /// Called when user is demoted from a role
        /// </summary>
        public async Task RemoveRolePermissionsAsync(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleName))
            {
                _logger.LogWarning("RemoveRolePermissionsAsync called with empty userId or roleName");
                return;
            }

            // Get all menu items associated with this role
            var menuItemIds = await _context.MenuItems
                .Where(m => m.TargetRole == roleName ||
                           (m.RequiredRoles != null && m.RequiredRoles.Contains(roleName)))
                .Select(m => m.Id)
                .ToListAsync();

            // Also get child items
            var childMenuItemIds = await _context.MenuItems
                .Where(m => m.ParentId != null && menuItemIds.Contains(m.ParentId.Value))
                .Select(m => m.Id)
                .ToListAsync();

            menuItemIds.AddRange(childMenuItemIds);
            menuItemIds = menuItemIds.Distinct().ToList();

            // Remove or revoke permissions
            var permissionsToRemove = await _context.UserMenuPermissions
                .Where(p => p.UserId == userId && menuItemIds.Contains(p.MenuItemId))
                .ToListAsync();

            _context.UserMenuPermissions.RemoveRange(permissionsToRemove);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Removed {Count} permissions for role {Role} from user {UserId}",
                permissionsToRemove.Count, roleName, userId);
        }

        /// <summary>
        /// Grant a specific special/sensitive permission to a user
        /// Only SuperAdmin can call this
        /// </summary>
        public async Task GrantSpecialPermissionAsync(string userId, int menuItemId, string grantedByEmail)
        {
            if (!IsSuperAdmin(grantedByEmail))
            {
                _logger.LogWarning(
                    "Non-SuperAdmin {Email} attempted to grant special permission to user {UserId}",
                    grantedByEmail, userId);
                throw new UnauthorizedAccessException("Only SuperAdmin can grant special/sensitive permissions");
            }

            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem == null)
            {
                throw new InvalidOperationException($"Menu item {menuItemId} not found");
            }

            // Check if permission already exists
            var existingPermission = await _context.UserMenuPermissions
                .FirstOrDefaultAsync(p => p.UserId == userId && p.MenuItemId == menuItemId);

            if (existingPermission != null)
            {
                existingPermission.CanAccess = true;
                existingPermission.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.UserMenuPermissions.Add(new UserMenuPermission
                {
                    UserId = userId,
                    MenuItemId = menuItemId,
                    CanAccess = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "SuperAdmin {Email} granted special permission for menu {MenuId} to user {UserId}",
                grantedByEmail, menuItemId, userId);
        }

        /// <summary>
        /// Revoke a specific special/sensitive permission from a user
        /// Only SuperAdmin can call this
        /// </summary>
        public async Task RevokeSpecialPermissionAsync(string userId, int menuItemId, string revokedByEmail)
        {
            if (!IsSuperAdmin(revokedByEmail))
            {
                _logger.LogWarning(
                    "Non-SuperAdmin {Email} attempted to revoke special permission from user {UserId}",
                    revokedByEmail, userId);
                throw new UnauthorizedAccessException("Only SuperAdmin can revoke special/sensitive permissions");
            }

            var permission = await _context.UserMenuPermissions
                .FirstOrDefaultAsync(p => p.UserId == userId && p.MenuItemId == menuItemId);

            if (permission != null)
            {
                permission.CanAccess = false;
                permission.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "SuperAdmin {Email} revoked special permission for menu {MenuId} from user {UserId}",
                    revokedByEmail, menuItemId, userId);
            }
        }

        /// <summary>
        /// Get all menu items grouped by permission type for a specific role
        /// </summary>
        public async Task<Dictionary<PermissionType, List<MenuItem>>> GetMenuItemsByPermissionTypeAsync(string roleName)
        {
            var menuItems = await _context.MenuItems
                .Where(m => m.IsActive)
                .Where(m => m.TargetRole == roleName ||
                           (m.RequiredRoles != null && m.RequiredRoles.Contains(roleName)))
                .OrderBy(m => m.Location)
                .ThenBy(m => m.DisplayOrder)
                .ToListAsync();

            return menuItems
                .GroupBy(m => m.PermissionType)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// Get user's current permissions with details
        /// </summary>
        public async Task<List<UserMenuPermissionDetail>> GetUserPermissionDetailsAsync(string userId)
        {
            var userPermissions = await _context.UserMenuPermissions
                .Include(p => p.MenuItem)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return userPermissions.Select(p => new UserMenuPermissionDetail
            {
                MenuItemId = p.MenuItemId,
                MenuItemName = p.MenuItem.Name,
                MenuItemIcon = p.MenuItem.Icon,
                Location = p.MenuItem.Location,
                PermissionType = p.MenuItem.PermissionType,
                TargetRole = p.MenuItem.TargetRole,
                CanAccess = p.CanAccess,
                IsAutoGranted = p.MenuItem.PermissionType == PermissionType.General ||
                               p.MenuItem.PermissionType == PermissionType.Required,
                GrantedAt = p.CreatedAt
            }).ToList();
        }

        /// <summary>
        /// Assign ALL permissions to a SuperAdmin user
        /// SuperAdmin has access to every single menu item regardless of role or permission type
        /// </summary>
        public async Task AssignAllPermissionsToSuperAdminAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("AssignAllPermissionsToSuperAdminAsync called with empty userId");
                return;
            }

            // Get ALL active menu items
            var allMenuItems = await _context.MenuItems
                .Where(m => m.IsActive)
                .ToListAsync();

            // Get existing permissions for this user
            var existingPermissions = await _context.UserMenuPermissions
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var existingMenuIds = existingPermissions.Select(p => p.MenuItemId).ToHashSet();

            // Add permissions for all menus not already assigned
            var newPermissions = new List<UserMenuPermission>();
            foreach (var menu in allMenuItems)
            {
                if (!existingMenuIds.Contains(menu.Id))
                {
                    newPermissions.Add(new UserMenuPermission
                    {
                        UserId = userId,
                        MenuItemId = menu.Id,
                        CanAccess = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    // Ensure access is granted
                    var existing = existingPermissions.First(p => p.MenuItemId == menu.Id);
                    if (!existing.CanAccess)
                    {
                        existing.CanAccess = true;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            if (newPermissions.Any())
            {
                _context.UserMenuPermissions.AddRange(newPermissions);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Assigned ALL {Count} permissions to SuperAdmin user {UserId}",
                allMenuItems.Count, userId);
        }
    }
}
