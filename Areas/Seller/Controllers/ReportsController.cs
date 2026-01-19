using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using X.PagedList.Extensions;

namespace Bangaliyana.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "SuperAdmin,Seller,Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISellerMonthlyReportService _reportService;
        private readonly ILogger<ReportsController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ReportsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ISellerMonthlyReportService reportService,
            ILogger<ReportsController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _userManager = userManager;
            _reportService = reportService;
            _logger = logger;
            _localizer = localizer;
        }

        // Helper to get current seller
        private async Task<Models.Seller?> GetCurrentSellerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _db.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);
        }

        // GET: My Monthly Reports
        public async Task<IActionResult> Index(int? page, int? year, MonthlyReportStatus? status)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var query = _db.SellerMonthlyReports
                .Where(r => r.SellerId == seller.Id)
                .AsQueryable();

            // Apply filters
            if (year.HasValue)
            {
                query = query.Where(r => r.Year == year.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            query = query.OrderByDescending(r => r.Year)
                         .ThenByDescending(r => r.Month);

            var reports = await query.ToListAsync();

            // Calculate summary statistics
            var pendingPayoutAmount = reports
                .Where(r => r.Status == MonthlyReportStatus.PendingPayout || r.Status == MonthlyReportStatus.Finalized)
                .Sum(r => r.NetPayable);

            var totalPaidAmount = reports
                .Where(r => r.Status == MonthlyReportStatus.Paid)
                .Sum(r => r.PayoutAmount);

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var currentMonthReport = reports.FirstOrDefault(r => r.Year == currentYear && r.Month == currentMonth);

            // Get available years for filter
            var availableYears = reports.Select(r => r.Year).Distinct().OrderByDescending(y => y).ToList();

            ViewBag.Seller = seller;
            ViewBag.PendingPayoutAmount = pendingPayoutAmount;
            ViewBag.TotalPaidAmount = totalPaidAmount;
            ViewBag.CurrentMonthReport = currentMonthReport;
            ViewBag.CurrentYear = year ?? currentYear;
            ViewBag.CurrentStatus = status;
            ViewBag.AvailableYears = availableYears;
            ViewBag.ReportStatuses = Enum.GetValues<MonthlyReportStatus>();
            ViewBag.AreaName = "Seller";
            ViewBag.ControllerName = "Reports";

            return View(reports.ToPagedList(page ?? 1, 12));
        }

        // GET: Report Details with order items
        public async Task<IActionResult> Details(int id, int? itemPage)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var report = await _db.SellerMonthlyReports
                .FirstOrDefaultAsync(r => r.Id == id && r.SellerId == seller.Id);

            if (report == null)
            {
                TempData["error"] = _localizer["ReportNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Get report order items with pagination
            var reportItems = await _reportService.GetReportItemsAsync(id, itemPage ?? 1, 50);
            var itemsCount = await _reportService.GetReportItemsCountAsync(id);

            // Get linked payout if exists
            ManualSellerPayout? linkedPayout = null;
            if (report.ManualSellerPayoutId.HasValue)
            {
                linkedPayout = await _db.ManualSellerPayouts
                    .FirstOrDefaultAsync(p => p.Id == report.ManualSellerPayoutId.Value);
            }

            ViewBag.Seller = seller;
            ViewBag.ReportItems = reportItems;
            ViewBag.ItemsCount = itemsCount;
            ViewBag.ItemPage = itemPage ?? 1;
            ViewBag.LinkedPayout = linkedPayout;
            ViewBag.AreaName = "Seller";
            ViewBag.ControllerName = "Reports";

            return View(report);
        }

        // GET: Current month summary
        public async Task<IActionResult> CurrentMonth()
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            // Get or create current month's report
            var report = await _reportService.GetOrCreateCurrentReportAsync(seller.Id);

            // Get order items for this report
            var reportItems = await _reportService.GetReportItemsAsync(report.Id, 1, 100);

            ViewBag.Seller = seller;
            ViewBag.ReportItems = reportItems;
            ViewBag.AreaName = "Seller";
            ViewBag.ControllerName = "Reports";

            return View(report);
        }

        // GET: Print a report
        public async Task<IActionResult> PrintReport(int id)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var report = await _db.SellerMonthlyReports
                .FirstOrDefaultAsync(r => r.Id == id && r.SellerId == seller.Id);

            if (report == null)
            {
                TempData["error"] = _localizer["ReportNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            var reportItems = await _reportService.GetReportItemsAsync(id, 1, 500);

            ViewBag.Seller = seller;
            ViewBag.ReportItems = reportItems;
            ViewBag.PrintDate = DateTime.Now;

            return View(report);
        }

        // GET: Payment History (from reports)
        public async Task<IActionResult> PaymentHistory(int? page)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            // Get all paid reports
            var paidReports = await _db.SellerMonthlyReports
                .Where(r => r.SellerId == seller.Id && r.Status == MonthlyReportStatus.Paid)
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .ToListAsync();

            // Also get manual payouts for legacy data
            var legacyPayouts = await _db.ManualSellerPayouts
                .Where(p => p.SellerId == seller.Id && p.Status == ManualPayoutStatus.Completed)
                .OrderByDescending(p => p.CompletedAt)
                .ToListAsync();

            var totalPaid = paidReports.Sum(r => r.PayoutAmount);
            if (!paidReports.Any() && legacyPayouts.Any())
            {
                totalPaid = legacyPayouts.Sum(p => p.Amount);
            }

            ViewBag.Seller = seller;
            ViewBag.LegacyPayouts = legacyPayouts;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.AreaName = "Seller";
            ViewBag.ControllerName = "Reports";

            return View(paidReports.ToPagedList(page ?? 1, 12));
        }

        // GET: Pending Balance Details
        public async Task<IActionResult> PendingBalance()
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            // Get all pending payout reports
            var pendingReports = await _db.SellerMonthlyReports
                .Where(r => r.SellerId == seller.Id &&
                           (r.Status == MonthlyReportStatus.PendingPayout || r.Status == MonthlyReportStatus.Finalized))
                .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
                .ToListAsync();

            // Get current open report
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var openReport = await _db.SellerMonthlyReports
                .FirstOrDefaultAsync(r => r.SellerId == seller.Id &&
                                         r.Year == currentYear &&
                                         r.Month == currentMonth &&
                                         r.Status == MonthlyReportStatus.Open);

            var totalPendingBalance = await _reportService.GetSellerPendingBalanceAsync(seller.Id);
            var minimumPayout = 500m;
            var eligibleForPayout = totalPendingBalance >= minimumPayout;

            ViewBag.Seller = seller;
            ViewBag.OpenReport = openReport;
            ViewBag.TotalPendingBalance = totalPendingBalance;
            ViewBag.MinimumPayout = minimumPayout;
            ViewBag.EligibleForPayout = eligibleForPayout;
            ViewBag.AreaName = "Seller";
            ViewBag.ControllerName = "Reports";

            return View(pendingReports);
        }
    }
}
