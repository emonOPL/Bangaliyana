using System.ComponentModel.DataAnnotations;
using Bangaliyana.Data;
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
    [Authorize(Policy = "RequireAdminRole")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IMenuPermissionService _menuPermissionService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IMenuPermissionService menuPermissionService,
            IStringLocalizer<SharedResources> localizer)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _menuPermissionService = menuPermissionService;
            _localizer = localizer;
        }

        // GET: Admin/UserManagement
        public async Task<IActionResult> Index(string? searchTerm, string? roleFilter, int page = 1)
        {
            const int pageSize = 20;

            // Build base query with role filter applied at database level (optimized to avoid N+1)
            IQueryable<ApplicationUser> query;

            if (!string.IsNullOrEmpty(roleFilter))
            {
                // Filter by role in database using join - avoids N+1 queries
                var roleId = await _context.Roles
                    .Where(r => r.Name == roleFilter)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (roleId != null)
                {
                    query = from user in _context.Users
                            join userRole in _context.UserRoles on user.Id equals userRole.UserId
                            where userRole.RoleId == roleId
                            select user;
                }
                else
                {
                    query = _userManager.Users.AsQueryable();
                }
            }
            else
            {
                query = _userManager.Users.AsQueryable();
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u =>
                    u.Email!.ToLower().Contains(searchTerm) ||
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)));
            }

            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Batch load roles for all users in a single query (avoids N+1)
            var userIds = users.Select(u => u.Id).ToList();
            var userRolesDict = await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
                .GroupBy(x => x.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.RoleName ?? "").ToList());

            var userViewModels = users.Select(user => new UserManagementViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                IsVerified = user.IsVerified,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                LockoutEnd = user.LockoutEnd,
                Roles = userRolesDict.TryGetValue(user.Id, out var roles) ? roles : new List<string>(),
                CreatedAt = user.CreatedAt,
                Role = user.Role
            }).ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(userViewModels);
        }

        // GET: Admin/UserManagement/Details/id
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.Users
                .Include(u => u.Division)
                .Include(u => u.District)
                .Include(u => u.Upazila)
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            ViewBag.User = user;
            ViewBag.Roles = roles.ToList();
            ViewBag.AllRoles = allRoles;
            ViewBag.OrderCount = user.Orders?.Count ?? 0;

            return View(user);
        }

        // POST: Admin/UserManagement/PromoteToSeller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToSeller(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Check if already a Seller or Admin
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("Admin"))
            {
                TempData["error"] = _localizer["CannotChangeAdminRoleToSeller"].Value;
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            // Remove User role if present, add Seller role
            if (currentRoles.Contains("User"))
            {
                await _userManager.RemoveFromRoleAsync(user, "User");
                // Remove User role permissions
                await _menuPermissionService.RemoveRolePermissionsAsync(userId, "User");
            }

            if (!currentRoles.Contains("Seller"))
            {
                var result = await _userManager.AddToRoleAsync(user, "Seller");
                if (result.Succeeded)
                {
                    user.Role = UserRole.Seller;
                    await _userManager.UpdateAsync(user);

                    // Auto-assign Seller general/required permissions
                    await _menuPermissionService.AssignRolePermissionsAsync(userId, "Seller");

                    TempData["success"] = _localizer["UserPromotedToSellerSuccessfully"].Value;
                }
                else
                {
                    TempData["error"] = _localizer["FailedToPromoteUserToSeller"].Value;
                }
            }
            else
            {
                TempData["info"] = _localizer["UserIsAlreadySeller"].Value;
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: Admin/UserManagement/PromoteToModerator
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToModerator(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Cannot change Admin to Moderator
            if (currentRoles.Contains("Admin"))
            {
                TempData["error"] = _localizer["CannotChangeAdminRoleToModerator"].Value;
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            // Remove User and Seller roles if present
            if (currentRoles.Contains("User"))
            {
                await _userManager.RemoveFromRoleAsync(user, "User");
                await _menuPermissionService.RemoveRolePermissionsAsync(userId, "User");
            }
            if (currentRoles.Contains("Seller"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Seller");
                await _menuPermissionService.RemoveRolePermissionsAsync(userId, "Seller");
            }

            if (!currentRoles.Contains("Moderator"))
            {
                var result = await _userManager.AddToRoleAsync(user, "Moderator");
                if (result.Succeeded)
                {
                    user.Role = UserRole.Moderator;
                    await _userManager.UpdateAsync(user);

                    // Auto-assign Moderator general/required permissions
                    await _menuPermissionService.AssignRolePermissionsAsync(userId, "Moderator");

                    TempData["success"] = _localizer["UserPromotedToModeratorSuccessfully"].Value;
                }
                else
                {
                    TempData["error"] = _localizer["FailedToPromoteUserToModerator"].Value;
                }
            }
            else
            {
                TempData["info"] = _localizer["UserIsAlreadyModerator"].Value;
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: Admin/UserManagement/DemoteFromModerator
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteFromModerator(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Prevent self-demotion
            var currentUserId = _userManager.GetUserId(User);
            if (userId == currentUserId)
            {
                TempData["error"] = _localizer["CannotDemoteYourself"].Value;
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains("Moderator"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Moderator");
                await _menuPermissionService.RemoveRolePermissionsAsync(userId, "Moderator");
            }

            if (!currentRoles.Contains("User"))
            {
                var result = await _userManager.AddToRoleAsync(user, "User");
                if (result.Succeeded)
                {
                    user.Role = UserRole.User;
                    await _userManager.UpdateAsync(user);

                    // Auto-assign User general/required permissions
                    await _menuPermissionService.AssignRolePermissionsAsync(userId, "User");

                    TempData["success"] = _localizer["UserDemotedFromModeratorSuccessfully"].Value;
                }
                else
                {
                    TempData["error"] = _localizer["FailedToDemoteUser"].Value;
                }
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: Admin/UserManagement/PromoteToAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove other roles and their permissions
            if (currentRoles.Contains("User"))
            {
                await _userManager.RemoveFromRoleAsync(user, "User");
                await _menuPermissionService.RemoveRolePermissionsAsync(userId, "User");
            }
            if (currentRoles.Contains("Seller"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Seller");
                await _menuPermissionService.RemoveRolePermissionsAsync(userId, "Seller");
            }
            if (currentRoles.Contains("Moderator"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Moderator");
                await _menuPermissionService.RemoveRolePermissionsAsync(userId, "Moderator");
            }

            if (!currentRoles.Contains("Admin"))
            {
                var result = await _userManager.AddToRoleAsync(user, "Admin");
                if (result.Succeeded)
                {
                    user.Role = UserRole.Admin;
                    await _userManager.UpdateAsync(user);

                    // Auto-assign Admin general/required permissions
                    await _menuPermissionService.AssignRolePermissionsAsync(userId, "Admin");

                    TempData["success"] = _localizer["UserPromotedToAdminSuccessfully"].Value;
                }
                else
                {
                    TempData["error"] = _localizer["FailedToPromoteUserToAdmin"].Value;
                }
            }
            else
            {
                TempData["info"] = _localizer["UserIsAlreadyAdmin"].Value;
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: Admin/UserManagement/DemoteToSeller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteToSeller(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Prevent self-demotion
            var currentUserId = _userManager.GetUserId(User);
            if (userId == currentUserId)
            {
                TempData["error"] = _localizer["CannotDemoteYourself"].Value;
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains("Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                // Remove Admin role permissions
                await _menuPermissionService.RemoveRolePermissionsAsync(userId, "Admin");
            }

            if (!currentRoles.Contains("Seller"))
            {
                var result = await _userManager.AddToRoleAsync(user, "Seller");
                if (result.Succeeded)
                {
                    user.Role = UserRole.Seller;
                    await _userManager.UpdateAsync(user);

                    // Auto-assign Seller general/required permissions
                    await _menuPermissionService.AssignRolePermissionsAsync(userId, "Seller");

                    TempData["success"] = _localizer["UserDemotedToSellerSuccessfully"].Value;
                }
                else
                {
                    TempData["error"] = _localizer["FailedToDemoteUserToSeller"].Value;
                }
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: Admin/UserManagement/DemoteToUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteToUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Prevent self-demotion
            var currentUserId = _userManager.GetUserId(User);
            if (userId == currentUserId)
            {
                TempData["error"] = _localizer["CannotDemoteYourself"].Value;
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains("Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                // Remove Admin role permissions
                await _menuPermissionService.RemoveRolePermissionsAsync(userId, "Admin");
            }
            if (currentRoles.Contains("Seller"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Seller");
                // Remove Seller role permissions
                await _menuPermissionService.RemoveRolePermissionsAsync(userId, "Seller");
            }

            if (!currentRoles.Contains("User"))
            {
                var result = await _userManager.AddToRoleAsync(user, "User");
                if (result.Succeeded)
                {
                    user.Role = UserRole.User;
                    await _userManager.UpdateAsync(user);

                    // Auto-assign User general/required permissions
                    await _menuPermissionService.AssignRolePermissionsAsync(userId, "User");

                    TempData["success"] = _localizer["UserDemotedToUserSuccessfully"].Value;
                }
                else
                {
                    TempData["error"] = _localizer["FailedToDemoteUser"].Value;
                }
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // GET: Admin/UserManagement/Permissions/id
        public async Task<IActionResult> Permissions(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Get current admin's email to check if SuperAdmin
            var currentUser = await _userManager.GetUserAsync(User);
            var isSuperAdmin = currentUser != null && _menuPermissionService.IsSuperAdmin(currentUser.Email ?? "");

            var menuItems = await _context.MenuItems
                .Where(m => m.ParentId == null && m.IsActive)
                .Include(m => m.Children)
                .OrderBy(m => m.Location)
                .ThenBy(m => m.DisplayOrder)
                .ToListAsync();

            var userPermissions = await _context.UserMenuPermissions
                .Where(p => p.UserId == id)
                .ToListAsync();

            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.User = user;
            ViewBag.UserRoles = roles.ToList();
            ViewBag.UserPermissions = userPermissions;
            ViewBag.MenuItems = menuItems;
            ViewBag.IsSuperAdmin = isSuperAdmin;
            ViewBag.SuperAdminEmails = _menuPermissionService.SuperAdminEmails;

            return View();
        }

        // POST: Admin/UserManagement/Permissions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Permissions(string userId, List<int> grantedMenus, List<int> revokedMenus)
        {
            if (string.IsNullOrEmpty(userId))
                return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Get current admin's email to check if SuperAdmin
            var currentUser = await _userManager.GetUserAsync(User);
            var isSuperAdmin = currentUser != null && _menuPermissionService.IsSuperAdmin(currentUser.Email ?? "");

            // Get special/sensitive menu IDs - only SuperAdmin can modify these
            var specialSensitiveMenuIds = await _context.MenuItems
                .Where(m => m.PermissionType == PermissionType.Special || m.PermissionType == PermissionType.Sensitive)
                .Select(m => m.Id)
                .ToListAsync();

            // If not SuperAdmin, filter out special/sensitive menus from the lists
            if (!isSuperAdmin)
            {
                grantedMenus = grantedMenus?.Where(id => !specialSensitiveMenuIds.Contains(id)).ToList() ?? new List<int>();
                revokedMenus = revokedMenus?.Where(id => !specialSensitiveMenuIds.Contains(id)).ToList() ?? new List<int>();
            }

            // Remove existing permissions for menus that the current admin can modify
            var existingPermissions = await _context.UserMenuPermissions
                .Where(p => p.UserId == userId)
                .ToListAsync();

            if (isSuperAdmin)
            {
                // SuperAdmin can modify all permissions
                _context.UserMenuPermissions.RemoveRange(existingPermissions);
            }
            else
            {
                // Non-SuperAdmin can only modify non-special/sensitive permissions
                var permissionsToRemove = existingPermissions
                    .Where(p => !specialSensitiveMenuIds.Contains(p.MenuItemId))
                    .ToList();
                _context.UserMenuPermissions.RemoveRange(permissionsToRemove);
            }

            // Add granted permissions (CanAccess = true)
            if (grantedMenus != null)
            {
                foreach (var menuId in grantedMenus)
                {
                    _context.UserMenuPermissions.Add(new UserMenuPermission
                    {
                        UserId = userId,
                        MenuItemId = menuId,
                        CanAccess = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Add revoked permissions (CanAccess = false)
            if (revokedMenus != null)
            {
                foreach (var menuId in revokedMenus)
                {
                    _context.UserMenuPermissions.Add(new UserMenuPermission
                    {
                        UserId = userId,
                        MenuItemId = menuId,
                        CanAccess = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["success"] = _localizer["UserPermissionsUpdatedSuccessfully"].Value;

            return RedirectToAction(nameof(Permissions), new { id = userId });
        }

        // POST: Admin/UserManagement/ToggleLockout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockout(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Prevent self-lockout
            var currentUserId = _userManager.GetUserId(User);
            if (userId == currentUserId)
            {
                TempData["error"] = _localizer["CannotLockYourselfOut"].Value;
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                // Unlock the user
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["success"] = _localizer["UserUnlockedSuccessfully"].Value;
            }
            else
            {
                // Lock the user for 100 years (effectively permanent)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["success"] = _localizer["UserLockedSuccessfully"].Value;
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: Admin/UserManagement/VerifyEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (!user.EmailConfirmed)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    TempData["success"] = _localizer["EmailVerifiedSuccessfully"].Value;
                }
                else
                {
                    TempData["error"] = _localizer["FailedToVerifyEmail"].Value;
                }
            }
            else
            {
                TempData["info"] = _localizer["EmailAlreadyVerified"].Value;
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // GET: Admin/UserManagement/Edit/id
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.Users
                .Include(u => u.Division)
                .Include(u => u.District)
                .Include(u => u.Upazila)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var editModel = new UserEditViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                PostalCode = user.PostalCode,
                DivisionId = user.DivisionId,
                DistrictId = user.DistrictId,
                UpazilaId = user.UpazilaId,
                IsVerified = user.IsVerified,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed
            };

            // Load dropdown data
            ViewBag.Divisions = await _context.Divisions.OrderBy(d => d.Name).ToListAsync();
            ViewBag.Districts = user.DivisionId.HasValue
                ? await _context.Districts.Where(d => d.DivisionId == user.DivisionId).OrderBy(d => d.Name).ToListAsync()
                : new List<District>();
            ViewBag.Upazilas = user.DistrictId.HasValue
                ? await _context.Upazilas.Where(u => u.DistrictId == user.DistrictId).OrderBy(u => u.Name).ToListAsync()
                : new List<Upazila>();

            return View(editModel);
        }

        // POST: Admin/UserManagement/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            // Clear model state errors for nullable fields that received empty strings
            var nullableFields = new[] { "DivisionId", "DistrictId", "UpazilaId", "DateOfBirth" };
            foreach (var field in nullableFields)
            {
                if (ModelState.ContainsKey(field) && ModelState[field]?.Errors.Count > 0)
                {
                    var rawValue = Request.Form[field].ToString();
                    if (string.IsNullOrEmpty(rawValue))
                    {
                        ModelState.Remove(field);
                    }
                }
            }

            // Clear model state errors for bool checkbox fields that may have binding issues
            var boolFields = new[] { "IsVerified", "EmailConfirmed" };
            foreach (var field in boolFields)
            {
                if (ModelState.ContainsKey(field) && ModelState[field]?.Errors.Count > 0)
                {
                    ModelState.Remove(field);
                    // Set default value to false if not present in form
                    var formValue = Request.Form[field].ToString().ToLower();
                    if (field == "IsVerified")
                        model.IsVerified = formValue == "true" || formValue == "on";
                    else if (field == "EmailConfirmed")
                        model.EmailConfirmed = formValue == "true" || formValue == "on";
                }
            }

            if (!ModelState.IsValid)
            {
                // Reload dropdown data
                ViewBag.Divisions = await _context.Divisions.OrderBy(d => d.Name).ToListAsync();
                ViewBag.Districts = model.DivisionId.HasValue
                    ? await _context.Districts.Where(d => d.DivisionId == model.DivisionId).OrderBy(d => d.Name).ToListAsync()
                    : new List<District>();
                ViewBag.Upazilas = model.DistrictId.HasValue
                    ? await _context.Upazilas.Where(u => u.DistrictId == model.DistrictId).OrderBy(u => u.Name).ToListAsync()
                    : new List<Upazila>();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Update user properties
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.DateOfBirth = model.DateOfBirth;
            user.Address = model.Address;
            user.PostalCode = model.PostalCode;
            user.DivisionId = model.DivisionId;
            user.DistrictId = model.DistrictId;
            user.UpazilaId = model.UpazilaId;
            user.IsVerified = model.IsVerified;
            user.UpdatedAt = DateTime.UtcNow;

            // Update email if changed
            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    ViewBag.Divisions = await _context.Divisions.OrderBy(d => d.Name).ToListAsync();
                    return View(model);
                }
                user.UserName = model.Email;
                user.NormalizedUserName = model.Email.ToUpper();
                user.NormalizedEmail = model.Email.ToUpper();
            }

            // Handle email confirmation toggle
            if (model.EmailConfirmed && !user.EmailConfirmed)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, token);
            }
            else if (!model.EmailConfirmed && user.EmailConfirmed)
            {
                // Unconfirm email (direct database update needed)
                user.EmailConfirmed = false;
            }

            // Note: PhoneNumberConfirmed is readonly - user must verify via OTP from their profile

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["success"] = _localizer["UserUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.Divisions = await _context.Divisions.OrderBy(d => d.Name).ToListAsync();
            return View(model);
        }

        // POST: Admin/UserManagement/Block
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Block(string userId, string? reason)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Prevent self-blocking
            var currentUserId = _userManager.GetUserId(User);
            if (userId == currentUserId)
            {
                TempData["error"] = _localizer["CannotBlockYourself"].Value;
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            // Check if trying to block another admin
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("Admin"))
            {
                TempData["error"] = _localizer["CannotBlockAnotherAdmin"].Value;
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            // Lock the user for 100 years (effectively permanent)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            TempData["success"] = _localizer["UserBlockedSuccessfully"].Value;

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: Admin/UserManagement/Unblock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unblock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Unlock the user
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["success"] = _localizer["UserUnblockedSuccessfully"].Value;

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // GET: Admin/UserManagement/GetDistricts
        [HttpGet]
        public async Task<IActionResult> GetDistricts(int divisionId)
        {
            var districts = await _context.Districts
                .Where(d => d.DivisionId == divisionId)
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();
            return Json(districts);
        }

        // GET: Admin/UserManagement/GetUpazilas
        [HttpGet]
        public async Task<IActionResult> GetUpazilas(int districtId)
        {
            var upazilas = await _context.Upazilas
                .Where(u => u.DistrictId == districtId)
                .OrderBy(u => u.Name)
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();
            return Json(upazilas);
        }

        // GET: Admin/UserManagement/CODSettings/id
        public async Task<IActionResult> CODSettings(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            // Get COD order history
            var codOrders = await _context.Orders
                .Where(o => o.UserId == id && o.PaymentMethod == PaymentMethod.CashOnDelivery)
                .OrderByDescending(o => o.OrderDate)
                .Take(20)
                .ToListAsync();

            var viewModel = new CODSettingsViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                IsCODAllowed = user.IsCODAllowed,
                CODRestrictionReason = user.CODRestrictionReason,
                CODRestrictedAt = user.CODRestrictedAt,
                FailedCODOrdersCount = user.FailedCODOrdersCount,
                ReturnedOrdersCount = user.ReturnedOrdersCount,
                Roles = roles.ToList(),
                RecentCODOrders = codOrders
            };

            return View(viewModel);
        }

        // POST: Admin/UserManagement/RestrictCOD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestrictCOD(string userId, string reason)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            user.IsCODAllowed = false;
            user.CODRestrictionReason = reason;
            user.CODRestrictedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
            TempData["success"] = _localizer["CODRestrictedSuccessfully"].Value;

            return RedirectToAction(nameof(CODSettings), new { id = userId });
        }

        // POST: Admin/UserManagement/AllowCOD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllowCOD(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            user.IsCODAllowed = true;
            user.CODRestrictionReason = null;
            user.CODRestrictedAt = null;
            user.FailedCODOrdersCount = 0;
            user.ReturnedOrdersCount = 0;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
            TempData["success"] = _localizer["CODEnabledSuccessfully"].Value;

            return RedirectToAction(nameof(CODSettings), new { id = userId });
        }

        // GET: Admin/UserManagement/SellerPayments/id
        public async Task<IActionResult> SellerPayments(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.Users
                .Include(u => u.Seller)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Seller"))
            {
                TempData["error"] = _localizer["UserIsNotSeller"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            // Get seller's orders
            var sellerOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.SellerId == user.Seller!.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Calculate payment summary
            var totalSales = sellerOrders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
            var pendingPayment = sellerOrders.Where(o => o.Status == OrderStatus.Delivered && !o.IsPaymentReceived).Sum(o => o.TotalAmount);
            var paidAmount = sellerOrders.Where(o => o.Status == OrderStatus.Delivered && o.IsPaymentReceived).Sum(o => o.TotalAmount);

            var viewModel = new SellerPaymentsViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                SellerId = user.Seller?.Id ?? 0,
                SellerName = user.Seller?.ShopName ?? user.FullName,
                TotalSales = totalSales,
                PendingPayment = pendingPayment,
                PaidAmount = paidAmount,
                Orders = sellerOrders,
                Roles = roles.ToList()
            };

            return View(viewModel);
        }

        // POST: Admin/UserManagement/ProcessSellerPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSellerPayment(string userId, int orderId, string transactionId, string notes)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction(nameof(SellerPayments), new { id = userId });
            }

            order.IsPaymentReceived = true;
            order.TransactionId = transactionId;
            order.AdminNotes = $"{order.AdminNotes}\n[Payment processed: {DateTime.UtcNow:yyyy-MM-dd HH:mm}] {notes}".Trim();
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["success"] = _localizer["PaymentProcessedSuccessfully"].Value;

            return RedirectToAction(nameof(SellerPayments), new { id = userId });
        }

        // GET: Admin/UserManagement/CustomerRefunds/id
        public async Task<IActionResult> CustomerRefunds(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            // Get customer's orders eligible for refund
            var customerOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Calculate refund summary
            var totalPaid = customerOrders.Where(o => o.PaymentStatus == PaymentStatus.Completed).Sum(o => o.TotalAmount);
            var pendingRefunds = customerOrders.Where(o => o.Status == OrderStatus.Returned || o.Status == OrderStatus.Cancelled)
                .Where(o => o.PaymentStatus == PaymentStatus.Completed || o.PaymentStatus == PaymentStatus.PartiallyRefunded)
                .Sum(o => o.TotalAmount);
            var refundedAmount = customerOrders.Where(o => o.PaymentStatus == PaymentStatus.Refunded).Sum(o => o.TotalAmount);

            var viewModel = new CustomerRefundsViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                TotalPaid = totalPaid,
                PendingRefunds = pendingRefunds,
                RefundedAmount = refundedAmount,
                Orders = customerOrders,
                Roles = roles.ToList()
            };

            return View(viewModel);
        }

        // POST: Admin/UserManagement/ProcessRefund
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(string userId, int orderId, decimal refundAmount, string transactionId, string notes)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction(nameof(CustomerRefunds), new { id = userId });
            }

            order.PaymentStatus = PaymentStatus.Refunded;
            order.Status = OrderStatus.Refunded;
            order.TransactionId = $"{order.TransactionId} | Refund: {transactionId}";
            order.AdminNotes = $"{order.AdminNotes}\n[Refund processed: {DateTime.UtcNow:yyyy-MM-dd HH:mm}] Amount: {refundAmount:C} - {notes}".Trim();
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["success"] = _localizer["RefundProcessedSuccessfully"].Value;

            return RedirectToAction(nameof(CustomerRefunds), new { id = userId });
        }

        // POST: Admin/UserManagement/MarkOrderAsReturned
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkOrderAsReturned(int orderId, string returnReason, bool customerRefused)
        {
            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            order.Status = OrderStatus.Returned;
            order.DeliveryStatus = DeliveryStatus.Returned;
            order.ReturnedAt = DateTime.UtcNow;
            order.ReturnReason = returnReason;
            order.CustomerRefusedDelivery = customerRefused;
            order.UpdatedAt = DateTime.UtcNow;

            // If COD order and customer refused, apply penalty
            if (order.PaymentMethod == PaymentMethod.CashOnDelivery && customerRefused && order.User != null)
            {
                var user = order.User;
                user.FailedCODOrdersCount++;
                user.ReturnedOrdersCount++;

                // Restrict COD after first failed/refused delivery
                if (!order.CODPenaltyApplied)
                {
                    user.IsCODAllowed = false;
                    user.CODRestrictionReason = $"Refused delivery for order #{order.OrderNumber} on {DateTime.UtcNow:yyyy-MM-dd}";
                    user.CODRestrictedAt = DateTime.UtcNow;
                    order.CODPenaltyApplied = true;
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            await _context.SaveChangesAsync();
            TempData["success"] = _localizer["OrderMarkedAsReturnedSuccessfully"].Value;

            if (order.UserId != null)
                return RedirectToAction(nameof(CustomerRefunds), new { id = order.UserId });

            return RedirectToAction("Index", "Orders");
        }
    }

    public class UserManagementViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public UserRole Role { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public bool IsLocked => LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.UtcNow;
    }

    public class UserEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(500)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [MaxLength(10)]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [Display(Name = "Division")]
        public int? DivisionId { get; set; }

        [Display(Name = "District")]
        public int? DistrictId { get; set; }

        [Display(Name = "Upazila")]
        public int? UpazilaId { get; set; }

        [Display(Name = "Is Verified")]
        public bool IsVerified { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        // Phone verification (readonly - user verifies via OTP from their profile)
        [Display(Name = "Phone Verified")]
        public bool PhoneNumberConfirmed { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }

    public class CODSettingsViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsCODAllowed { get; set; }
        public string? CODRestrictionReason { get; set; }
        public DateTime? CODRestrictedAt { get; set; }
        public int FailedCODOrdersCount { get; set; }
        public int ReturnedOrdersCount { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<Order> RecentCODOrders { get; set; } = new();
    }

    public class SellerPaymentsViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal PendingPayment { get; set; }
        public decimal PaidAmount { get; set; }
        public List<Order> Orders { get; set; } = new();
        public List<string> Roles { get; set; } = new();
    }

    public class CustomerRefundsViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal PendingRefunds { get; set; }
        public decimal RefundedAmount { get; set; }
        public List<Order> Orders { get; set; } = new();
        public List<string> Roles { get; set; } = new();
    }
}
