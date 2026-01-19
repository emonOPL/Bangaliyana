using System.Security.Claims;
using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public class MenuService : IMenuService
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<MenuService> _logger;

        public MenuService(
            ApplicationDbContext context,
            RoleManager<IdentityRole> roleManager,
            ILogger<MenuService> logger)
        {
            _context = context;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<List<MenuItem>> GetMenuItemsForUserAsync(ClaimsPrincipal? user, MenuLocation location)
        {
            var query = _context.MenuItems
                .Include(m => m.Children.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder))
                .Include(m => m.Permissions)
                .Where(m => m.IsActive && m.Location == location && m.ParentId == null)
                .OrderBy(m => m.DisplayOrder)
                .AsQueryable();

            var menuItems = await query.ToListAsync();

            // Filter based on user permissions
            return menuItems.Where(m => CanUserAccessMenu(user, m)).ToList();
        }

        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            return await _context.MenuItems
                .Include(m => m.Children.OrderBy(c => c.DisplayOrder))
                .Include(m => m.Permissions)
                .OrderBy(m => m.Location)
                .ThenBy(m => m.DisplayOrder)
                .ToListAsync();
        }

        public async Task<MenuItem?> GetMenuItemByIdAsync(int id)
        {
            return await _context.MenuItems
                .Include(m => m.Children)
                .Include(m => m.Permissions)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MenuItem> CreateMenuItemAsync(MenuItem item)
        {
            item.CreatedAt = DateTime.UtcNow;
            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created menu item: {Name}", item.Name);
            return item;
        }

        public async Task<MenuItem> UpdateMenuItemAsync(MenuItem item)
        {
            var existing = await _context.MenuItems.FindAsync(item.Id);
            if (existing == null)
            {
                throw new InvalidOperationException($"Menu item with ID {item.Id} not found");
            }

            existing.Name = item.Name;
            existing.Icon = item.Icon;
            existing.Url = item.Url;
            existing.Area = item.Area;
            existing.Controller = item.Controller;
            existing.Action = item.Action;
            existing.ParentId = item.ParentId;
            existing.DisplayOrder = item.DisplayOrder;
            existing.IsActive = item.IsActive;
            existing.Location = item.Location;
            existing.RequiredRoles = item.RequiredRoles;
            existing.IsDefault = item.IsDefault;
            existing.RequiresAuthentication = item.RequiresAuthentication;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated menu item: {Name}", item.Name);
            return existing;
        }

        public async Task DeleteMenuItemAsync(int id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Children)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null)
            {
                throw new InvalidOperationException($"Menu item with ID {id} not found");
            }

            // Delete children first
            if (item.Children.Any())
            {
                _context.MenuItems.RemoveRange(item.Children);
            }

            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted menu item: {Name}", item.Name);
        }

        public async Task UpdateMenuPermissionsAsync(int menuItemId, List<string> allowedRoles)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Permissions)
                .FirstOrDefaultAsync(m => m.Id == menuItemId);

            if (menuItem == null)
            {
                throw new InvalidOperationException($"Menu item with ID {menuItemId} not found");
            }

            // Remove existing permissions
            _context.MenuPermissions.RemoveRange(menuItem.Permissions);

            // Add new permissions
            foreach (var role in allowedRoles)
            {
                menuItem.Permissions.Add(new MenuPermission
                {
                    MenuItemId = menuItemId,
                    RoleName = role,
                    CanAccess = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Update RequiredRoles string
            menuItem.RequiredRoles = string.Join(",", allowedRoles);
            menuItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated permissions for menu item {Id}: {Roles}", menuItemId, string.Join(", ", allowedRoles));
        }

        public async Task<List<MenuPermission>> GetMenuPermissionsAsync(int menuItemId)
        {
            return await _context.MenuPermissions
                .Where(p => p.MenuItemId == menuItemId)
                .ToListAsync();
        }

        public async Task<List<string>> GetAvailableRolesAsync()
        {
            return await _roleManager.Roles
                .Select(r => r.Name!)
                .Where(n => n != null)
                .ToListAsync();
        }

        /// <summary>
        /// Update existing menu items to mark sensitive menus with proper PermissionType
        /// This ensures sensitive menus (User Management, Menu Management, Settings) are not auto-granted
        /// </summary>
        public async Task UpdateSensitiveMenuPermissionsAsync()
        {
            _logger.LogInformation("Updating sensitive menu permissions...");

            // List of sensitive controller names that require SuperAdmin to grant
            var sensitiveControllers = new[] { "UserManagement", "RoleManagement", "MenuManagement", "Settings", "SiteContent" };
            var sensitiveNames = new[] { "Users", "User Management", "Role Management", "Menu Management", "Settings", "Site Settings" };

            var menuItems = await _context.MenuItems.ToListAsync();
            var updated = 0;

            foreach (var menu in menuItems)
            {
                bool isSensitive = sensitiveControllers.Contains(menu.Controller) ||
                                   sensitiveNames.Contains(menu.Name);

                if (isSensitive && menu.PermissionType != PermissionType.Sensitive)
                {
                    menu.PermissionType = PermissionType.Sensitive;
                    menu.TargetRole = "Admin";
                    menu.UpdatedAt = DateTime.UtcNow;
                    updated++;
                    _logger.LogInformation("Marked menu '{Name}' as Sensitive", menu.Name);
                }
                // Set General for other Admin menus that don't have a PermissionType set
                else if (!isSensitive && menu.Location == MenuLocation.AdminSidebar &&
                         menu.PermissionType == PermissionType.General &&
                         string.IsNullOrEmpty(menu.TargetRole))
                {
                    menu.TargetRole = "Admin";
                    menu.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (updated > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated {Count} menu items with Sensitive permission type", updated);
            }
        }

        /// <summary>
        /// Ensures the Returns menu item exists in Admin Sidebar
        /// </summary>
        private async Task EnsureReturnsMenuItemExistsAsync()
        {
            var returnsMenuExists = await _context.MenuItems
                .AnyAsync(m => m.Controller == "Orders" && m.Action == "Returns" && m.Location == MenuLocation.AdminSidebar);

            if (!returnsMenuExists)
            {
                _logger.LogInformation("Adding Returns menu item...");

                // Find the Orders menu to get its display order and place Returns after it
                var ordersMenu = await _context.MenuItems
                    .FirstOrDefaultAsync(m => m.Controller == "Orders" && m.Action == "Index" && m.Location == MenuLocation.AdminSidebar);

                var displayOrder = ordersMenu?.DisplayOrder + 1 ?? 4;

                var returnsMenuItem = new MenuItem
                {
                    Name = "Returns",
                    Icon = "fas fa-undo-alt",
                    Area = "Admin",
                    Controller = "Orders",
                    Action = "Returns",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = displayOrder,
                    RequiredRoles = "Admin",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.General,
                    TargetRole = "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                _context.MenuItems.Add(returnsMenuItem);
                await _context.SaveChangesAsync();

                // Add permission for Admin role
                _context.MenuPermissions.Add(new MenuPermission
                {
                    MenuItemId = returnsMenuItem.Id,
                    RoleName = "Admin",
                    CanAccess = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Added Returns menu item with ID {Id}", returnsMenuItem.Id);
            }
        }

        /// <summary>
        /// Ensures messaging and promotional campaign menu items exist in Admin Sidebar
        /// </summary>
        private async Task EnsureMessagingMenuItemsExistAsync()
        {
            // Add Seller Messages menu
            var sellerMessagesExists = await _context.MenuItems
                .AnyAsync(m => m.Controller == "SellerMessages" && m.Location == MenuLocation.AdminSidebar);

            if (!sellerMessagesExists)
            {
                _logger.LogInformation("Adding Seller Messages menu item...");

                var maxDisplayOrder = await _context.MenuItems
                    .Where(m => m.Location == MenuLocation.AdminSidebar)
                    .MaxAsync(m => (int?)m.DisplayOrder) ?? 10;

                var sellerMessagesMenuItem = new MenuItem
                {
                    Name = "Seller Messages",
                    Icon = "fas fa-comment-dots",
                    Area = "Admin",
                    Controller = "SellerMessages",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = maxDisplayOrder + 1,
                    RequiredRoles = "Admin,Seller",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.General,
                    TargetRole = "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                _context.MenuItems.Add(sellerMessagesMenuItem);
                await _context.SaveChangesAsync();

                // Add permissions for Admin and Seller roles
                _context.MenuPermissions.Add(new MenuPermission
                {
                    MenuItemId = sellerMessagesMenuItem.Id,
                    RoleName = "Admin",
                    CanAccess = true,
                    CreatedAt = DateTime.UtcNow
                });
                _context.MenuPermissions.Add(new MenuPermission
                {
                    MenuItemId = sellerMessagesMenuItem.Id,
                    RoleName = "Seller",
                    CanAccess = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Added Seller Messages menu item with ID {Id}", sellerMessagesMenuItem.Id);
            }

            // Add Promotional Campaigns menu
            var campaignsExists = await _context.MenuItems
                .AnyAsync(m => m.Controller == "PromotionalCampaigns" && m.Location == MenuLocation.AdminSidebar);

            if (!campaignsExists)
            {
                _logger.LogInformation("Adding Promotional Campaigns menu item...");

                var maxDisplayOrder = await _context.MenuItems
                    .Where(m => m.Location == MenuLocation.AdminSidebar)
                    .MaxAsync(m => (int?)m.DisplayOrder) ?? 10;

                var campaignsMenuItem = new MenuItem
                {
                    Name = "Campaigns",
                    Icon = "fas fa-bullhorn",
                    Area = "Admin",
                    Controller = "PromotionalCampaigns",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = maxDisplayOrder + 1,
                    RequiredRoles = "Admin",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.General,
                    TargetRole = "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                _context.MenuItems.Add(campaignsMenuItem);
                await _context.SaveChangesAsync();

                // Add permission for Admin role
                _context.MenuPermissions.Add(new MenuPermission
                {
                    MenuItemId = campaignsMenuItem.Id,
                    RoleName = "Admin",
                    CanAccess = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Added Promotional Campaigns menu item with ID {Id}", campaignsMenuItem.Id);
            }
        }

        /// <summary>
        /// Ensures the Reviews menu item exists in Admin Sidebar
        /// </summary>
        private async Task EnsureReviewsMenuItemExistsAsync()
        {
            var reviewsMenuExists = await _context.MenuItems
                .AnyAsync(m => m.Controller == "Reviews" && m.Action == "Index" && m.Location == MenuLocation.AdminSidebar);

            if (!reviewsMenuExists)
            {
                _logger.LogInformation("Adding Reviews menu item...");

                // Find the Orders menu to get its display order and place Reviews after it
                var ordersMenu = await _context.MenuItems
                    .FirstOrDefaultAsync(m => m.Controller == "Orders" && m.Action == "Index" && m.Location == MenuLocation.AdminSidebar);

                var displayOrder = ordersMenu?.DisplayOrder + 2 ?? 5; // After Returns

                var reviewsMenuItem = new MenuItem
                {
                    Name = "Reviews",
                    Icon = "fas fa-star",
                    Area = "Admin",
                    Controller = "Reviews",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = displayOrder,
                    RequiredRoles = "Admin",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.General,
                    TargetRole = "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                _context.MenuItems.Add(reviewsMenuItem);
                await _context.SaveChangesAsync();

                // Add permission for Admin role
                _context.MenuPermissions.Add(new MenuPermission
                {
                    MenuItemId = reviewsMenuItem.Id,
                    RoleName = "Admin",
                    CanAccess = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Added Reviews menu item with ID {Id}", reviewsMenuItem.Id);
            }
        }

        public async Task SeedDefaultMenuItemsAsync()
        {
            if (await _context.MenuItems.AnyAsync())
            {
                _logger.LogInformation("Menu items already exist, skipping seed");
                // Still update sensitive permissions for existing menus
                await UpdateSensitiveMenuPermissionsAsync();
                // Add Returns menu item if it doesn't exist
                await EnsureReturnsMenuItemExistsAsync();
                // Add messaging and campaign menu items if they don't exist
                await EnsureMessagingMenuItemsExistAsync();
                // Add Reviews menu item if it doesn't exist
                await EnsureReviewsMenuItemExistsAsync();
                return;
            }

            _logger.LogInformation("Seeding default menu items...");

            var menuItems = new List<MenuItem>
            {
                // Navbar - Default items (visible to all)
                new MenuItem
                {
                    Name = "Home",
                    Icon = "fas fa-home",
                    Area = "Customer",
                    Controller = "Home",
                    Action = "Index",
                    Location = MenuLocation.Navbar,
                    DisplayOrder = 1,
                    IsDefault = true,
                    IsActive = true
                },
                new MenuItem
                {
                    Name = "Products",
                    Icon = "fas fa-box",
                    Area = "Customer",
                    Controller = "Home",
                    Action = "Products",
                    Location = MenuLocation.Navbar,
                    DisplayOrder = 2,
                    IsDefault = true,
                    IsActive = true
                },

                // Admin Sidebar items - General permissions (auto-granted to Admin role)
                new MenuItem
                {
                    Name = "Dashboard",
                    Icon = "fas fa-tachometer-alt",
                    Area = "Admin",
                    Controller = "Dashboard",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = 1,
                    RequiredRoles = "Admin",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.General,
                    TargetRole = "Admin"
                },
                new MenuItem
                {
                    Name = "Products",
                    Icon = "fas fa-box",
                    Area = "Admin",
                    Controller = "Products",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = 2,
                    RequiredRoles = "Admin,Seller",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.General,
                    TargetRole = "Admin"
                },
                new MenuItem
                {
                    Name = "Orders",
                    Icon = "fas fa-shopping-cart",
                    Area = "Admin",
                    Controller = "Orders",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = 3,
                    RequiredRoles = "Admin,Seller",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.General,
                    TargetRole = "Admin"
                },
                new MenuItem
                {
                    Name = "Returns",
                    Icon = "fas fa-undo-alt",
                    Area = "Admin",
                    Controller = "Orders",
                    Action = "Returns",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = 4,
                    RequiredRoles = "Admin",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.General,
                    TargetRole = "Admin"
                },
                new MenuItem
                {
                    Name = "Seller Payments",
                    Icon = "fas fa-money-bill-wave",
                    Area = "Admin",
                    Controller = "SellerPayment",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = 5,
                    RequiredRoles = "Admin,Moderator",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.General,
                    TargetRole = "Admin"
                },
                new MenuItem
                {
                    Name = "Categories",
                    Icon = "fas fa-folder",
                    Area = "Admin",
                    Controller = "Categories",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = 5,
                    RequiredRoles = "Admin",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.General,
                    TargetRole = "Admin"
                },
                // SENSITIVE - User Management: Only SuperAdmin can grant this
                new MenuItem
                {
                    Name = "Users",
                    Icon = "fas fa-users",
                    Area = "Admin",
                    Controller = "UserManagement",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = 6,
                    RequiredRoles = "Admin",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.Sensitive,
                    TargetRole = "Admin"
                },
                // SENSITIVE - Menu Management: Only SuperAdmin can grant this
                new MenuItem
                {
                    Name = "Menu Management",
                    Icon = "fas fa-bars",
                    Area = "Admin",
                    Controller = "MenuManagement",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = 7,
                    RequiredRoles = "Admin",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.Sensitive,
                    TargetRole = "Admin"
                },
                // SENSITIVE - Settings: Only SuperAdmin can grant this
                new MenuItem
                {
                    Name = "Settings",
                    Icon = "fas fa-cog",
                    Area = "Admin",
                    Controller = "Settings",
                    Action = "Index",
                    Location = MenuLocation.AdminSidebar,
                    DisplayOrder = 8,
                    RequiredRoles = "Admin",
                    RequiresAuthentication = true,
                    IsActive = true,
                    PermissionType = PermissionType.Sensitive,
                    TargetRole = "Admin"
                },

                // User Dropdown items
                new MenuItem
                {
                    Name = "My Profile",
                    Icon = "fas fa-user",
                    Url = "/Identity/Account/Manage",
                    Location = MenuLocation.UserDropdown,
                    DisplayOrder = 1,
                    RequiresAuthentication = true,
                    IsActive = true
                },
                new MenuItem
                {
                    Name = "My Orders",
                    Icon = "fas fa-shopping-bag",
                    Area = "Customer",
                    Controller = "Home",
                    Action = "MyOrders",
                    Location = MenuLocation.UserDropdown,
                    DisplayOrder = 2,
                    RequiresAuthentication = true,
                    IsActive = true
                },
                new MenuItem
                {
                    Name = "Wishlist",
                    Icon = "fas fa-heart",
                    Area = "Customer",
                    Controller = "Home",
                    Action = "Wishlist",
                    Location = MenuLocation.UserDropdown,
                    DisplayOrder = 3,
                    RequiresAuthentication = true,
                    IsActive = true
                },

                // Footer items
                new MenuItem
                {
                    Name = "About Us",
                    Icon = "fas fa-info-circle",
                    Area = "Customer",
                    Controller = "Home",
                    Action = "About",
                    Location = MenuLocation.Footer,
                    DisplayOrder = 1,
                    IsDefault = true,
                    IsActive = true
                },
                new MenuItem
                {
                    Name = "Contact",
                    Icon = "fas fa-envelope",
                    Area = "Customer",
                    Controller = "Home",
                    Action = "Contact",
                    Location = MenuLocation.Footer,
                    DisplayOrder = 2,
                    IsDefault = true,
                    IsActive = true
                },
                new MenuItem
                {
                    Name = "Privacy Policy",
                    Icon = "fas fa-shield-alt",
                    Area = "Customer",
                    Controller = "Home",
                    Action = "Privacy",
                    Location = MenuLocation.Footer,
                    DisplayOrder = 3,
                    IsDefault = true,
                    IsActive = true
                },
                new MenuItem
                {
                    Name = "Terms of Service",
                    Icon = "fas fa-file-contract",
                    Area = "Customer",
                    Controller = "Home",
                    Action = "Terms",
                    Location = MenuLocation.Footer,
                    DisplayOrder = 4,
                    IsDefault = true,
                    IsActive = true
                }
            };

            _context.MenuItems.AddRange(menuItems);
            await _context.SaveChangesAsync();

            // Add permissions for role-specific menus
            var adminMenus = menuItems.Where(m => m.RequiredRoles?.Contains("Admin") == true).ToList();
            foreach (var menu in adminMenus)
            {
                var roles = menu.RequiredRoles!.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var role in roles)
                {
                    _context.MenuPermissions.Add(new MenuPermission
                    {
                        MenuItemId = menu.Id,
                        RoleName = role.Trim(),
                        CanAccess = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} default menu items", menuItems.Count);
        }

        public async Task ReorderMenuItemsAsync(int location, List<int> menuItemIds)
        {
            var menuItems = await _context.MenuItems
                .Where(m => (int)m.Location == location)
                .ToListAsync();

            for (int i = 0; i < menuItemIds.Count; i++)
            {
                var item = menuItems.FirstOrDefault(m => m.Id == menuItemIds[i]);
                if (item != null)
                {
                    item.DisplayOrder = i + 1;
                    item.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Reordered menu items for location {Location}", location);
        }

        public async Task<List<MenuItemViewModel>> GetMenuHierarchyAsync(ClaimsPrincipal? user, MenuLocation location)
        {
            var menuItems = await GetMenuItemsForUserAsync(user, location);

            return menuItems.Select(m => new MenuItemViewModel
            {
                Id = m.Id,
                Name = m.Name,
                Icon = m.Icon,
                Url = m.Url,
                Area = m.Area,
                Controller = m.Controller,
                Action = m.Action,
                IsActive = m.IsActive,
                Children = m.Children
                    .Where(c => CanUserAccessMenu(user, c))
                    .Select(c => new MenuItemViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Icon = c.Icon,
                        Url = c.Url,
                        Area = c.Area,
                        Controller = c.Controller,
                        Action = c.Action,
                        IsActive = c.IsActive
                    }).ToList()
            }).ToList();
        }

        private bool CanUserAccessMenu(ClaimsPrincipal? user, MenuItem menu)
        {
            // SuperAdmin has access to ALL menus - bypass all restrictions
            if (user?.Identity?.IsAuthenticated == true && user.IsInRole("SuperAdmin"))
            {
                return true;
            }

            // If menu is default (visible to all), allow access
            if (menu.IsDefault)
            {
                return true;
            }

            // If menu requires authentication
            if (menu.RequiresAuthentication && (user == null || !user.Identity?.IsAuthenticated == true))
            {
                return false;
            }

            // If no specific roles required, allow authenticated users
            if (string.IsNullOrEmpty(menu.RequiredRoles))
            {
                return true;
            }

            // Check if user is authenticated
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return false;
            }

            // Check if user has any of the required roles
            var requiredRoles = menu.RequiredRoles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());

            return requiredRoles.Any(role => user.IsInRole(role));
        }

        /// <summary>
        /// Check if user can access menu considering individual user-level permissions
        /// </summary>
        public async Task<bool> CanUserAccessMenuAsync(string userId, int menuItemId)
        {
            // SuperAdmin has access to ALL menus - bypass all restrictions
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToListAsync();

                if (userRoles.Contains("SuperAdmin"))
                {
                    return true;
                }
            }

            // Check for user-specific permission override
            var userPermission = await _context.UserMenuPermissions
                .FirstOrDefaultAsync(p => p.UserId == userId && p.MenuItemId == menuItemId);

            if (userPermission != null)
            {
                // User has a specific permission set (either granted or revoked)
                return userPermission.CanAccess;
            }

            // Fall back to role-based permissions (return null to indicate no override)
            return true; // Default to true if no specific override
        }

        /// <summary>
        /// Get user-specific menu permissions
        /// </summary>
        public async Task<List<UserMenuPermission>> GetUserMenuPermissionsAsync(string userId)
        {
            return await _context.UserMenuPermissions
                .Include(p => p.MenuItem)
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// Update user-specific menu permissions
        /// </summary>
        public async Task UpdateUserMenuPermissionsAsync(string userId, List<int> grantedMenuIds, List<int> revokedMenuIds)
        {
            // Remove existing permissions for this user
            var existingPermissions = await _context.UserMenuPermissions
                .Where(p => p.UserId == userId)
                .ToListAsync();
            _context.UserMenuPermissions.RemoveRange(existingPermissions);

            // Add granted permissions
            foreach (var menuId in grantedMenuIds)
            {
                _context.UserMenuPermissions.Add(new UserMenuPermission
                {
                    UserId = userId,
                    MenuItemId = menuId,
                    CanAccess = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Add revoked permissions
            foreach (var menuId in revokedMenuIds)
            {
                _context.UserMenuPermissions.Add(new UserMenuPermission
                {
                    UserId = userId,
                    MenuItemId = menuId,
                    CanAccess = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated menu permissions for user {UserId}: Granted={GrantedCount}, Revoked={RevokedCount}",
                userId, grantedMenuIds.Count, revokedMenuIds.Count);
        }
    }
}
