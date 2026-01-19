using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using X.PagedList.Extensions;

namespace Bangaliyana.Areas.Moderator.Controllers
{
    [Area("Moderator")]
    [Authorize(Roles = "SuperAdmin,Admin,Moderator")]
    public class MonthlyReportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISellerMonthlyReportService _reportService;
        private readonly ILogger<MonthlyReportController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public MonthlyReportController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ISellerMonthlyReportService reportService,
            ILogger<MonthlyReportController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _userManager = userManager;
            _reportService = reportService;
            _logger = logger;
            _localizer = localizer;
        }

        // GET: Monthly Reports Index - All reports with filtering
        public async Task<IActionResult> Index(int? page, int? year, int? month, MonthlyReportStatus? status, int? sellerId)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;

            var query = _db.SellerMonthlyReports
                .Include(r => r.Seller)
                    .ThenInclude(s => s!.User)
                .AsQueryable();

            // Apply filters
            if (year.HasValue && month.HasValue)
            {
                query = query.Where(r => r.Year == targetYear && r.Month == targetMonth);
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (sellerId.HasValue)
            {
                query = query.Where(r => r.SellerId == sellerId.Value);
            }

            query = query.OrderByDescending(r => r.Year)
                         .ThenByDescending(r => r.Month)
                         .ThenByDescending(r => r.NetPayable);

            var reports = await query.ToListAsync();

            // Get summary statistics
            var summary = await _reportService.GetMonthlySummaryAsync(targetYear, targetMonth);

            // Get list of sellers for filter dropdown
            var sellers = await _db.Sellers
                .Where(s => s.Status == SellerStatus.Approved)
                .OrderBy(s => s.ShopName)
                .Select(s => new { s.Id, s.ShopName })
                .ToListAsync();

            ViewBag.Summary = summary;
            ViewBag.Sellers = sellers;
            ViewBag.CurrentYear = targetYear;
            ViewBag.CurrentMonth = targetMonth;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSellerId = sellerId;
            ViewBag.MonthName = new DateTime(targetYear, targetMonth, 1).ToString("MMMM yyyy");
            ViewBag.ReportStatuses = Enum.GetValues<MonthlyReportStatus>();
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "MonthlyReport";

            return View(reports.ToPagedList(page ?? 1, 20));
        }

        // GET: Report Details with order items
        public async Task<IActionResult> Details(int id, int? itemPage)
        {
            var report = await _db.SellerMonthlyReports
                .Include(r => r.Seller)
                    .ThenInclude(s => s!.User)
                .Include(r => r.Seller)
                    .ThenInclude(s => s!.BankAccounts)
                .FirstOrDefaultAsync(r => r.Id == id);

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

            ViewBag.ReportItems = reportItems;
            ViewBag.ItemsCount = itemsCount;
            ViewBag.ItemPage = itemPage ?? 1;
            ViewBag.LinkedPayout = linkedPayout;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "MonthlyReport";

            return View(report);
        }

        // GET: Seller's report history
        public async Task<IActionResult> SellerHistory(int sellerId, int? page)
        {
            var seller = await _db.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sellerId);

            if (seller == null)
            {
                TempData["error"] = _localizer["SellerNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            var reports = await _reportService.GetSellerReportsAsync(sellerId, page ?? 1, 12);
            var totalCount = await _reportService.GetSellerReportsCountAsync(sellerId);

            // Calculate total pending balance
            var pendingBalance = await _reportService.GetSellerPendingBalanceAsync(sellerId);

            ViewBag.Seller = seller;
            ViewBag.PendingBalance = pendingBalance;
            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page ?? 1;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "MonthlyReport";

            return View(reports.ToPagedList(page ?? 1, 12));
        }

        // POST: Manually finalize a report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeReport(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                await _reportService.FinalizeSellerReportAsync(id, user?.Id);

                TempData["success"] = _localizer["ReportFinalizedSuccessfully"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing report {ReportId}", id);
                TempData["error"] = _localizer["ErrorFinalizingReport"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Finalize all reports for a month
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeMonth(int year, int month)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                await _reportService.FinalizeMonthAsync(year, month, user?.Id);

                TempData["success"] = _localizer["AllReportsFinalized"].Value;
                return RedirectToAction(nameof(Index), new { year, month });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing month {Year}-{Month}", year, month);
                TempData["error"] = _localizer["ErrorFinalizingMonth"].Value;
                return RedirectToAction(nameof(Index), new { year, month });
            }
        }

        // POST: Recalculate a report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecalculateReport(int id)
        {
            try
            {
                await _reportService.RecalculateReportAsync(id);

                TempData["success"] = _localizer["ReportRecalculatedSuccessfully"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating report {ReportId}", id);
                TempData["error"] = _localizer["ErrorRecalculatingReport"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Add adjustment to a report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdjustment(int reportId, decimal amount, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return Json(new { success = false, message = "Adjustment reason is required." });
                }

                var user = await _userManager.GetUserAsync(User);
                await _reportService.AddAdjustmentAsync(reportId, amount, reason, user?.Id ?? "System");

                return Json(new { success = true, message = "Adjustment added successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding adjustment to report {ReportId}", reportId);
                return Json(new { success = false, message = "An error occurred while adding the adjustment." });
            }
        }

        // POST: Process month-end (for manual trigger)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessMonthEnd()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var result = await _reportService.ProcessMonthEndAsync(user?.Id);

                if (result.Success)
                {
                    TempData["success"] = _localizer["MonthEndProcessingCompleted"].Value;
                }
                else
                {
                    TempData["error"] = _localizer["MonthEndProcessingCompletedWithErrors"].Value;
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing month-end");
                TempData["error"] = _localizer["ErrorProcessingMonthEnd"].Value;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Reports pending payout
        public async Task<IActionResult> PendingPayout(int? page)
        {
            var pendingReports = await _reportService.GetReportsPendingPayoutAsync();
            var reportsList = pendingReports.ToList();

            // Group by seller for summary
            var sellerGroups = reportsList
                .GroupBy(r => r.SellerId)
                .Select(g => new
                {
                    SellerId = g.Key,
                    SellerName = g.First().Seller?.ShopName ?? "Unknown",
                    ReportCount = g.Count(),
                    TotalPayable = g.Sum(r => r.NetPayable)
                })
                .OrderByDescending(x => x.TotalPayable)
                .ToList();

            ViewBag.SellerGroups = sellerGroups;
            ViewBag.TotalPendingAmount = reportsList.Sum(r => r.NetPayable);
            ViewBag.TotalPendingReports = reportsList.Count;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "MonthlyReport";

            return View(reportsList.ToPagedList(page ?? 1, 20));
        }

        // GET: Print a single report
        public async Task<IActionResult> PrintReport(int id)
        {
            var report = await _db.SellerMonthlyReports
                .Include(r => r.Seller)
                    .ThenInclude(s => s!.User)
                .Include(r => r.Seller)
                    .ThenInclude(s => s!.BankAccounts)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                TempData["error"] = _localizer["ReportNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            var reportItems = await _reportService.GetReportItemsAsync(id, 1, 500);

            ViewBag.ReportItems = reportItems;
            ViewBag.PrintDate = DateTime.Now;
            ViewBag.GeneratedBy = (await _userManager.GetUserAsync(User))?.Email ?? "System";

            return View(report);
        }

        // GET: Print all pending reports for a month
        public async Task<IActionResult> PrintMonthlyPending(int year, int month)
        {
            var reports = await _db.SellerMonthlyReports
                .Include(r => r.Seller)
                    .ThenInclude(s => s!.User)
                .Where(r => r.Year == year && r.Month == month &&
                           (r.Status == MonthlyReportStatus.PendingPayout || r.Status == MonthlyReportStatus.Finalized))
                .OrderByDescending(r => r.NetPayable)
                .ToListAsync();

            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.MonthName = new DateTime(year, month, 1).ToString("MMMM yyyy");
            ViewBag.PrintDate = DateTime.Now;
            ViewBag.GeneratedBy = (await _userManager.GetUserAsync(User))?.Email ?? "System";
            ViewBag.TotalPayable = reports.Sum(r => r.NetPayable);
            ViewBag.TotalSales = reports.Sum(r => r.TotalSales);
            ViewBag.TotalCommission = reports.Sum(r => r.CommissionAmount);

            return View(reports);
        }

        // POST: Seed historical data - One-time migration of existing orders
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> SeedHistoricalData()
        {
            try
            {
                _logger.LogInformation("Admin initiated historical data seeding for monthly reports");

                var result = await _reportService.SeedHistoricalDataAsync();

                if (result.Success)
                {
                    TempData["success"] = _localizer["HistoricalDataSeedingCompleted"].Value;
                }
                else
                {
                    TempData["warning"] = _localizer["HistoricalDataSeedingCompletedWithErrors"].Value;
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during historical data seeding");
                TempData["error"] = _localizer["ErrorSeedingHistoricalData"].Value;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
