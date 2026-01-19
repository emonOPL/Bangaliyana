using Bangaliyana.Data;
using Bangaliyana.Filters;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireSuperAdminRole")]
    public class AdminRolesController : Controller
    {
        private readonly IPermissionService _permissionService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public AdminRolesController(
            IPermissionService permissionService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResources> localizer)
        {
            _permissionService = permissionService;
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        // GET: Admin/AdminRoles
        public async Task<IActionResult> Index()
        {
            var roles = await _permissionService.GetAllRolesAsync();
            return View(roles);
        }

        // GET: Admin/AdminRoles/Create
        public IActionResult Create()
        {
            ViewBag.Modules = AdminModules.GetAllModules();
            return View(new AdminRole());
        }

        // POST: Admin/AdminRoles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminRole role, Dictionary<string, string[]> permissions)
        {
            if (string.IsNullOrWhiteSpace(role.Name))
            {
                ModelState.AddModelError("Name", "Role name is required");
            }

            if (ModelState.IsValid)
            {
                // Create the role
                role.IsSystemRole = false;
                var createdRole = await _permissionService.CreateRoleAsync(role);

                // Create permissions from form data
                var permissionList = new List<AdminPermission>();
                foreach (var module in AdminModules.GetAllModules())
                {
                    var modulePermissions = permissions.GetValueOrDefault(module, Array.Empty<string>());
                    if (modulePermissions.Length > 0)
                    {
                        permissionList.Add(new AdminPermission
                        {
                            AdminRoleId = createdRole.Id,
                            Module = module,
                            CanView = modulePermissions.Contains("View"),
                            CanCreate = modulePermissions.Contains("Create"),
                            CanEdit = modulePermissions.Contains("Edit"),
                            CanDelete = modulePermissions.Contains("Delete")
                        });
                    }
                }

                if (permissionList.Any())
                {
                    await _permissionService.UpdatePermissionsAsync(createdRole.Id, permissionList);
                }

                TempData["Success"] = _localizer["AdminRoleCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Modules = AdminModules.GetAllModules();
            return View(role);
        }

        // GET: Admin/AdminRoles/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _permissionService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            ViewBag.Modules = AdminModules.GetAllModules();
            return View(role);
        }

        // POST: Admin/AdminRoles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminRole role, Dictionary<string, string[]> permissions)
        {
            if (id != role.Id)
            {
                return NotFound();
            }

            var existingRole = await _permissionService.GetRoleByIdAsync(id);
            if (existingRole == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(role.Name))
            {
                ModelState.AddModelError("Name", "Role name is required");
            }

            if (ModelState.IsValid)
            {
                // Update basic role info
                existingRole.Name = role.Name;
                existingRole.Description = role.Description;
                await _permissionService.UpdateRoleAsync(existingRole);

                // Update permissions
                var permissionList = new List<AdminPermission>();
                foreach (var module in AdminModules.GetAllModules())
                {
                    var modulePermissions = permissions.GetValueOrDefault(module, Array.Empty<string>());
                    if (modulePermissions.Length > 0)
                    {
                        permissionList.Add(new AdminPermission
                        {
                            AdminRoleId = id,
                            Module = module,
                            CanView = modulePermissions.Contains("View"),
                            CanCreate = modulePermissions.Contains("Create"),
                            CanEdit = modulePermissions.Contains("Edit"),
                            CanDelete = modulePermissions.Contains("Delete")
                        });
                    }
                }

                await _permissionService.UpdatePermissionsAsync(id, permissionList);

                TempData["Success"] = _localizer["AdminRoleUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Modules = AdminModules.GetAllModules();
            return View(role);
        }

        // GET: Admin/AdminRoles/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _permissionService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            if (role.IsSystemRole)
            {
                TempData["Error"] = _localizer["CannotDeleteSystemRoles"].Value;
                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        // POST: Admin/AdminRoles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _permissionService.DeleteRoleAsync(id);
            if (result)
            {
                TempData["Success"] = _localizer["AdminRoleDeletedSuccessfully"].Value;
            }
            else
            {
                TempData["Error"] = _localizer["CannotDeleteAdminRole"].Value;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/AdminRoles/ManageAssignments/5
        public async Task<IActionResult> ManageAssignments(int id)
        {
            var role = await _permissionService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Get all admin/moderator users who can be assigned admin roles
            var adminRoleIds = await _context.Roles
                .Where(r => r.Name == "SuperAdmin" || r.Name == "Admin" || r.Name == "Moderator" ||
                           r.Name == "ProductManager" || r.Name == "OrderManager" ||
                           r.Name == "SupportTeam" || r.Name == "ContentManager")
                .Select(r => r.Id)
                .ToListAsync();

            var eligibleUserIds = await _context.UserRoles
                .Where(ur => adminRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            var eligibleUsers = await _context.Users
                .Where(u => eligibleUserIds.Contains(u.Id))
                .OrderBy(u => u.Email)
                .ToListAsync();

            var assignedUsers = await _permissionService.GetRoleUsersAsync(id);
            var assignedUserIds = assignedUsers.Select(u => u.Id).ToHashSet();

            ViewBag.Role = role;
            ViewBag.EligibleUsers = eligibleUsers;
            ViewBag.AssignedUserIds = assignedUserIds;

            return View(role);
        }

        // POST: Admin/AdminRoles/AssignUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUser(int roleId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            await _permissionService.AssignRoleToUserAsync(userId, roleId, currentUserId ?? "");
            TempData["Success"] = _localizer["UserAssignedToRoleSuccessfully"].Value;
            return RedirectToAction(nameof(ManageAssignments), new { id = roleId });
        }

        // POST: Admin/AdminRoles/RemoveUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUser(int roleId, string userId)
        {
            await _permissionService.RemoveRoleFromUserAsync(userId, roleId);
            TempData["Success"] = _localizer["UserRemovedFromRoleSuccessfully"].Value;
            return RedirectToAction(nameof(ManageAssignments), new { id = roleId });
        }

        // GET: Admin/AdminRoles/UserPermissions
        public async Task<IActionResult> UserPermissions(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _permissionService.GetUserRolesAsync(id);
            var allPermissions = await _permissionService.GetAllPermissionsAsync(id);

            ViewBag.User = user;
            ViewBag.UserRoles = userRoles;
            ViewBag.Modules = AdminModules.GetAllModules();

            return View(allPermissions);
        }
    }
}
