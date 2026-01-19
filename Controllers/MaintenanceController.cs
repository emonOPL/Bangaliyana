using Bangaliyana.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bangaliyana.Controllers
{
    public class MaintenanceController : Controller
    {
        private readonly ISiteSettingsService _siteSettingsService;

        public MaintenanceController(ISiteSettingsService siteSettingsService)
        {
            _siteSettingsService = siteSettingsService;
        }

        public async Task<IActionResult> Index()
        {
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();

            // If maintenance mode is off, redirect to home
            if (siteSettings?.IsMaintenanceMode != true)
            {
                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }

            ViewBag.SiteName = siteSettings.SiteName;
            ViewBag.MaintenanceMessage = siteSettings.MaintenanceMessage ?? "We are currently under maintenance. Please check back soon.";
            ViewBag.ContactEmail = siteSettings.ContactEmail;
            ViewBag.LogoUrl = siteSettings.LogoUrl;

            return View();
        }
    }
}
