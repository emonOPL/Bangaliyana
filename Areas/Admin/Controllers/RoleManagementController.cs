using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminRole")]
    public class RoleManagementController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public RoleManagementController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IStringLocalizer<SharedResources> localizer)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _localizer = localizer;
        }

        // GET: Admin/RoleManagement
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        // GET: Admin/RoleManagement/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/RoleManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Required] string name)
        {
            if (ModelState.IsValid)
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(name));
                if (result.Succeeded)
                {
                    TempData["Success"] = _localizer["RoleCreatedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(name);
        }

        // GET: Admin/RoleManagement/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Check if it's a system role
            var systemRoles = new[] { "Admin", "SuperAdmin", "User", "Seller", "Moderator" };
            ViewData["IsSystemRole"] = systemRoles.Contains(role.Name);

            return View(role);
        }

        // POST: Admin/RoleManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Required] string name)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Prevent editing system roles
            var systemRoles = new[] { "Admin", "SuperAdmin", "User", "Seller", "Moderator" };
            if (systemRoles.Contains(role.Name))
            {
                TempData["Error"] = _localizer["CannotEditSystemRoles"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                // Check if new name already exists
                var existingRole = await _roleManager.FindByNameAsync(name);
                if (existingRole != null && existingRole.Id != id)
                {
                    ModelState.AddModelError(string.Empty, "A role with this name already exists.");
                    ViewData["IsSystemRole"] = false;
                    return View(role);
                }

                role.Name = name;
                role.NormalizedName = name.ToUpper();
                var result = await _roleManager.UpdateAsync(role);

                if (result.Succeeded)
                {
                    TempData["Success"] = _localizer["RoleUpdatedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["IsSystemRole"] = false;
            return View(role);
        }

        // GET: Admin/RoleManagement/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        // POST: Admin/RoleManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                // Prevent deletion of system roles
                if (role.Name == "Admin" || role.Name == "User" || role.Name == "Manager")
                {
                    TempData["Error"] = _localizer["CannotDeleteSystemRoles"].Value;
                    return RedirectToAction(nameof(Index));
                }

                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    TempData["Success"] = _localizer["RoleDeletedSuccessfully"].Value;
                }
                else
                {
                    TempData["Error"] = _localizer["ErrorDeletingRole"].Value;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/RoleManagement/ManageUsers
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRolesViewModel.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles.ToList()
                });
            }

            ViewData["AllRoles"] = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(userRolesViewModel);
        }

        // POST: Admin/RoleManagement/UpdateUserRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRoles(string userId, List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!result.Succeeded)
            {
                TempData["Error"] = _localizer["FailedToUpdateUserRoles"].Value;
                return RedirectToAction(nameof(ManageUsers));
            }

            if (roles != null && roles.Count > 0)
            {
                result = await _userManager.AddToRolesAsync(user, roles);
                if (!result.Succeeded)
                {
                    TempData["Error"] = _localizer["FailedToAddUserToRoles"].Value;
                    return RedirectToAction(nameof(ManageUsers));
                }
            }

            TempData["Success"] = _localizer["UserRolesUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(ManageUsers));
        }
    }

    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
    }
}