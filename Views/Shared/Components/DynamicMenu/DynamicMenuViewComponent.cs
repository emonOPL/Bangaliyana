using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bangaliyana.Views.Shared.Components.DynamicMenu
{
    public class DynamicMenuViewComponent : ViewComponent
    {
        private readonly IMenuService _menuService;

        public DynamicMenuViewComponent(IMenuService menuService)
        {
            _menuService = menuService;
        }

        public async Task<IViewComponentResult> InvokeAsync(MenuLocation location)
        {
            var menuItems = await _menuService.GetMenuHierarchyAsync(UserClaimsPrincipal, location);

            ViewBag.MenuLocation = location;

            return View(menuItems);
        }
    }
}
