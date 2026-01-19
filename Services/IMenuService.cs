using System.Security.Claims;
using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface IMenuService
    {
        /// <summary>
        /// Gets menu items accessible to the current user for a specific location
        /// </summary>
        Task<List<MenuItem>> GetMenuItemsForUserAsync(ClaimsPrincipal? user, MenuLocation location);

        /// <summary>
        /// Gets all menu items (for admin management)
        /// </summary>
        Task<List<MenuItem>> GetAllMenuItemsAsync();

        /// <summary>
        /// Gets a single menu item by ID
        /// </summary>
        Task<MenuItem?> GetMenuItemByIdAsync(int id);

        /// <summary>
        /// Creates a new menu item
        /// </summary>
        Task<MenuItem> CreateMenuItemAsync(MenuItem item);

        /// <summary>
        /// Updates an existing menu item
        /// </summary>
        Task<MenuItem> UpdateMenuItemAsync(MenuItem item);

        /// <summary>
        /// Deletes a menu item
        /// </summary>
        Task DeleteMenuItemAsync(int id);

        /// <summary>
        /// Updates permissions for a menu item
        /// </summary>
        Task UpdateMenuPermissionsAsync(int menuItemId, List<string> allowedRoles);

        /// <summary>
        /// Gets permissions for a menu item
        /// </summary>
        Task<List<MenuPermission>> GetMenuPermissionsAsync(int menuItemId);

        /// <summary>
        /// Gets all available roles
        /// </summary>
        Task<List<string>> GetAvailableRolesAsync();

        /// <summary>
        /// Seeds default menu items if none exist
        /// </summary>
        Task SeedDefaultMenuItemsAsync();

        /// <summary>
        /// Update existing menu items to mark sensitive menus with proper PermissionType
        /// </summary>
        Task UpdateSensitiveMenuPermissionsAsync();

        /// <summary>
        /// Reorders menu items within a location
        /// </summary>
        Task ReorderMenuItemsAsync(int location, List<int> menuItemIds);

        /// <summary>
        /// Gets hierarchical menu structure for a location
        /// </summary>
        Task<List<MenuItemViewModel>> GetMenuHierarchyAsync(ClaimsPrincipal? user, MenuLocation location);

        /// <summary>
        /// Check if user can access a specific menu considering individual permissions
        /// </summary>
        Task<bool> CanUserAccessMenuAsync(string userId, int menuItemId);

        /// <summary>
        /// Get user-specific menu permissions
        /// </summary>
        Task<List<UserMenuPermission>> GetUserMenuPermissionsAsync(string userId);

        /// <summary>
        /// Update user-specific menu permissions
        /// </summary>
        Task UpdateUserMenuPermissionsAsync(string userId, List<int> grantedMenuIds, List<int> revokedMenuIds);
    }

    public class MenuItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Url { get; set; }
        public string? Area { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public bool IsActive { get; set; }
        public List<MenuItemViewModel> Children { get; set; } = new();
    }
}
