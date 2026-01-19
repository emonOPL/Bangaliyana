using Bangaliyana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bangaliyana.Filters
{
    /// <summary>
    /// Authorization filter that checks module-based permissions
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(string module, string permission = "View")
            : base(typeof(PermissionAuthorizationFilter))
        {
            Arguments = new object[] { module, permission };
        }
    }

    /// <summary>
    /// Filter that performs the permission check
    /// </summary>
    public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly IPermissionService _permissionService;
        private readonly string _module;
        private readonly string _permission;

        public PermissionAuthorizationFilter(
            IPermissionService permissionService,
            string module,
            string permission)
        {
            _permissionService = permissionService;
            _module = module;
            _permission = permission;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new ForbidResult();
                return;
            }

            var permissions = await _permissionService.GetPermissionsAsync(userId, _module);

            bool hasPermission = _permission.ToLower() switch
            {
                "view" => permissions.CanView,
                "create" => permissions.CanCreate,
                "edit" => permissions.CanEdit,
                "delete" => permissions.CanDelete,
                _ => false
            };

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
            }
        }
    }

    /// <summary>
    /// Helper methods for permission checking in controllers
    /// </summary>
    public static class PermissionHelper
    {
        /// <summary>
        /// Get the permission service from HttpContext
        /// </summary>
        public static IPermissionService GetPermissionService(this Controller controller)
        {
            return controller.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        }

        /// <summary>
        /// Check if current user can view the module
        /// </summary>
        public static async Task<bool> CanViewAsync(this Controller controller, string module)
        {
            var userId = controller.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return false;

            var service = controller.GetPermissionService();
            return await service.CanViewAsync(userId, module);
        }

        /// <summary>
        /// Check if current user can create in the module
        /// </summary>
        public static async Task<bool> CanCreateAsync(this Controller controller, string module)
        {
            var userId = controller.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return false;

            var service = controller.GetPermissionService();
            return await service.CanCreateAsync(userId, module);
        }

        /// <summary>
        /// Check if current user can edit in the module
        /// </summary>
        public static async Task<bool> CanEditAsync(this Controller controller, string module)
        {
            var userId = controller.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return false;

            var service = controller.GetPermissionService();
            return await service.CanEditAsync(userId, module);
        }

        /// <summary>
        /// Check if current user can delete in the module
        /// </summary>
        public static async Task<bool> CanDeleteAsync(this Controller controller, string module)
        {
            var userId = controller.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return false;

            var service = controller.GetPermissionService();
            return await service.CanDeleteAsync(userId, module);
        }

        /// <summary>
        /// Get all permissions for the current user on a module
        /// </summary>
        public static async Task<PermissionResult> GetPermissionsAsync(this Controller controller, string module)
        {
            var userId = controller.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return new PermissionResult();

            var service = controller.GetPermissionService();
            return await service.GetPermissionsAsync(userId, module);
        }
    }
}
