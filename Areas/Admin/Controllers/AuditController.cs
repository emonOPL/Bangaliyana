using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireSuperAdminRole")]
    public class AuditController : Controller
    {
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public AuditController(IAuditService auditService, UserManager<ApplicationUser> userManager, IStringLocalizer<SharedResources> localizer)
        {
            _auditService = auditService;
            _userManager = userManager;
            _localizer = localizer;
        }

        // GET: Admin/Audit
        public async Task<IActionResult> Index(
            string? userId,
            AuditActionType? actionType,
            AuditModule? module,
            string? entityType,
            DateTime? fromDate,
            DateTime? toDate,
            string? search,
            int page = 1)
        {
            const int pageSize = 50;

            var (logs, totalCount) = await _auditService.GetLogsAsync(
                userId: userId,
                actionType: actionType,
                module: module,
                entityType: entityType,
                fromDate: fromDate,
                toDate: toDate,
                searchTerm: search,
                page: page,
                pageSize: pageSize);

            // Get statistics
            var stats = await _auditService.GetStatisticsAsync(fromDate, toDate);

            // Get admin users for filter dropdown
            var adminRoles = new[] { "SuperAdmin", "Admin", "Moderator", "ProductManager", "OrderManager", "SupportTeam", "ContentManager" };
            var adminUsers = new List<ApplicationUser>();
            foreach (var role in adminRoles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                adminUsers.AddRange(usersInRole);
            }
            ViewBag.AdminUsers = adminUsers.DistinctBy(u => u.Id).OrderBy(u => u.Email).ToList();

            // ViewBag for filters
            ViewBag.UserId = userId;
            ViewBag.ActionType = actionType;
            ViewBag.Module = module;
            ViewBag.EntityType = entityType;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.Statistics = stats;

            return View(logs);
        }

        // GET: Admin/Audit/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var log = await _auditService.GetLogByIdAsync(id);
            if (log == null)
            {
                return NotFound();
            }
            return View(log);
        }

        // GET: Admin/Audit/UserActivity/{userId}
        public async Task<IActionResult> UserActivity(string id)
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

            var logs = await _auditService.GetUserActivityAsync(id, 100);
            ViewBag.User = user;
            return View(logs);
        }

        // GET: Admin/Audit/EntityHistory
        public async Task<IActionResult> EntityHistory(string entityType, string entityId)
        {
            if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId))
            {
                return NotFound();
            }

            var logs = await _auditService.GetEntityHistoryAsync(entityType, entityId);
            ViewBag.EntityType = entityType;
            ViewBag.EntityId = entityId;
            return View(logs);
        }

        // GET: Admin/Audit/Export
        public async Task<IActionResult> Export(
            string? userId,
            AuditActionType? actionType,
            AuditModule? module,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var csvData = await _auditService.ExportToCsvAsync(
                userId: userId,
                actionType: actionType,
                module: module,
                fromDate: fromDate,
                toDate: toDate);

            var fileName = $"AuditLogs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(csvData, "text/csv", fileName);
        }

        // POST: Admin/Audit/Cleanup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cleanup(int daysToKeep = 90)
        {
            var deletedCount = await _auditService.DeleteOldLogsAsync(daysToKeep);
            TempData["success"] = _localizer["AuditLogsCleanedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }
    }
}
