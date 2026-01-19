using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class MenuManagementController : Controller
    {
        private readonly IMenuService _menuService;
        private readonly ILogger<MenuManagementController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public MenuManagementController(
            IMenuService menuService,
            ILogger<MenuManagementController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _menuService = menuService;
            _logger = logger;
            _localizer = localizer;
        }

        // GET: Admin/MenuManagement
        public async Task<IActionResult> Index()
        {
            var menuItems = await _menuService.GetAllMenuItemsAsync();
            return View(menuItems);
        }

        // GET: Admin/MenuManagement/Create
        public async Task<IActionResult> Create()
        {
            await PopulateViewData();
            return View(new MenuItem { IsActive = true, DisplayOrder = 1 });
        }

        // POST: Admin/MenuManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItem menuItem, List<string>? selectedRoles)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var created = await _menuService.CreateMenuItemAsync(menuItem);

                    if (selectedRoles?.Any() == true)
                    {
                        await _menuService.UpdateMenuPermissionsAsync(created.Id, selectedRoles);
                    }

                    TempData["Success"] = _localizer["MenuItemCreatedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating menu item");
                    ModelState.AddModelError("", "An error occurred while creating the menu item.");
                }
            }

            await PopulateViewData();
            return View(menuItem);
        }

        // GET: Admin/MenuManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var menuItem = await _menuService.GetMenuItemByIdAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            await PopulateViewData(menuItem.Id);
            ViewBag.SelectedRoles = menuItem.Permissions?.Select(p => p.RoleName).ToList() ?? new List<string>();
            return View(menuItem);
        }

        // POST: Admin/MenuManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuItem menuItem, List<string>? selectedRoles)
        {
            if (id != menuItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _menuService.UpdateMenuItemAsync(menuItem);

                    if (selectedRoles != null)
                    {
                        await _menuService.UpdateMenuPermissionsAsync(id, selectedRoles);
                    }

                    TempData["Success"] = _localizer["MenuItemUpdatedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating menu item {Id}", id);
                    ModelState.AddModelError("", "An error occurred while updating the menu item.");
                }
            }

            await PopulateViewData(menuItem.Id);
            ViewBag.SelectedRoles = selectedRoles ?? new List<string>();
            return View(menuItem);
        }

        // POST: Admin/MenuManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _menuService.DeleteMenuItemAsync(id);
                TempData["Success"] = _localizer["MenuItemDeletedSuccessfully"].Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting menu item {Id}", id);
                TempData["Error"] = _localizer["ErrorDeletingMenuItem"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/MenuManagement/Permissions/5
        public async Task<IActionResult> Permissions(int id)
        {
            var menuItem = await _menuService.GetMenuItemByIdAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            var roles = await _menuService.GetAvailableRolesAsync();
            var currentPermissions = await _menuService.GetMenuPermissionsAsync(id);

            ViewBag.MenuItem = menuItem;
            ViewBag.AllRoles = roles;
            ViewBag.CurrentPermissions = currentPermissions.Select(p => p.RoleName).ToList();

            return View();
        }

        // POST: Admin/MenuManagement/Permissions/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Permissions(int id, List<string> selectedRoles)
        {
            try
            {
                await _menuService.UpdateMenuPermissionsAsync(id, selectedRoles ?? new List<string>());
                TempData["Success"] = _localizer["MenuPermissionsUpdatedSuccessfully"].Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permissions for menu item {Id}", id);
                TempData["Error"] = _localizer["ErrorUpdatingPermissions"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/MenuManagement/Reorder
        [HttpPost]
        public async Task<IActionResult> Reorder([FromBody] ReorderRequest request)
        {
            try
            {
                await _menuService.ReorderMenuItemsAsync(request.Location, request.MenuItemIds);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering menu items");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/MenuManagement/ToggleActive/5
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var menuItem = await _menuService.GetMenuItemByIdAsync(id);
                if (menuItem == null)
                {
                    return NotFound();
                }

                menuItem.IsActive = !menuItem.IsActive;
                await _menuService.UpdateMenuItemAsync(menuItem);

                return Ok(new { success = true, isActive = menuItem.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling menu item {Id}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateViewData(int? excludeId = null)
        {
            var allMenuItems = await _menuService.GetAllMenuItemsAsync();

            // Exclude current item from parent options to prevent circular reference
            var parentOptions = allMenuItems
                .Where(m => m.ParentId == null && m.Id != excludeId)
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = $"{m.Name} ({m.Location})" })
                .ToList();

            parentOptions.Insert(0, new SelectListItem { Value = "", Text = "-- No Parent --" });

            ViewBag.ParentOptions = parentOptions;
            ViewBag.Locations = Enum.GetValues<MenuLocation>()
                .Select(l => new SelectListItem { Value = ((int)l).ToString(), Text = l.ToString() })
                .ToList();
            ViewBag.Roles = await _menuService.GetAvailableRolesAsync();
        }

        public class ReorderRequest
        {
            public int Location { get; set; }
            public List<int> MenuItemIds { get; set; } = new();
        }
    }
}
