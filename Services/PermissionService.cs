using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Service implementation for module-based permission checking
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Check if a user is SuperAdmin (has all permissions)
        /// </summary>
        public async Task<bool> IsSuperAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            return await _userManager.IsInRoleAsync(user, "SuperAdmin");
        }

        /// <summary>
        /// Get permissions for a user on a specific module
        /// </summary>
        public async Task<PermissionResult> GetPermissionsAsync(string userId, string module)
        {
            // SuperAdmin has all permissions
            if (await IsSuperAdminAsync(userId))
            {
                return new PermissionResult
                {
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true
                };
            }

            // Check if user is Admin - they have all permissions too
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return new PermissionResult
                {
                    CanView = true,
                    CanCreate = true,
                    CanEdit = true,
                    CanDelete = true
                };
            }

            // Get user's assigned admin roles
            var userRoleIds = await _context.AdminRoleAssignments
                .Where(a => a.UserId == userId)
                .Select(a => a.AdminRoleId)
                .ToListAsync();

            if (!userRoleIds.Any())
            {
                return new PermissionResult();
            }

            // Get combined permissions for the module from all assigned roles
            var permissions = await _context.AdminPermissions
                .Where(p => userRoleIds.Contains(p.AdminRoleId) && p.Module == module)
                .ToListAsync();

            // Combine permissions (OR logic - if any role grants permission, user has it)
            return new PermissionResult
            {
                CanView = permissions.Any(p => p.CanView),
                CanCreate = permissions.Any(p => p.CanCreate),
                CanEdit = permissions.Any(p => p.CanEdit),
                CanDelete = permissions.Any(p => p.CanDelete)
            };
        }

        public async Task<bool> CanViewAsync(string userId, string module)
        {
            var permissions = await GetPermissionsAsync(userId, module);
            return permissions.CanView;
        }

        public async Task<bool> CanCreateAsync(string userId, string module)
        {
            var permissions = await GetPermissionsAsync(userId, module);
            return permissions.CanCreate;
        }

        public async Task<bool> CanEditAsync(string userId, string module)
        {
            var permissions = await GetPermissionsAsync(userId, module);
            return permissions.CanEdit;
        }

        public async Task<bool> CanDeleteAsync(string userId, string module)
        {
            var permissions = await GetPermissionsAsync(userId, module);
            return permissions.CanDelete;
        }

        /// <summary>
        /// Get all permissions for a user across all modules
        /// </summary>
        public async Task<Dictionary<string, PermissionResult>> GetAllPermissionsAsync(string userId)
        {
            var result = new Dictionary<string, PermissionResult>();
            var modules = AdminModules.GetAllModules();

            foreach (var module in modules)
            {
                result[module] = await GetPermissionsAsync(userId, module);
            }

            return result;
        }

        /// <summary>
        /// Get all AdminRoles
        /// </summary>
        public async Task<List<AdminRole>> GetAllRolesAsync()
        {
            return await _context.AdminRoles
                .Include(r => r.Permissions)
                .Include(r => r.Assignments)
                .OrderBy(r => r.IsSystemRole ? 0 : 1)
                .ThenBy(r => r.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Get AdminRole by ID
        /// </summary>
        public async Task<AdminRole?> GetRoleByIdAsync(int roleId)
        {
            return await _context.AdminRoles
                .Include(r => r.Permissions)
                .Include(r => r.Assignments)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(r => r.Id == roleId);
        }

        /// <summary>
        /// Create a new AdminRole
        /// </summary>
        public async Task<AdminRole> CreateRoleAsync(AdminRole role)
        {
            role.CreatedAt = DateTime.UtcNow;
            role.UpdatedAt = DateTime.UtcNow;
            _context.AdminRoles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        /// <summary>
        /// Update an AdminRole
        /// </summary>
        public async Task UpdateRoleAsync(AdminRole role)
        {
            role.UpdatedAt = DateTime.UtcNow;
            _context.AdminRoles.Update(role);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Delete an AdminRole (only non-system roles)
        /// </summary>
        public async Task<bool> DeleteRoleAsync(int roleId)
        {
            var role = await _context.AdminRoles.FindAsync(roleId);
            if (role == null || role.IsSystemRole)
            {
                return false;
            }

            // Remove all assignments first
            var assignments = await _context.AdminRoleAssignments
                .Where(a => a.AdminRoleId == roleId)
                .ToListAsync();
            _context.AdminRoleAssignments.RemoveRange(assignments);

            // Remove all permissions
            var permissions = await _context.AdminPermissions
                .Where(p => p.AdminRoleId == roleId)
                .ToListAsync();
            _context.AdminPermissions.RemoveRange(permissions);

            // Remove the role
            _context.AdminRoles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Update permissions for an AdminRole
        /// </summary>
        public async Task UpdatePermissionsAsync(int roleId, List<AdminPermission> permissions)
        {
            // Remove existing permissions
            var existingPermissions = await _context.AdminPermissions
                .Where(p => p.AdminRoleId == roleId)
                .ToListAsync();
            _context.AdminPermissions.RemoveRange(existingPermissions);

            // Add new permissions
            foreach (var perm in permissions)
            {
                perm.AdminRoleId = roleId;
                perm.CreatedAt = DateTime.UtcNow;
                _context.AdminPermissions.Add(perm);
            }

            // Update role timestamp
            var role = await _context.AdminRoles.FindAsync(roleId);
            if (role != null)
            {
                role.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Assign a role to a user
        /// </summary>
        public async Task AssignRoleToUserAsync(string userId, int roleId, string assignedBy)
        {
            // Check if already assigned
            var existing = await _context.AdminRoleAssignments
                .FirstOrDefaultAsync(a => a.UserId == userId && a.AdminRoleId == roleId);

            if (existing != null)
            {
                return; // Already assigned
            }

            var assignment = new AdminRoleAssignment
            {
                UserId = userId,
                AdminRoleId = roleId,
                AssignedBy = assignedBy,
                AssignedAt = DateTime.UtcNow
            };

            _context.AdminRoleAssignments.Add(assignment);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Remove a role from a user
        /// </summary>
        public async Task RemoveRoleFromUserAsync(string userId, int roleId)
        {
            var assignment = await _context.AdminRoleAssignments
                .FirstOrDefaultAsync(a => a.UserId == userId && a.AdminRoleId == roleId);

            if (assignment != null)
            {
                _context.AdminRoleAssignments.Remove(assignment);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Get user's assigned AdminRoles
        /// </summary>
        public async Task<List<AdminRole>> GetUserRolesAsync(string userId)
        {
            return await _context.AdminRoleAssignments
                .Where(a => a.UserId == userId)
                .Include(a => a.AdminRole)
                    .ThenInclude(r => r!.Permissions)
                .Select(a => a.AdminRole!)
                .ToListAsync();
        }

        /// <summary>
        /// Get users assigned to a specific AdminRole
        /// </summary>
        public async Task<List<ApplicationUser>> GetRoleUsersAsync(int roleId)
        {
            return await _context.AdminRoleAssignments
                .Where(a => a.AdminRoleId == roleId)
                .Include(a => a.User)
                .Select(a => a.User!)
                .ToListAsync();
        }
    }
}
