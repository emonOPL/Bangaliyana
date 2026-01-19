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
    [Authorize(Roles = "SuperAdmin,Admin,Moderator")]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<TransactionController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public TransactionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<TransactionController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
            _localizer = localizer;
        }

        // GET: Admin/Transaction - All Transactions List
        public async Task<IActionResult> Index(
            string? type,
            string? status,
            string? search,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1)
        {
            var query = _context.Transactions
                .Include(t => t.Order)
                    .ThenInclude(o => o!.User)
                .Include(t => t.Seller)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, out var transactionType))
            {
                query = query.Where(t => t.Type == transactionType);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TransactionStatus>(status, out var transactionStatus))
            {
                query = query.Where(t => t.Status == transactionStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t =>
                    (t.GatewayTransactionId != null && t.GatewayTransactionId.Contains(search)) ||
                    (t.GatewayReference != null && t.GatewayReference.Contains(search)) ||
                    (t.Order != null && t.Order.OrderNumber.Contains(search)) ||
                    (t.Notes != null && t.Notes.Contains(search)));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.AddDays(1);
                query = query.Where(t => t.CreatedAt < endDate);
            }

            // Order by most recent
            query = query.OrderByDescending(t => t.CreatedAt);

            // Get statistics
            var allTransactions = await _context.Transactions.ToListAsync();
            ViewBag.TotalPayments = allTransactions.Where(t => t.Type == TransactionType.Payment).Sum(t => t.Amount);
            ViewBag.TotalRefunds = allTransactions.Where(t => t.Type == TransactionType.Refund).Sum(t => t.Amount);
            ViewBag.TotalSellerPayouts = allTransactions.Where(t => t.Type == TransactionType.SellerPayout).Sum(t => t.Amount);
            ViewBag.TotalCommissions = allTransactions.Where(t => t.Type == TransactionType.Commission).Sum(t => t.Amount);
            ViewBag.TotalTransactions = allTransactions.Count;

            // Status counts
            ViewBag.PendingCount = allTransactions.Count(t => t.Status == TransactionStatus.Pending);
            ViewBag.CompletedCount = allTransactions.Count(t => t.Status == TransactionStatus.Completed);
            ViewBag.FailedCount = allTransactions.Count(t => t.Status == TransactionStatus.Failed);

            // Pagination
            var pageSize = 25;
            var totalCount = await query.CountAsync();
            var transactions = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentType = type;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.TransactionTypes = Enum.GetValues<TransactionType>().ToList();
            ViewBag.TransactionStatuses = Enum.GetValues<TransactionStatus>().ToList();

            return View(transactions);
        }

        // GET: Admin/Transaction/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Order)
                    .ThenInclude(o => o!.User)
                .Include(t => t.Order)
                    .ThenInclude(o => o!.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Include(t => t.Seller)
                    .ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                TempData["error"] = _localizer["TransactionNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Get related transactions (for the same order)
            if (transaction.OrderId.HasValue)
            {
                ViewBag.RelatedTransactions = await _context.Transactions
                    .Where(t => t.OrderId == transaction.OrderId && t.Id != transaction.Id)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }

            return View(transaction);
        }

        // GET: Admin/Transaction/RefundRequests - Enhanced Refund Management
        public async Task<IActionResult> RefundRequests(string? status, string? search, int page = 1)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.ReferredToAccounting == true ||
                           o.RefundStatus != RefundStatus.Pending ||
                           o.Status == OrderStatus.Returned)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RefundStatus>(status, out var refundStatus))
            {
                query = query.Where(o => o.RefundStatus == refundStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o =>
                    o.OrderNumber.Contains(search) ||
                    (o.User != null && (o.User.FirstName + " " + o.User.LastName).Contains(search)) ||
                    (o.User != null && o.User.Email != null && o.User.Email.Contains(search)) ||
                    (o.User != null && o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(search)));
            }

            query = query.OrderByDescending(o => o.RefundStatusChangedAt ?? o.ReferredToAccountingAt ?? o.UpdatedAt);

            // Get statistics
            var allRefundOrders = await _context.Orders
                .Where(o => o.ReferredToAccounting == true ||
                           o.RefundStatus != RefundStatus.Pending ||
                           o.Status == OrderStatus.Returned)
                .ToListAsync();

            ViewBag.TotalRefunds = allRefundOrders.Count;
            ViewBag.PendingRefunds = allRefundOrders.Count(o => o.RefundStatus == RefundStatus.Pending || o.RefundStatus == RefundStatus.Reviewing);
            ViewBag.ApprovedRefunds = allRefundOrders.Count(o => o.RefundStatus == RefundStatus.Approved);
            ViewBag.InProgressRefunds = allRefundOrders.Count(o => o.RefundStatus == RefundStatus.InProgress);
            ViewBag.CompletedRefunds = allRefundOrders.Count(o => o.RefundStatus == RefundStatus.Completed);
            ViewBag.RejectedRefunds = allRefundOrders.Count(o => o.RefundStatus == RefundStatus.Rejected);
            ViewBag.TotalRefundAmount = allRefundOrders.Where(o => o.RefundStatus == RefundStatus.Completed).Sum(o => o.RefundPaidAmount ?? 0);

            // Pagination
            var pageSize = 20;
            var totalCount = await query.CountAsync();
            var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;
            ViewBag.RefundStatuses = Enum.GetValues<RefundStatus>().ToList();

            return View(orders);
        }

        // GET: Admin/Transaction/CreateRefund - Manual Refund Creation
        public async Task<IActionResult> CreateRefund(int? orderId)
        {
            if (orderId.HasValue)
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order != null)
                {
                    ViewBag.Order = order;
                }
            }

            ViewBag.RefundMethods = Enum.GetValues<RefundMethod>().ToList();
            ViewBag.PaymentMethods = Enum.GetValues<PaymentMethod>().ToList();

            return View();
        }

        // POST: Admin/Transaction/CreateRefund
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRefund(
            int? orderId,
            string? orderNumber,
            string? customerEmail,
            decimal amount,
            string reason,
            RefundMethod refundMethod,
            string? refundAccountNumber,
            string? refundAccountHolderName,
            string? refundBankName,
            string? notes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                Order? order = null;

                // Find order by ID or order number
                if (orderId.HasValue)
                {
                    order = await _context.Orders
                        .Include(o => o.User)
                        .FirstOrDefaultAsync(o => o.Id == orderId);
                }
                else if (!string.IsNullOrEmpty(orderNumber))
                {
                    order = await _context.Orders
                        .Include(o => o.User)
                        .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
                }

                ApplicationUser? customer = null;

                if (order != null)
                {
                    customer = order.User;
                }
                else if (!string.IsNullOrEmpty(customerEmail))
                {
                    customer = await _userManager.FindByEmailAsync(customerEmail);
                }

                if (amount <= 0)
                {
                    TempData["error"] = _localizer["RefundAmountMustBeGreaterThan0"].Value;
                    return RedirectToAction(nameof(CreateRefund), new { orderId });
                }

                // Create transaction record
                var transaction = new Transaction
                {
                    OrderId = order?.Id,
                    UserId = customer?.Id,
                    Type = TransactionType.Refund,
                    Amount = amount,
                    PaymentMethod = order?.PaymentMethod ?? PaymentMethod.OnlinePayment,
                    Notes = $"Manual Refund - Reason: {reason}" + (string.IsNullOrEmpty(notes) ? "" : $" - {notes}"),
                    Status = TransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);

                // Update order refund status if exists
                if (order != null)
                {
                    order.RefundStatus = RefundStatus.Completed;
                    order.RefundStatusChangedAt = DateTime.UtcNow;
                    order.RefundPaymentMethod = refundMethod;
                    order.RefundPaidAmount = amount;
                    order.RefundPaymentDate = DateTime.UtcNow;
                    order.RefundTransactionId = $"MR-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                    order.RefundAdminNotes = $"Manual refund by {user?.Email}. Reason: {reason}";

                    if (!string.IsNullOrEmpty(refundAccountNumber))
                    {
                        order.RefundAccountNumber = refundAccountNumber;
                        order.RefundAccountHolderName = refundAccountHolderName;
                        order.RefundBankName = refundBankName;
                    }

                    // If customer used wallet, add to wallet balance
                    if (refundMethod == RefundMethod.WalletCredit && customer != null)
                    {
                        customer.WalletBalance += amount;
                    }
                }

                await _context.SaveChangesAsync();

                // Notify customer
                if (customer != null)
                {
                    await _notificationService.CreateNotificationAsync(new UserNotification
                    {
                        UserId = customer.Id,
                        Type = "refund",
                        Icon = "fa-money-bill-wave",
                        IconColor = "text-success",
                        Title = "Refund Processed",
                        Message = $"Your refund of ৳{amount:N0} has been processed" + (order != null ? $" for order #{order.OrderNumber}" : "") + ".",
                        Link = order != null ? $"/Orders/Details/{order.Id}" : "/Orders"
                    });

                    // Send email notification
                    if (!string.IsNullOrEmpty(customer.Email))
                    {
                        await _emailService.SendEmailAsync(
                            customer.Email,
                            "Refund Processed - Bangaliyana",
                            $@"<h2>Refund Processed</h2>
                            <p>Dear {customer.FirstName ?? "Customer"},</p>
                            <p>We have processed your refund of <strong>৳{amount:N0}</strong>" +
                            (order != null ? $" for order <strong>#{order.OrderNumber}</strong>" : "") + $@".</p>
                            <p><strong>Refund Method:</strong> {refundMethod}</p>
                            <p><strong>Reason:</strong> {reason}</p>
                            <p>If you have any questions, please contact our support team.</p>
                            <p>Thank you for shopping with us!</p>
                            <p>Best regards,<br/>Bangaliyana Team</p>");
                    }
                }

                _logger.LogInformation("Manual refund created. Amount: {Amount}, Order: {OrderId}, By: {User}",
                    amount, order?.Id, user?.Email);

                TempData["success"] = _localizer["RefundProcessedSuccessfully"].Value;
                return RedirectToAction(nameof(RefundRequests));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manual refund");
                TempData["error"] = _localizer["ErrorProcessingRefund"].Value;
                return RedirectToAction(nameof(CreateRefund), new { orderId });
            }
        }

        // GET: Admin/Transaction/ProcessRefund/5 - Process a specific refund request
        public async Task<IActionResult> ProcessRefund(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction(nameof(RefundRequests));
            }

            ViewBag.RefundMethods = Enum.GetValues<RefundMethod>().ToList();

            // Calculate maximum refundable amount
            ViewBag.MaxRefundAmount = order.TotalAmount;

            // Get previous refunds for this order
            var previousRefunds = await _context.Transactions
                .Where(t => t.OrderId == order.Id && t.Type == TransactionType.Refund)
                .ToListAsync();
            ViewBag.PreviousRefunds = previousRefunds;
            ViewBag.TotalRefunded = previousRefunds.Sum(t => t.Amount);

            return View(order);
        }

        // POST: Admin/Transaction/ProcessRefund
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(
            int id,
            decimal refundAmount,
            RefundMethod refundMethod,
            string? refundAccountNumber,
            string? refundAccountHolderName,
            string? refundBankName,
            string? transactionId,
            string? adminNotes,
            string action) // approve, reject, complete
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    TempData["error"] = _localizer["OrderNotFound"].Value;
                    return RedirectToAction(nameof(RefundRequests));
                }

                switch (action.ToLower())
                {
                    case "approve":
                        order.RefundStatus = RefundStatus.Approved;
                        order.ApprovedRefundAmount = refundAmount;
                        order.RefundAdminNotes = adminNotes;
                        TempData["success"] = _localizer["RefundApprovedSuccessfully"].Value;
                        break;

                    case "reject":
                        order.RefundStatus = RefundStatus.Rejected;
                        order.RefundAdminNotes = adminNotes;

                        // Notify customer
                        if (order.User != null)
                        {
                            await _notificationService.CreateNotificationAsync(new UserNotification
                            {
                                UserId = order.UserId,
                                Type = "refund",
                                Icon = "fa-times-circle",
                                IconColor = "text-danger",
                                Title = "Refund Request Rejected",
                                Message = $"Your refund request for order #{order.OrderNumber} has been rejected." + (!string.IsNullOrEmpty(adminNotes) ? $" Reason: {adminNotes}" : ""),
                                Link = $"/Orders/Details/{order.Id}"
                            });
                        }
                        TempData["success"] = _localizer["RefundRejectedSuccessfully"].Value;
                        break;

                    case "complete":
                        if (refundAmount <= 0)
                        {
                            TempData["error"] = _localizer["RefundAmountMustBeGreaterThan0"].Value;
                            return RedirectToAction(nameof(ProcessRefund), new { id });
                        }

                        // Create transaction
                        var transaction = new Transaction
                        {
                            OrderId = order.Id,
                            UserId = order.UserId,
                            Type = TransactionType.Refund,
                            Amount = refundAmount,
                            PaymentMethod = order.PaymentMethod,
                            GatewayTransactionId = transactionId,
                            Notes = $"Refund for order #{order.OrderNumber}" + (!string.IsNullOrEmpty(adminNotes) ? $" - {adminNotes}" : ""),
                            Status = TransactionStatus.Completed,
                            CreatedAt = DateTime.UtcNow,
                            CompletedAt = DateTime.UtcNow
                        };

                        _context.Transactions.Add(transaction);

                        // Update order
                        order.RefundStatus = RefundStatus.Completed;
                        order.RefundPaymentMethod = refundMethod;
                        order.RefundPaidAmount = refundAmount;
                        order.RefundPaymentDate = DateTime.UtcNow;
                        order.RefundTransactionId = transactionId ?? $"RF-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                        order.RefundAdminNotes = adminNotes;

                        if (!string.IsNullOrEmpty(refundAccountNumber))
                        {
                            order.RefundAccountNumber = refundAccountNumber;
                            order.RefundAccountHolderName = refundAccountHolderName;
                            order.RefundBankName = refundBankName;
                        }

                        // Wallet credit if applicable
                        if (refundMethod == RefundMethod.WalletCredit && order.User != null)
                        {
                            order.User.WalletBalance += refundAmount;
                        }

                        // Update payment status
                        var totalRefunded = await _context.Transactions
                            .Where(t => t.OrderId == order.Id && t.Type == TransactionType.Refund)
                            .SumAsync(t => t.Amount) + refundAmount;

                        if (totalRefunded >= order.TotalAmount)
                        {
                            order.PaymentStatus = PaymentStatus.Refunded;
                        }
                        else if (totalRefunded > 0)
                        {
                            order.PaymentStatus = PaymentStatus.PartiallyRefunded;
                        }

                        // Notify customer
                        if (order.User != null)
                        {
                            await _notificationService.CreateNotificationAsync(new UserNotification
                            {
                                UserId = order.UserId,
                                Type = "refund",
                                Icon = "fa-money-bill-wave",
                                IconColor = "text-success",
                                Title = "Refund Completed",
                                Message = $"Your refund of ৳{refundAmount:N0} for order #{order.OrderNumber} has been completed via {refundMethod}.",
                                Link = $"/Orders/Details/{order.Id}"
                            });

                            // Send email
                            if (!string.IsNullOrEmpty(order.User.Email))
                            {
                                await _emailService.SendEmailAsync(
                                    order.User.Email,
                                    "Refund Completed - Bangaliyana",
                                    $@"<h2>Refund Completed</h2>
                                    <p>Dear {order.User.FirstName ?? "Customer"},</p>
                                    <p>We have successfully processed your refund.</p>
                                    <p><strong>Order Number:</strong> #{order.OrderNumber}</p>
                                    <p><strong>Refund Amount:</strong> ৳{refundAmount:N0}</p>
                                    <p><strong>Refund Method:</strong> {refundMethod}</p>
                                    <p><strong>Transaction ID:</strong> {order.RefundTransactionId}</p>
                                    <p>If you have any questions, please contact our support team.</p>
                                    <p>Thank you for shopping with us!</p>
                                    <p>Best regards,<br/>Bangaliyana Team</p>");
                            }
                        }

                        _logger.LogInformation("Refund completed. Order: {OrderNumber}, Amount: {Amount}, Method: {Method}, By: {User}",
                            order.OrderNumber, refundAmount, refundMethod, user?.Email);

                        TempData["success"] = _localizer["RefundCompletedSuccessfully"].Value;
                        break;

                    default:
                        TempData["error"] = _localizer["InvalidAction"].Value;
                        return RedirectToAction(nameof(ProcessRefund), new { id });
                }

                order.RefundStatusChangedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(RefundRequests));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for order {OrderId}", id);
                TempData["error"] = _localizer["ErrorProcessingRefund"].Value;
                return RedirectToAction(nameof(ProcessRefund), new { id });
            }
        }

        // POST: Admin/Transaction/UpdateRefundStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRefundStatus(int id, RefundStatus status, string? notes)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }

            order.RefundStatus = status;
            order.RefundStatusChangedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(notes))
            {
                order.RefundAdminNotes = notes;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Refund status updated to {status}." });
        }

        // GET: Admin/Transaction/Export - Export transactions to CSV
        public async Task<IActionResult> Export(
            string? type,
            string? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _context.Transactions
                .Include(t => t.Order)
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

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.AddDays(1);
                query = query.Where(t => t.CreatedAt < endDate);
            }

            var transactions = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            // Create CSV content
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("ID,Date,Type,Order Number,Amount,Payment Method,Status,Transaction ID,Notes");

            foreach (var t in transactions)
            {
                csv.AppendLine($"{t.Id},{t.CreatedAt:yyyy-MM-dd HH:mm},{t.Type},{t.Order?.OrderNumber ?? "-"},{t.Amount:N2},{t.PaymentMethod},{t.Status},{t.GatewayTransactionId ?? "-"},\"{t.Notes?.Replace("\"", "\"\"") ?? ""}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"transactions-{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
