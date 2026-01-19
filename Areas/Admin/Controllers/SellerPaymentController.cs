using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Models.ViewModels;
using Bangaliyana.Services;
using Bangaliyana.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Hangfire;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,Moderator")]
    public class SellerPaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISellerPaymentService _paymentService;
        private readonly ISellerMonthlyReportService _monthlyReportService;
        private readonly IFileValidationService _fileValidationService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SellerPaymentController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SellerPaymentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISellerPaymentService paymentService,
            ISellerMonthlyReportService monthlyReportService,
            IFileValidationService fileValidationService,
            INotificationService notificationService,
            ILogger<SellerPaymentController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _paymentService = paymentService;
            _monthlyReportService = monthlyReportService;
            _fileValidationService = fileValidationService;
            _notificationService = notificationService;
            _logger = logger;
            _localizer = localizer;
        }

        // GET: Admin/SellerPayment - Seller Earnings Overview - Now using Monthly Reports
        public async Task<IActionResult> Index(int? page, int? sellerId, string? status, int? year, int? month)
        {
            var currentYear = year ?? DateTime.UtcNow.Year;
            var currentMonth = month ?? DateTime.UtcNow.Month;

            // Get all sellers with their reports for the selected month
            var sellersQuery = _context.Sellers
                .Include(s => s.User)
                .Include(s => s.BankAccounts)
                .Where(s => s.Status == SellerStatus.Approved);

            if (sellerId.HasValue)
            {
                sellersQuery = sellersQuery.Where(s => s.Id == sellerId.Value);
            }

            var sellers = await sellersQuery.ToListAsync();

            // Get monthly reports for the current month
            var currentMonthReports = await _context.SellerMonthlyReports
                .Where(r => r.Year == currentYear && r.Month == currentMonth)
                .ToListAsync();

            // Get all reports that are pending payout (from any month)
            var pendingPayoutReports = await _context.SellerMonthlyReports
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
                    PayableAmount = pendingInfo?.TotalPendingPayable ?? 0,
                    HasBankAccount = seller.BankAccounts?.Any(b => b.IsVerified) ?? false,
                    HasMobileBanking = seller.MobileBankingProvider != MobileBankingProvider.None,
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
            ViewBag.AreaName = "Admin";
            ViewBag.ControllerName = "SellerPayment";
            ViewBag.CurrentStatus = status;

            var pageSize = 20;
            var pageNumber = page ?? 1;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling((double)sellerPayments.Count / pageSize);

            return View("~/Areas/Moderator/Views/Payment/SellerPayments.cshtml", X.PagedList.Extensions.PagedListExtensions.ToPagedList(sellerPayments, pageNumber, pageSize));
        }

        // GET: Admin/SellerPayment/PaymentRequests - Formal payment request system
        public async Task<IActionResult> PaymentRequests(SellerPaymentStatus? status, string? search, int page = 1)
        {
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;

            // Get status counts for tabs
            ViewBag.StatusCounts = await _paymentService.GetPaymentStatusCountsAsync();

            var pageSize = 20;
            var payments = await _paymentService.GetAllPaymentsAsync(status, search, page, pageSize);
            var totalCount = await _paymentService.GetPaymentsCountAsync(status, search);

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            // Get summary statistics
            var allPayments = await _context.SellerPayments.ToListAsync();
            ViewBag.TotalPendingAmount = allPayments.Where(p => p.Status == SellerPaymentStatus.Pending).Sum(p => p.Amount);
            ViewBag.TotalProcessingAmount = allPayments.Where(p => p.Status == SellerPaymentStatus.Processing || p.Status == SellerPaymentStatus.UnderReview).Sum(p => p.Amount);
            ViewBag.TotalPaidAmount = allPayments.Where(p => p.Status == SellerPaymentStatus.Paid).Sum(p => p.Amount);
            ViewBag.TotalFailedAmount = allPayments.Where(p => p.Status == SellerPaymentStatus.Failed).Sum(p => p.Amount);

            return View("Index", payments);
        }

        // GET: Admin/SellerPayment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                TempData["error"] = _localizer["PaymentNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Mark messages as read for staff
            await _paymentService.MarkMessagesAsReadAsync(id, forStaff: true);

            ViewBag.Messages = await _paymentService.GetPaymentMessagesAsync(id);

            // Get transactions for this payment period (Order Payouts and Commissions)
            var periodTransactions = await _context.SellerTransactions
                .Include(t => t.Order)
                .Where(t => t.SellerId == payment.SellerId &&
                           t.OrderId != null &&
                           (t.Type == SellerTransactionType.OrderPayout || t.Type == SellerTransactionType.Commission) &&
                           t.CreatedAt >= payment.PeriodStart &&
                           t.CreatedAt <= payment.PeriodEnd.AddDays(1))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.PeriodTransactions = periodTransactions;

            // Calculate totals
            var totalProductPayout = periodTransactions
                .Where(t => t.Type == SellerTransactionType.OrderPayout)
                .Sum(t => t.Amount);
            var totalCommission = periodTransactions
                .Where(t => t.Type == SellerTransactionType.Commission)
                .Sum(t => Math.Abs(t.Amount));

            ViewBag.TotalProductPayout = totalProductPayout;
            ViewBag.TotalCommission = totalCommission;

            // Get seller's all bank accounts for reference
            if (payment.Seller != null)
            {
                ViewBag.SellerBankAccounts = await _context.SellerBankAccounts
                    .Where(b => b.SellerId == payment.SellerId)
                    .ToListAsync();
            }

            return View(payment);
        }

        // GET: Admin/SellerPayment/Edit/5
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _context.SellerPayments
                .Include(p => p.Seller)
                    .ThenInclude(s => s!.User)
                .Include(p => p.Seller)
                    .ThenInclude(s => s!.BankAccounts)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                TempData["error"] = _localizer["PaymentNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Only allow editing for Pending or UnderReview payments
            if (payment.Status != SellerPaymentStatus.Pending && payment.Status != SellerPaymentStatus.UnderReview)
            {
                TempData["error"] = _localizer["OnlyPendingOrUnderReviewPaymentsCanBeEdited"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.PaymentStatuses = Enum.GetValues<SellerPaymentStatus>()
                .Where(s => s != SellerPaymentStatus.Paid && s != SellerPaymentStatus.Failed)
                .ToList();
            ViewBag.PaymentMethods = Enum.GetValues<SellerPaymentMethod>().ToList();

            return View(payment);
        }

        // POST: Admin/SellerPayment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Edit(int id, decimal amount, SellerPaymentMethod paymentMethod,
            string? bankName, string? branchName, string? accountHolderName, string? accountNumber, string? routingNumber,
            MobileBankingProvider? mobileBankingProvider, string? mobileNumber, string? mobileAccountName,
            string? adminNotes)
        {
            var payment = await _context.SellerPayments.FindAsync(id);
            if (payment == null)
            {
                TempData["error"] = _localizer["PaymentNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (payment.Status != SellerPaymentStatus.Pending && payment.Status != SellerPaymentStatus.UnderReview)
            {
                TempData["error"] = _localizer["OnlyPendingOrUnderReviewPaymentsCanBeEdited"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            // Update payment details
            payment.Amount = amount;
            payment.PaymentMethod = paymentMethod;
            payment.AdminNotes = adminNotes;
            payment.UpdatedAt = DateTime.UtcNow;

            if (paymentMethod == SellerPaymentMethod.BankTransfer)
            {
                payment.BankName = bankName;
                payment.BranchName = branchName;
                payment.AccountHolderName = accountHolderName;
                payment.AccountNumber = accountNumber;
                payment.RoutingNumber = routingNumber;
                // Clear mobile banking fields
                payment.MobileBankingProvider = null;
                payment.MobileNumber = null;
                payment.MobileAccountName = null;
            }
            else
            {
                payment.MobileBankingProvider = mobileBankingProvider;
                payment.MobileNumber = mobileNumber;
                payment.MobileAccountName = mobileAccountName;
                // Clear bank fields
                payment.BankName = null;
                payment.BranchName = null;
                payment.AccountHolderName = null;
                payment.AccountNumber = null;
                payment.RoutingNumber = null;
            }

            await _context.SaveChangesAsync();

            // Log the edit
            var user = await _userManager.GetUserAsync(User);
            await _paymentService.AddLogAsync(
                PaymentLogType.StatusChanged,
                $"Payment #{payment.PaymentNumber} edited",
                $"Amount: BDT {(int)Math.Round(amount)}, Method: {paymentMethod}. Edited by: {user?.Email}",
                paymentId: id,
                userId: user?.Id);

            TempData["success"] = _localizer["PaymentUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/SellerPayment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _context.SellerPayments
                .Include(p => p.Seller)
                .Include(p => p.Messages)
                    .ThenInclude(m => m.Attachments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                TempData["error"] = _localizer["PaymentNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Only allow deleting Pending or Failed payments
            if (payment.Status != SellerPaymentStatus.Pending && payment.Status != SellerPaymentStatus.Failed)
            {
                TempData["error"] = _localizer["OnlyPendingOrFailedPaymentsCanBeDeleted"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            var paymentNumber = payment.PaymentNumber;
            var sellerName = payment.Seller?.ShopName ?? "Unknown";

            // If pending, refund the amount back to seller
            if (payment.Status == SellerPaymentStatus.Pending && payment.Seller != null)
            {
                payment.Seller.AccountBalance += payment.Amount;

                // Create refund transaction
                var transaction = new SellerTransaction
                {
                    SellerId = payment.SellerId,
                    Type = SellerTransactionType.Adjustment,
                    Amount = payment.Amount,
                    Description = $"Payment #{paymentNumber} cancelled - amount refunded",
                    BalanceAfter = payment.Seller.AccountBalance,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SellerTransactions.Add(transaction);
            }

            // Delete related messages and attachments
            foreach (var message in payment.Messages)
            {
                _context.SellerPaymentMessageAttachments.RemoveRange(message.Attachments);
            }
            _context.SellerPaymentMessages.RemoveRange(payment.Messages);

            // Delete payment
            _context.SellerPayments.Remove(payment);
            await _context.SaveChangesAsync();

            // Log deletion
            var user = await _userManager.GetUserAsync(User);
            await _paymentService.AddLogAsync(
                PaymentLogType.Error,
                $"Payment #{paymentNumber} deleted",
                $"Seller: {sellerName}. Deleted by: {user?.Email}",
                userId: user?.Id);

            TempData["success"] = _localizer["PaymentDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/SellerPayment/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, SellerPaymentStatus status, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _paymentService.UpdatePaymentStatusAsync(id, status, notes, user.Id);
            if (result)
            {
                TempData["success"] = _localizer["PaymentStatusUpdatedSuccessfully"].Value;
            }
            else
            {
                TempData["error"] = _localizer["FailedToUpdatePaymentStatus"].Value;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/SellerPayment/MarkAsPaid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id, string transactionReference)
        {
            if (string.IsNullOrWhiteSpace(transactionReference))
            {
                TempData["error"] = _localizer["TransactionReferenceRequired"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _paymentService.MarkAsPaidAsync(id, transactionReference, user.Id);
            if (result)
            {
                TempData["success"] = _localizer["PaymentMarkedAsPaidSuccessfully"].Value;
            }
            else
            {
                TempData["error"] = _localizer["FailedToMarkPaymentAsPaid"].Value;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/SellerPayment/MarkAsFailed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsFailed(int id, string failureReason)
        {
            if (string.IsNullOrWhiteSpace(failureReason))
            {
                TempData["error"] = _localizer["FailureReasonRequired"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _paymentService.MarkAsFailedAsync(id, failureReason, user.Id);
            if (result)
            {
                TempData["success"] = _localizer["PaymentMarkedAsFailedSuccessfully"].Value;
            }
            else
            {
                TempData["error"] = _localizer["FailedToUpdatePaymentStatus"].Value;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/SellerPayment/Messages/5
        public async Task<IActionResult> Messages(int id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                TempData["error"] = _localizer["PaymentNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Mark messages as read for staff
            await _paymentService.MarkMessagesAsReadAsync(id, forStaff: true);

            var messages = await _paymentService.GetPaymentMessagesAsync(id);
            ViewBag.Payment = payment;

            return View(messages);
        }

        // POST: Admin/SellerPayment/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int paymentId, string message, IFormFile? attachment)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["error"] = _localizer["MessageCannotBeEmpty"].Value;
                return RedirectToAction(nameof(Messages), new { id = paymentId });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var paymentMessage = await _paymentService.AddPaymentMessageAsync(paymentId, user.Id, message, isStaffReply: true);

            // Handle attachment if provided
            if (attachment != null && attachment.Length > 0)
            {
                if (_fileValidationService.ValidateImage(attachment, 5, out var error) ||
                    _fileValidationService.ValidateDocument(attachment, 10, out error))
                {
                    var fileUrl = await _fileValidationService.SaveFileAsync(attachment, "payment-messages", $"msg_{paymentMessage.Id}_");
                    await _paymentService.AddMessageAttachmentAsync(
                        paymentMessage.Id,
                        attachment.FileName,
                        fileUrl,
                        attachment.ContentType,
                        attachment.Length);
                }
                else
                {
                    TempData["warning"] = _localizer["MessageSentButAttachmentFailed"].Value;
                    return RedirectToAction(nameof(Messages), new { id = paymentId });
                }
            }

            TempData["success"] = _localizer["MessageSentSuccessfully"].Value;
            return RedirectToAction(nameof(Messages), new { id = paymentId });
        }

        // GET: Admin/SellerPayment/Settings
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Settings()
        {
            var settings = await _paymentService.GetSettingsAsync();
            return View(settings);
        }

        // POST: Admin/SellerPayment/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Settings(SellerPaymentSettings model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _paymentService.UpdateSettingsAsync(model);
            TempData["success"] = _localizer["PaymentSettingsUpdatedSuccessfully"].Value;

            return RedirectToAction(nameof(Settings));
        }

        // POST: Admin/SellerPayment/TriggerMonthlyPayments
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> TriggerMonthlyPayments()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            // Log manual trigger
            await _paymentService.AddLogAsync(
                Models.PaymentLogType.ManualTrigger,
                "Monthly payment processing manually triggered",
                $"Triggered by: {user?.Email ?? "Unknown"}",
                userId: userId);

            // Enqueue the job to run immediately
            BackgroundJob.Enqueue<SellerPaymentBackgroundJobs>(x => x.ProcessMonthlyPaymentsAsync(userId));

            TempData["success"] = _localizer["MonthlyPaymentProcessingTriggered"].Value;
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/SellerPayment/Logs
        public async Task<IActionResult> Logs(int page = 1)
        {
            var pageSize = 50;
            var logs = await _paymentService.GetPaymentLogsAsync(page, pageSize);
            var totalCount = await _paymentService.GetPaymentLogsCountAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View(logs);
        }

        // API: Get unread message count for a payment
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount(int id)
        {
            var count = await _paymentService.GetUnreadMessagesCountAsync(id, forStaff: true);
            return Json(new { count });
        }

        // GET: Admin/SellerPayment/SellerPaymentDetails - Now using Monthly Reports
        public async Task<IActionResult> SellerPaymentDetails(int id, int? reportId)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .Include(s => s.BankAccounts.Where(b => b.IsVerified))
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                TempData["error"] = _localizer["SellerNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Get all monthly reports for this seller
            var monthlyReports = await _context.SellerMonthlyReports
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
            var previousPayouts = await _context.ManualSellerPayouts
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
            ViewBag.AreaName = "Admin";
            ViewBag.ControllerName = "SellerPayment";

            return View("~/Areas/Moderator/Views/Payment/SellerPaymentDetails.cshtml", seller);
        }

        // POST: Admin/SellerPayment/ProcessSellerPayout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessSellerPayout(int sellerId, decimal amount, string paymentMethod, string? transactionId, string? notes)
        {
            try
            {
                var seller = await _context.Sellers
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
                var sellerOrders = await _context.OrderItems
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

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

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
                return Json(new { success = false, message = "An error occurred while processing the payout." });
            }
        }

        // POST: Admin/SellerPayment/CompletePayment - Now integrates with Monthly Reports
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
            string? senderMobileNumber,
            string? receiverMobileNumber,
            string? mobileTransactionId,
            string? paymentDateMobile,
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
                var seller = await _context.Sellers
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
                var pendingReports = await _context.SellerMonthlyReports
                    .Where(r => r.SellerId == sellerId &&
                               (r.Status == MonthlyReportStatus.PendingPayout || r.Status == MonthlyReportStatus.Finalized))
                    .ToListAsync();

                var maxPayableAmount = pendingReports.Sum(r => r.NetPayable);

                // If no reports exist yet, fall back to legacy calculation
                if (!pendingReports.Any())
                {
                    var sellerOrders = await _context.OrderItems
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
                else if (method != ManualPayoutMethod.BankTransfer && method != ManualPayoutMethod.Cash && !string.IsNullOrEmpty(paymentDateMobile))
                    paymentDate = DateTime.Parse(paymentDateMobile);
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
                    payout.BankName = bankName;
                    payout.BankAccountNumber = bankAccountNumber;
                    payout.BankAccountHolderName = bankAccountHolderName;
                    payout.BankBranchName = bankBranchName;
                    payout.BankRoutingNumber = bankRoutingNumber;
                    payout.BankTransactionReference = bankTransactionReference;
                    payout.SenderBankName = senderBankName;
                    payout.SenderAccountNumber = senderAccountNumber;
                }
                else if (method == ManualPayoutMethod.bKash || method == ManualPayoutMethod.Nagad ||
                         method == ManualPayoutMethod.Rocket || method == ManualPayoutMethod.Upay)
                {
                    payout.MobileBankingProvider = method.ToString();
                    payout.SenderMobileNumber = senderMobileNumber;
                    payout.ReceiverMobileNumber = receiverMobileNumber;
                    payout.MobileTransactionId = mobileTransactionId;
                }
                else if (method == ManualPayoutMethod.Cash)
                {
                    payout.Notes = cashReference;
                }

                _context.ManualSellerPayouts.Add(payout);
                await _context.SaveChangesAsync();

                // Also create a Transaction record for consistency
                var transaction = new Transaction
                {
                    OrderId = null,
                    SellerId = sellerId,
                    Type = TransactionType.SellerPayout,
                    Amount = amount,
                    GatewayTransactionId = method == ManualPayoutMethod.BankTransfer ? bankTransactionReference : mobileTransactionId,
                    Notes = $"Manual payout #{payoutNumber} via {method}",
                    Status = TransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

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
                    await _context.SaveChangesAsync();
                }
                else if (reportId.HasValue)
                {
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
                _logger.LogError(ex, "Error processing payment for seller {SellerId}", sellerId);
                return Json(new { success = false, message = "An error occurred while processing the payment." });
            }
        }

        // GET: Admin/SellerPayment/PrintPendingPayouts - Now using Monthly Reports
        public async Task<IActionResult> PrintPendingPayouts(int? month, int? year)
        {
            var targetMonth = month ?? DateTime.Today.Month;
            var targetYear = year ?? DateTime.Today.Year;

            // Get all pending payout reports for the specified month
            var pendingReports = await _context.SellerMonthlyReports
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

            return View("~/Areas/Moderator/Views/Payment/PrintPendingPayouts.cshtml");
        }

        // GET: Admin/SellerPayment/PrintCompletedPayouts
        public async Task<IActionResult> PrintCompletedPayouts(int? month, int? year)
        {
            var targetMonth = month ?? DateTime.Today.Month;
            var targetYear = year ?? DateTime.Today.Year;
            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = startDate.AddMonths(1);

            var completedPayouts = await _context.ManualSellerPayouts
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

            return View("~/Areas/Moderator/Views/Payment/PrintCompletedPayouts.cshtml");
        }

        // GET: Admin/SellerPayment/CustomerRefunds
        public async Task<IActionResult> CustomerRefunds(int? page, string? status)
        {
            // Only show orders that have been referred to accounting for refund
            var query = _context.Orders
                .Include(o => o.User)
                .Where(o => o.ReferredToAccounting == true)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RefundStatus>(status, out var refundStatus))
            {
                query = query.Where(o => o.RefundStatus == refundStatus);
            }

            query = query.OrderByDescending(o => o.RefundStatusChangedAt ?? o.ReferredToAccountingAt ?? o.UpdatedAt);

            var orders = await query.ToListAsync();
            var pageSize = 20;
            var pageNumber = page ?? 1;
            var pagedOrders = orders.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            // Count only orders that have been referred to accounting
            ViewBag.TotalRefunds = await _context.Orders.CountAsync(o => o.ReferredToAccounting == true);
            ViewBag.PendingRefunds = await _context.Orders.CountAsync(o => o.ReferredToAccounting == true && o.RefundStatus == RefundStatus.Pending);
            ViewBag.ApprovedRefunds = await _context.Orders.CountAsync(o => o.ReferredToAccounting == true && o.RefundStatus == RefundStatus.Approved);
            ViewBag.ProcessingRefunds = await _context.Orders.CountAsync(o => o.ReferredToAccounting == true && o.RefundStatus == RefundStatus.InProgress);
            ViewBag.RejectedRefunds = await _context.Orders.CountAsync(o => o.ReferredToAccounting == true && o.RefundStatus == RefundStatus.Rejected);
            ViewBag.CompletedRefunds = await _context.Orders.CountAsync(o => o.ReferredToAccounting == true && o.RefundStatus == RefundStatus.Completed);
            ViewBag.CurrentStatus = status;
            ViewBag.RefundStatuses = Enum.GetValues<RefundStatus>().ToList();
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling((double)orders.Count / pageSize);

            return View(pagedOrders);
        }
    }
}
