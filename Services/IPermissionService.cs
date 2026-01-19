using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Available admin modules for permission management
    /// </summary>
    public static class AdminModules
    {
        // Dashboard
        public const string Dashboard = "Dashboard";

        // Product Management
        public const string Products = "Products";
        public const string Categories = "Categories";
        public const string Inventory = "Inventory";
        public const string Reviews = "Reviews";

        // Order Management
        public const string Orders = "Orders";
        public const string Transactions = "Transactions";
        public const string DeliveryCharges = "DeliveryCharges";

        // Support & Customer Service
        public const string SupportTickets = "SupportTickets";
        public const string SupportChat = "SupportChat";
        public const string ContactInquiries = "ContactInquiries";

        // Content Management
        public const string Blog = "Blog";
        public const string StylingGuides = "StylingGuides";
        public const string SeasonalCollections = "SeasonalCollections";
        public const string SiteSettings = "SiteSettings";
        public const string MenuManagement = "MenuManagement";

        // User Management
        public const string Users = "Users";
        public const string Roles = "Roles";

        // Seller Management
        public const string Sellers = "Sellers";
        public const string SellerPayments = "SellerPayments";
        public const string SellerMessages = "SellerMessages";

        // Promotions & Marketing
        public const string Coupons = "Coupons";
        public const string FlashSales = "FlashSales";
        public const string PromotionalCampaigns = "PromotionalCampaigns";
        public const string CategoryPromotions = "CategoryPromotions";

        // Reports & Analytics
        public const string Reports = "Reports";

        // Other
        public const string BusinessTypes = "BusinessTypes";
        public const string QA = "QA";

        /// <summary>
        /// Get all module names
        /// </summary>
        public static string[] GetAllModules()
        {
            return new[]
            {
                Dashboard,
                Products, Categories, Inventory, Reviews,
                Orders, Transactions, DeliveryCharges,
                SupportTickets, SupportChat, ContactInquiries,
                Blog, StylingGuides, SeasonalCollections, SiteSettings, MenuManagement,
                Users, Roles,
                Sellers, SellerPayments, SellerMessages,
                Coupons, FlashSales, PromotionalCampaigns, CategoryPromotions,
                Reports,
                BusinessTypes, QA
            };
        }
    }

    /// <summary>
    /// Permission check result
    /// </summary>
    public class PermissionResult
    {
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        public bool HasAnyPermission => CanView || CanCreate || CanEdit || CanDelete;
    }

    /// <summary>
    /// Service interface for checking module-based permissions
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// Check if a user has permission to access a module
        /// </summary>
        Task<PermissionResult> GetPermissionsAsync(string userId, string module);

        /// <summary>
        /// Check if a user can view a module
        /// </summary>
        Task<bool> CanViewAsync(string userId, string module);

        /// <summary>
        /// Check if a user can create in a module
        /// </summary>
        Task<bool> CanCreateAsync(string userId, string module);

        /// <summary>
        /// Check if a user can edit in a module
        /// </summary>
        Task<bool> CanEditAsync(string userId, string module);

        /// <summary>
        /// Check if a user can delete in a module
        /// </summary>
        Task<bool> CanDeleteAsync(string userId, string module);

        /// <summary>
        /// Check if a user is SuperAdmin (has all permissions)
        /// </summary>
        Task<bool> IsSuperAdminAsync(string userId);

        /// <summary>
        /// Get all permissions for a user across all modules
        /// </summary>
        Task<Dictionary<string, PermissionResult>> GetAllPermissionsAsync(string userId);

        /// <summary>
        /// Get all AdminRoles
        /// </summary>
        Task<List<AdminRole>> GetAllRolesAsync();

        /// <summary>
        /// Get AdminRole by ID
        /// </summary>
        Task<AdminRole?> GetRoleByIdAsync(int roleId);

        /// <summary>
        /// Create a new AdminRole
        /// </summary>
        Task<AdminRole> CreateRoleAsync(AdminRole role);

        /// <summary>
        /// Update an AdminRole
        /// </summary>
        Task UpdateRoleAsync(AdminRole role);

        /// <summary>
        /// Delete an AdminRole (only non-system roles)
        /// </summary>
        Task<bool> DeleteRoleAsync(int roleId);

        /// <summary>
        /// Update permissions for an AdminRole
        /// </summary>
        Task UpdatePermissionsAsync(int roleId, List<AdminPermission> permissions);

        /// <summary>
        /// Assign a role to a user
        /// </summary>
        Task AssignRoleToUserAsync(string userId, int roleId, string assignedBy);

        /// <summary>
        /// Remove a role from a user
        /// </summary>
        Task RemoveRoleFromUserAsync(string userId, int roleId);

        /// <summary>
        /// Get user's assigned AdminRoles
        /// </summary>
        Task<List<AdminRole>> GetUserRolesAsync(string userId);

        /// <summary>
        /// Get users assigned to a specific AdminRole
        /// </summary>
        Task<List<ApplicationUser>> GetRoleUsersAsync(int roleId);
    }
}
