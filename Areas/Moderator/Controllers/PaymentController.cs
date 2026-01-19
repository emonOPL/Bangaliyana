using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Models.ViewModels;
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
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ISellerMonthlyReportService _monthlyReportService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public PaymentController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            ISellerMonthlyReportService monthlyReportService,
            ILogger<PaymentController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _userManager = userManager;
            _notificationService = notificationService;
            _monthlyReportService = monthlyReportService;
            _logger = logger;
            _localizer = localizer;
        }

        // GET: Overview of all payments
        public async Task<IActionResult> Index(int? page, string? type, string? status)
        {
            var query = _db.Transactions
                .Include(t => t.Order)
                .Include(t => t.User)
                .Include(t => t.Seller)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, out var transactionType))
            {
                query = query.Where(t => t.Type == transactionType);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TransactionStatus>(status, out var transactionStatus))
            {
                query = query.Where(t => t.Status == transactionStatus);
            }

            query = query.OrderByDescending(t => t.CreatedAt);

            var transactions = await query.ToListAsync();
            var pagedTransactions = transactions.ToPagedList(page ?? 1, 20);

            // Stats
            ViewBag.TotalTransactions = await _db.Transactions.CountAsync();
            ViewBag.PendingPayouts = await _db.Transactions.CountAsync(t => t.Type == TransactionType.SellerPayout && t.Status == TransactionStatus.Pending);
            ViewBag.PendingRefunds = await _db.Transactions.CountAsync(t => t.Type == TransactionType.Refund && t.Status == TransactionStatus.Pending);
            ViewBag.TotalRevenue = await _db.Transactions.Where(t => t.Type == TransactionType.Payment && t.Status == TransactionStatus.Completed).SumAsync(t => t.Amount);

            ViewBag.CurrentType = type;
            ViewBag.CurrentStatus = status;
            ViewBag.TransactionTypes = Enum.GetValues<TransactionType>().ToList();
            ViewBag.TransactionStatuses = Enum.GetValues<TransactionStatus>().ToList();
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Payment";

            return View(pagedTransactions);
        }

        // GET: Seller payment management - Now using Monthly Reports for consistency
        public async Task<IActionResult> SellerPayments(int? page, int? sellerId, string? status, int? year, int? month)
        {
            var currentYear = year ?? DateTime.UtcNow.Year;
            var currentMonth = month ?? DateTime.UtcNow.Month;

            // Get all sellers with their reports for the selected month
            var sellersQuery = _db.Sellers
                .Include(s => s.User)
                .Include(s => s.BankAccounts)
                .Where(s => s.Status == SellerStatus.Approved);

            if (sellerId.HasValue)
            {
                sellersQuery = sellersQuery.Where(s => s.Id == sellerId.Value);
            }

            var sellers = await sellersQuery.ToListAsync();

            // Get monthly reports for the current month
            var currentMonthReports = await _db.SellerMonthlyReports
                .Where(r => r.Year == currentYear && r.Month == currentMonth)
                .ToListAsync();

            // Get all reports that are pending payout (from any month)
            var pendingPayoutReports = await _db.SellerMonthlyReports
                .Where(r => r.Status == MonthlyReportStatus.PendingPayout ||
                           r.Status == MonthlyReportStatus.Finalized)
                .GroupBy(r => r.SellerId)
                .Select(g => new
                {
                    SellerId = g.Key,
                    TotalPendingPayable = g.Sum(r => r.NetPayable),
                    PendingReportsCount = g.Count()
                })
                .ToListAsync();

            var sellerPayments = sellers.Select(seller =>
            {
                var currentReport = currentMonthReports.FirstOrDefault(r => r.SellerId == seller.Id);
                var pendingInfo = pendingPayoutReports.FirstOrDefault(p => p.SellerId == seller.Id);

                return new SellerPaymentViewModel
                {
                    SellerId = seller.Id,
                    ShopName = seller.ShopName,
                    SellerEmail = seller.User?.Email ?? "",
                    TotalOrders = currentReport?.TotalOrders ?? 0,
                    TotalSales = currentReport?.TotalSales ?? 0,
                    CommissionRate = seller.CommissionRate,
                    CommissionAmount = currentReport?.CommissionAmount ?? 0,
                    // Payable amount from pending payout reports (not current month which is still open)
                    PayableAmount = pendingInfo?.TotalPendingPayable ?? 0,
                    HasBankAccount = seller.BankAccounts?.Any(b => b.IsVerified) ?? false,
                    HasMobileBanking = seller.MobileBankingProvider != MobileBankingProvider.None,
                    // Additional fields for monthly report view
                    CurrentMonthSales = currentReport?.TotalSales ?? 0,
                    CurrentMonthPayable = currentReport?.NetPayable ?? 0,
                    PendingPayoutReportsCount = pendingInfo?.PendingReportsCount ?? 0,
                    CarryForwardAmount = currentReport?.CarryForwardFromPrevious ?? 0
                };
            })
            .Where(s => s.TotalSales > 0 || s.PayableAmount > 0 || s.CurrentMonthSales > 0)
            .OrderByDescending(s => s.PayableAmount)
            .ThenByDescending(s => s.CurrentMonthSales)
            .ToList();

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "pending")
                    sellerPayments = sellerPayments.Where(s => s.PayableAmount >= 500).ToList();
                else if (status == "below_minimum")
                    sellerPayments = sellerPayments.Where(s => s.PayableAmount > 0 && s.PayableAmount < 500).ToList();
            }

            ViewBag.TotalPayable = sellerPayments.Sum(s => s.PayableAmount);
            ViewBag.TotalCommission = sellerPayments.Sum(s => s.CommissionAmount);
            ViewBag.SellerCount = sellerPayments.Count;
            ViewBag.CurrentYear = currentYear;
            ViewBag.CurrentMonth = currentMonth;
            ViewBag.MonthName = new DateTime(currentYear, currentMonth, 1).ToString("MMMM yyyy");
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Payment";
            ViewBag.CurrentStatus = status;

            return View(sellerPayments.ToPagedList(page ?? 1, 20));
        }

        // GET: Seller payment details - Now using Monthly Reports
        public async Task<IActionResult> SellerPaymentDetails(int id, int? reportId)
        {
            var seller = await _db.Sellers
                .Include(s => s.User)
                .Include(s => s.BankAccounts.Where(b => b.IsVerified))
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                TempData["error"] = _localizer["SellerNotFound"].Value;
                return RedirectToAction(nameof(SellerPayments));
            }

            // Get all monthly reports for this seller
            var monthlyReports = await _db.SellerMonthlyReports
                .Where(r => r.SellerId == id)
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .ToListAsync();

            // Get the current/selected report with order items
            SellerMonthlyReport? selectedReport = null;
            IEnumerable<MonthlyReportOrderItem> reportItems = new List<MonthlyReportOrderItem>();

            if (reportId.HasValue)
            {
                selectedReport = monthlyReports.FirstOrDefault(r => r.Id == reportId.Value);
            }
            else
            {
                // Default to current month or latest report
                var now = DateTime.UtcNow;
                selectedReport = monthlyReports.FirstOrDefault(r => r.Year == now.Year && r.Month == now.Month)
                                ?? monthlyReports.FirstOrDefault();
            }

            if (selectedReport != null)
            {
                reportItems = await _monthlyReportService.GetReportItemsAsync(selectedReport.Id, 1, 100);
            }

            // Get previous payouts from ManualSellerPayouts
            var previousPayouts = await _db.ManualSellerPayouts
                .Where(p => p.SellerId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Calculate totals from monthly reports (for pending payout)
            var pendingPayoutAmount = monthlyReports
                .Where(r => r.Status == MonthlyReportStatus.PendingPayout || r.Status == MonthlyReportStatus.Finalized)
                .Sum(r => r.NetPayable);

            var totalPaidAmount = monthlyReports
                .Where(r => r.Status == MonthlyReportStatus.Paid)
                .Sum(r => r.PayoutAmount);

            // Summary across all reports
            var allTimeTotalSales = monthlyReports.Sum(r => r.TotalSales);
            var allTimeCommission = monthlyReports.Sum(r => r.CommissionAmount);

            ViewBag.Seller = seller;
            ViewBag.MonthlyReports = monthlyReports;
            ViewBag.SelectedReport = selectedReport;
            ViewBag.ReportItems = reportItems;
            ViewBag.PreviousPayouts = previousPayouts;
            ViewBag.TotalSales = allTimeTotalSales;
            ViewBag.CommissionAmount = allTimeCommission;
            ViewBag.PayableAmount = Math.Max(0, pendingPayoutAmount);
            ViewBag.TotalPaidAmount = totalPaidAmount;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Payment";

            return View(seller);
        }

        // POST: Complete Payment - Now integrates with Monthly Reports
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePayment(
            int sellerId,
            decimal amount,
            string paymentMethod,
            // Bank fields (Receiver - Seller's Bank)
            string? bankName,
            string? bankAccountNumber,
            string? bankAccountHolderName,
            string? bankBranchName,
            string? bankRoutingNumber,
            string? bankTransactionReference,
            string? paymentDateBank,
            // Bank fields (Sender - Company's Bank)
            string? senderBankName,
            string? senderAccountNumber,
            // Mobile banking fields
            string? senderNumber,
            string? receiverMobileNumber,
            string? transactionId,
            string? mbPaymentDate,
            // Cash fields
            string? paymentDateCash,
            string? cashReference,
            // Common
            string? adminNotes,
            // Monthly Report Integration
            int? reportId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var seller = await _db.Sellers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == sellerId);

                if (seller == null)
                {
                    return Json(new { success = false, message = "Seller not found." });
                }

                if (amount <= 0)
                {
                    return Json(new { success = false, message = "Invalid payout amount." });
                }

                // Calculate max payable amount from pending payout reports
                var pendingReports = await _db.SellerMonthlyReports
                    .Where(r => r.SellerId == sellerId &&
                               (r.Status == MonthlyReportStatus.PendingPayout || r.Status == MonthlyReportStatus.Finalized))
                    .ToListAsync();

                var maxPayableAmount = pendingReports.Sum(r => r.NetPayable);

                // If no reports exist yet, fall back to legacy calculation
                if (!pendingReports.Any())
                {
                    var sellerOrders = await _db.OrderItems
                        .Include(oi => oi.Order)
                        .Where(oi => oi.Product!.SellerId == sellerId &&
                                    oi.Order.Status == OrderStatus.Delivered &&
                                    oi.Order.IsPaymentReceived)
                        .ToListAsync();

                    var totalSalesLegacy = Math.Round(sellerOrders.Sum(oi => oi.TotalPrice));
                    var commissionAmountLegacy = Math.Round(totalSalesLegacy * (seller.CommissionRate / 100));
                    maxPayableAmount = Math.Max(0, totalSalesLegacy - commissionAmountLegacy);
                }

                if (amount > maxPayableAmount)
                {
                    return Json(new { success = false, message = $"Amount exceeds maximum payable amount of ৳{maxPayableAmount:N0}." });
                }

                // Calculate total sales from reports or legacy
                var totalSales = pendingReports.Sum(r => r.TotalSales);
                var commissionAmount = pendingReports.Sum(r => r.CommissionAmount);

                // Parse payment method
                if (!Enum.TryParse<ManualPayoutMethod>(paymentMethod, out var method))
                {
                    return Json(new { success = false, message = "Invalid payment method." });
                }

                // Generate payout number
                var payoutNumber = $"PO{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}";

                // Parse payment date
                DateTime? paymentDate = null;
                if (method == ManualPayoutMethod.BankTransfer && !string.IsNullOrEmpty(paymentDateBank))
                    paymentDate = DateTime.Parse(paymentDateBank);
                else if (method != ManualPayoutMethod.BankTransfer && method != ManualPayoutMethod.Cash && !string.IsNullOrEmpty(mbPaymentDate))
                    paymentDate = DateTime.Parse(mbPaymentDate);
                else if (method == ManualPayoutMethod.Cash && !string.IsNullOrEmpty(paymentDateCash))
                    paymentDate = DateTime.Parse(paymentDateCash);
                else
                    paymentDate = DateTime.Today;

                // Calculate period from reports or use default
                var periodStart = pendingReports.Any()
                    ? pendingReports.Min(r => r.PeriodStart)
                    : DateTime.UtcNow.AddMonths(-1);
                var periodEnd = pendingReports.Any()
                    ? pendingReports.Max(r => r.PeriodEnd)
                    : DateTime.UtcNow;

                var payout = new ManualSellerPayout
                {
                    PayoutNumber = payoutNumber,
                    SellerId = sellerId,
                    Amount = amount,
                    PaymentMethod = method,
                    Status = ManualPayoutStatus.Completed,
                    PaymentDate = paymentDate,
                    TotalSales = totalSales,
                    CommissionRate = seller.CommissionRate,
                    CommissionAmount = commissionAmount,
                    AdminNotes = adminNotes,
                    ProcessedById = user?.Id,
                    ProcessedByName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "System",
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                // Set method-specific fields
                if (method == ManualPayoutMethod.BankTransfer)
                {
                    // Receiver (Seller's Bank)
                    payout.BankName = bankName;
                    payout.BankAccountNumber = bankAccountNumber;
                    payout.BankAccountHolderName = bankAccountHolderName;
                    payout.BankBranchName = bankBranchName;
                    payout.BankRoutingNumber = bankRoutingNumber;
                    payout.BankTransactionReference = bankTransactionReference;
                    // Sender (Company's Bank)
                    payout.SenderBankName = senderBankName;
                    payout.SenderAccountNumber = senderAccountNumber;
                }
                else if (method == ManualPayoutMethod.bKash || method == ManualPayoutMethod.Nagad ||
                         method == ManualPayoutMethod.Rocket || method == ManualPayoutMethod.Upay)
                {
                    payout.MobileBankingProvider = method.ToString();
                    payout.SenderMobileNumber = senderNumber;
                    payout.ReceiverMobileNumber = receiverMobileNumber;
                    payout.MobileTransactionId = transactionId;
                }
                else if (method == ManualPayoutMethod.Cash)
                {
                    payout.Notes = cashReference;
                }

                _db.ManualSellerPayouts.Add(payout);
                await _db.SaveChangesAsync();

                // Also create a Transaction record for consistency
                var transaction = new Transaction
                {
                    OrderId = null,
                    SellerId = sellerId,
                    Type = TransactionType.SellerPayout,
                    Amount = amount,
                    GatewayTransactionId = method == ManualPayoutMethod.BankTransfer ? bankTransactionReference : transactionId,
                    Notes = $"Manual payout #{payoutNumber} via {method}",
                    Status = TransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                _db.Transactions.Add(transaction);
                await _db.SaveChangesAsync();

                // Update monthly reports - mark as paid
                if (pendingReports.Any())
                {
                    var remainingAmount = amount;
                    foreach (var report in pendingReports.OrderBy(r => r.Year).ThenBy(r => r.Month))
                    {
                        if (remainingAmount <= 0) break;

                        var paymentForThisReport = Math.Min(remainingAmount, report.NetPayable);
                        report.PayoutAmount = paymentForThisReport;
                        report.ManualSellerPayoutId = payout.Id;
                        report.Status = MonthlyReportStatus.Paid;
                        remainingAmount -= paymentForThisReport;
                    }
                    await _db.SaveChangesAsync();
                }
                else if (reportId.HasValue)
                {
                    // Link to specific report if provided
                    await _monthlyReportService.LinkPayoutToReportAsync(reportId.Value, payout.Id);
                }

                // Notify seller
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = seller.UserId,
                    Type = "payment",
                    Icon = "fa-money-bill-wave",
                    IconColor = "text-success",
                    Title = "Payment Received",
                    Message = $"You have received a payment of ৳{amount:N0} via {method}. Payout Number: {payoutNumber}",
                    Link = "/Seller/Reports"
                });

                _logger.LogInformation("Payment completed for seller {SellerId}. Amount: {Amount}, Method: {Method}, PayoutNumber: {PayoutNumber}",
                    sellerId, amount, method, payoutNumber);

                return Json(new { success = true, message = $"Payment of ৳{amount:N0} completed successfully! Payout Number: {payoutNumber}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing payment for seller {SellerId}", sellerId);
                return Json(new { success = false, message = "An error occurred while processing the payment." });
            }
        }

        // GET: Print Pending Payouts PDF - Now using Monthly Reports
        public async Task<IActionResult> PrintPendingPayouts(int? month, int? year)
        {
            var targetMonth = month ?? DateTime.Today.Month;
            var targetYear = year ?? DateTime.Today.Year;

            // Get all pending payout reports for the specified month
            var pendingReports = await _db.SellerMonthlyReports
                .Include(r => r.Seller)
                    .ThenInclude(s => s!.User)
                .Include(r => r.Seller)
                    .ThenInclude(s => s!.BankAccounts)
                .Where(r => r.Year == targetYear && r.Month == targetMonth &&
                           (r.Status == MonthlyReportStatus.PendingPayout || r.Status == MonthlyReportStatus.Finalized))
                .OrderByDescending(r => r.NetPayable)
                .ToListAsync();

            // Transform to expected format for view
            var pendingPayments = pendingReports
                .Where(r => r.Seller != null)
                .Select(r => new
                {
                    Seller = r.Seller!,
                    TotalSales = r.TotalSales,
                    CommissionAmount = r.CommissionAmount,
                    PayableAmount = r.NetPayable,
                    ReportNumber = r.ReportNumber,
                    CarryForward = r.CarryForwardFromPrevious
                })
                .Where(x => x.PayableAmount > 0)
                .ToList();

            ViewBag.PendingPayments = pendingPayments;
            ViewBag.Month = targetMonth;
            ViewBag.Year = targetYear;
            ViewBag.PrintDate = DateTime.Now;
            ViewBag.GeneratedBy = (await _userManager.GetUserAsync(User))?.Email ?? "System";
            ViewBag.TotalPayable = pendingPayments.Sum(p => p.PayableAmount);

            return View("PrintPendingPayouts");
        }

        // GET: Print Completed Payouts PDF
        public async Task<IActionResult> PrintCompletedPayouts(int? month, int? year)
        {
            var targetMonth = month ?? DateTime.Today.Month;
            var targetYear = year ?? DateTime.Today.Year;
            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = startDate.AddMonths(1);

            var completedPayouts = await _db.ManualSellerPayouts
                .Include(p => p.Seller)
                    .ThenInclude(s => s!.User)
                .Where(p => p.Status == ManualPayoutStatus.Completed &&
                           p.CompletedAt >= startDate && p.CompletedAt < endDate)
                .OrderByDescending(p => p.CompletedAt)
                .ToListAsync();

            ViewBag.CompletedPayouts = completedPayouts;
            ViewBag.Month = targetMonth;
            ViewBag.Year = targetYear;
            ViewBag.PrintDate = DateTime.Now;
            ViewBag.GeneratedBy = (await _userManager.GetUserAsync(User))?.Email ?? "System";
            ViewBag.TotalAmount = completedPayouts.Sum(p => p.Amount);

            return View("PrintCompletedPayouts");
        }

        // GET: Print Pending Payouts for specific seller - Now using Monthly Reports
        public async Task<IActionResult> PrintSellerPendingPayouts(int sellerId)
        {
            var seller = await _db.Sellers
                .Include(s => s.User)
                .Include(s => s.BankAccounts.Where(b => b.IsVerified))
                .FirstOrDefaultAsync(s => s.Id == sellerId);

            if (seller == null)
            {
                TempData["error"] = _localizer["SellerNotFound"].Value;
                return RedirectToAction(nameof(SellerPayments));
            }

            // Get all pending payout reports for this seller
            var pendingReports = await _db.SellerMonthlyReports
                .Where(r => r.SellerId == sellerId &&
                           (r.Status == MonthlyReportStatus.PendingPayout || r.Status == MonthlyReportStatus.Finalized))
                .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
                .ToListAsync();

            // Get report items for all pending reports
            var reportItems = new List<MonthlyReportOrderItem>();
            foreach (var report in pendingReports)
            {
                var items = await _monthlyReportService.GetReportItemsAsync(report.Id, 1, 100);
                reportItems.AddRange(items);
            }

            var totalPaidAmount = await _db.SellerMonthlyReports
                .Where(r => r.SellerId == sellerId && r.Status == MonthlyReportStatus.Paid)
                .SumAsync(r => r.PayoutAmount);

            var totalSales = pendingReports.Sum(r => r.TotalSales);
            var commissionAmount = pendingReports.Sum(r => r.CommissionAmount);
            var payableAmount = pendingReports.Sum(r => r.NetPayable);
            var carryForward = pendingReports.Sum(r => r.CarryForwardFromPrevious);

            ViewBag.Seller = seller;
            ViewBag.PendingReports = pendingReports;
            ViewBag.ReportItems = reportItems;
            ViewBag.TotalSales = totalSales;
            ViewBag.CommissionAmount = commissionAmount;
            ViewBag.PayableAmount = payableAmount;
            ViewBag.TotalPaidAmount = totalPaidAmount;
            ViewBag.CarryForwardAmount = carryForward;
            ViewBag.PrintDate = DateTime.Now;
            ViewBag.GeneratedBy = (await _userManager.GetUserAsync(User))?.Email ?? "System";

            return View("PrintSellerPendingPayouts");
        }

        // GET: Print Completed Payouts for specific seller - Now includes Monthly Report info
        public async Task<IActionResult> PrintSellerCompletedPayouts(int sellerId)
        {
            var seller = await _db.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sellerId);

            if (seller == null)
            {
                TempData["error"] = _localizer["SellerNotFound"].Value;
                return RedirectToAction(nameof(SellerPayments));
            }

            // Get all paid reports for this seller
            var paidReports = await _db.SellerMonthlyReports
                .Where(r => r.SellerId == sellerId && r.Status == MonthlyReportStatus.Paid)
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .ToListAsync();

            // Also get manual payouts
            var completedPayouts = await _db.ManualSellerPayouts
                .Where(p => p.SellerId == sellerId && p.Status == ManualPayoutStatus.Completed)
                .OrderByDescending(p => p.CompletedAt)
                .ToListAsync();

            ViewBag.Seller = seller;
            ViewBag.PaidReports = paidReports;
            ViewBag.CompletedPayouts = completedPayouts;
            ViewBag.PrintDate = DateTime.Now;
            ViewBag.GeneratedBy = (await _userManager.GetUserAsync(User))?.Email ?? "System";
            ViewBag.TotalAmount = paidReports.Sum(r => r.PayoutAmount);

            return View("PrintSellerCompletedPayouts");
        }

        // GET: Payout Details
        public async Task<IActionResult> PayoutDetails(int id)
        {
            var payout = await _db.ManualSellerPayouts
                .Include(p => p.Seller)
                    .ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payout == null)
            {
                TempData["error"] = _localizer["PayoutNotFound"].Value;
                return RedirectToAction(nameof(SellerPayments));
            }

            return View(payout);
        }

        // GET: Edit Payout
        public async Task<IActionResult> EditPayout(int id)
        {
            var payout = await _db.ManualSellerPayouts
                .Include(p => p.Seller)
                    .ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payout == null)
            {
                TempData["error"] = _localizer["PayoutNotFound"].Value;
                return RedirectToAction(nameof(SellerPayments));
            }

            return View(payout);
        }

        // POST: Update Payout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePayout(int id, ManualSellerPayout model)
        {
            var payout = await _db.ManualSellerPayouts
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payout == null)
            {
                TempData["error"] = _localizer["PayoutNotFound"].Value;
                return RedirectToAction(nameof(SellerPayments));
            }

            // Update editable fields
            payout.Amount = model.Amount;
            payout.PaymentMethod = model.PaymentMethod;
            payout.PaymentDate = model.PaymentDate;
            payout.Status = model.Status;
            payout.AdminNotes = model.AdminNotes;

            // Bank Transfer fields
            payout.SenderBankName = model.SenderBankName;
            payout.SenderAccountNumber = model.SenderAccountNumber;
            payout.BankTransactionReference = model.BankTransactionReference;

            // Mobile Banking fields
            payout.SenderMobileNumber = model.SenderMobileNumber;
            payout.MobileTransactionId = model.MobileTransactionId;

            // Cash fields
            payout.Notes = model.Notes;

            payout.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["success"] = _localizer["PayoutUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(SellerPaymentDetails), new { id = payout.SellerId });
        }

        // POST: Process seller payout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSellerPayout(int sellerId, decimal amount, string paymentMethod, string? transactionId, string? notes)
        {
            try
            {
                var seller = await _db.Sellers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == sellerId);

                if (seller == null)
                {
                    return Json(new { success = false, message = "Seller not found." });
                }

                if (amount <= 0)
                {
                    return Json(new { success = false, message = "Invalid payout amount." });
                }

                // Calculate max payable amount
                var sellerOrders = await _db.OrderItems
                    .Include(oi => oi.Order)
                    .Where(oi => oi.Product!.SellerId == sellerId &&
                                oi.Order.Status == OrderStatus.Delivered &&
                                oi.Order.IsPaymentReceived)
                    .ToListAsync();

                var totalSales = sellerOrders.Sum(oi => oi.TotalPrice);
                var maxPayableAmount = totalSales * (1 - seller.CommissionRate / 100);

                if (amount > maxPayableAmount)
                {
                    return Json(new { success = false, message = $"Amount exceeds maximum payable amount of ৳{maxPayableAmount:N0}." });
                }

                var transaction = new Transaction
                {
                    OrderId = null,
                    SellerId = sellerId,
                    Type = TransactionType.SellerPayout,
                    Amount = amount,
                    GatewayTransactionId = transactionId,
                    Notes = $"Seller payout via {paymentMethod}" + (string.IsNullOrEmpty(notes) ? "" : $" - {notes}"),
                    Status = TransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                _db.Transactions.Add(transaction);
                await _db.SaveChangesAsync();

                // Notify seller
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = seller.UserId,
                    Type = "payment",
                    Icon = "fa-money-bill-wave",
                    IconColor = "text-success",
                    Title = "Payment Received",
                    Message = $"You have received a payment of ৳{amount:N2} via {paymentMethod}.",
                    Link = "/Seller/Payments"
                });

                return Json(new { success = true, message = $"Payout of ৳{amount:N2} processed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing seller payout for seller {SellerId}", sellerId);
                return Json(new { success = false, message = "An error occurred while processing the payout." });
            }
        }

        // GET: Customer refunds
        public async Task<IActionResult> CustomerRefunds(int? page, string? status)
        {
            // Only show orders that have actual return requests (ReturnReason != null)
            var query = _db.Orders
                .Include(o => o.User)
                .Where(o => o.ReturnReason != null)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RefundStatus>(status, out var refundStatus))
            {
                query = query.Where(o => o.RefundStatus == refundStatus);
            }

            query = query.OrderByDescending(o => o.RefundStatusChangedAt ?? o.UpdatedAt);

            var orders = await query.ToListAsync();
            var pagedOrders = orders.ToPagedList(page ?? 1, 20);

            // Calculate all counts for stats and tabs - only for orders with actual return requests
            ViewBag.TotalRefunds = await _db.Orders.CountAsync(o => o.ReturnReason != null);
            ViewBag.PendingRefunds = await _db.Orders.CountAsync(o => o.ReturnReason != null && (o.RefundStatus == RefundStatus.Pending || o.RefundStatus == RefundStatus.Reviewing));
            ViewBag.ProcessingRefunds = await _db.Orders.CountAsync(o => o.ReturnReason != null && o.RefundStatus == RefundStatus.InProgress);
            ViewBag.ApprovedRefunds = await _db.Orders.CountAsync(o => o.ReturnReason != null && o.RefundStatus == RefundStatus.Approved);
            ViewBag.RejectedRefunds = await _db.Orders.CountAsync(o => o.ReturnReason != null && o.RefundStatus == RefundStatus.Rejected);
            ViewBag.CompletedRefunds = await _db.Orders.CountAsync(o => o.ReturnReason != null && o.RefundStatus == RefundStatus.Completed);
            ViewBag.CurrentStatus = status;
            ViewBag.RefundStatuses = Enum.GetValues<RefundStatus>().ToList();
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Payment";

            return View(pagedOrders);
        }

        // GET: Transaction details
        public async Task<IActionResult> TransactionDetails(int id)
        {
            var transaction = await _db.Transactions
                .Include(t => t.Order)
                    .ThenInclude(o => o!.User)
                .Include(t => t.Seller)
                    .ThenInclude(s => s!.User)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                TempData["error"] = _localizer["TransactionNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Payment";

            return View(transaction);
        }

        // POST: Update transaction status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTransactionStatus(int id, TransactionStatus status, string? notes)
        {
            try
            {
                var transaction = await _db.Transactions.FindAsync(id);
                if (transaction == null)
                {
                    return Json(new { success = false, message = "Transaction not found." });
                }

                transaction.Status = status;
                if (status == TransactionStatus.Completed)
                {
                    transaction.CompletedAt = DateTime.UtcNow;
                }

                if (!string.IsNullOrEmpty(notes))
                {
                    transaction.Notes = string.IsNullOrEmpty(transaction.Notes)
                        ? notes
                        : $"{transaction.Notes}\n{notes}";
                }

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = $"Transaction status updated to {status}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction {TransactionId}", id);
                return Json(new { success = false, message = "An error occurred while updating the transaction." });
            }
        }
    }
}
