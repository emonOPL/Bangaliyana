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
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<OrderController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRewardService _rewardService;
        private readonly INotificationService _notificationService;
        private readonly ISellerMonthlyReportService _monthlyReportService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public OrderController(
            ApplicationDbContext db,
            ILogger<OrderController> logger,
            UserManager<ApplicationUser> userManager,
            IRewardService rewardService,
            INotificationService notificationService,
            ISellerMonthlyReportService monthlyReportService,
            IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _logger = logger;
            _userManager = userManager;
            _rewardService = rewardService;
            _notificationService = notificationService;
            _monthlyReportService = monthlyReportService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(int? page, string? status, string? orderId, DateTime? orderDate)
        {
            var query = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var statusEnum))
            {
                query = query.Where(o => o.Status == statusEnum);
            }

            if (!string.IsNullOrEmpty(orderId) && int.TryParse(orderId, out var orderIdInt))
            {
                query = query.Where(o => o.Id == orderIdInt);
            }

            if (orderDate.HasValue)
            {
                query = query.Where(o => o.OrderDate.Date == orderDate.Value.Date);
            }

            query = query.OrderByDescending(o => o.OrderDate);

            var orders = await query.ToListAsync();
            var pagedOrders = orders.ToPagedList(page ?? 1, 10);

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentOrderId = orderId;
            ViewBag.CurrentOrderDate = orderDate?.ToString("yyyy-MM-dd");
            ViewBag.StatusList = Enum.GetValues<OrderStatus>().Select(s => s.ToString()).ToList();
            ViewBag.IsAdmin = true;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Order";

            return View("~/Areas/Admin/Views/Orders/Index.cshtml", pagedOrders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Coupon)
                .Include(o => o.AppliedReward)
                .Include(o => o.AppliedFreeShippingReward)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction("Index");
            }

            ViewBag.IsAdmin = true;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Order";
            return View("~/Areas/Admin/Views/Orders/Details.cshtml", order);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction("Index");
            }

            ViewBag.StatusList = Enum.GetValues<OrderStatus>().ToList();
            ViewBag.IsAdmin = true;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Order";
            return View("~/Areas/Admin/Views/Orders/Edit.cshtml", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (!ModelState.IsValid)
            {
                var existingOrder = await _db.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                if (existingOrder == null)
                {
                    TempData["error"] = _localizer["OrderNotFound"].Value;
                    return RedirectToAction("Index");
                }

                ViewBag.StatusList = Enum.GetValues<OrderStatus>().ToList();
                ViewBag.IsAdmin = true;
                ViewBag.AreaName = "Moderator";
                ViewBag.ControllerName = "Order";
                return View("~/Areas/Admin/Views/Orders/Edit.cshtml", existingOrder);
            }

            try
            {
                var existingOrder = await _db.Orders.FirstOrDefaultAsync(o => o.Id == order.Id);
                if (existingOrder == null)
                {
                    TempData["error"] = _localizer["OrderNotFound"].Value;
                    return RedirectToAction("Index");
                }

                var previousStatus = existingOrder.Status;

                existingOrder.Status = order.Status;
                existingOrder.CustomerName = order.CustomerName;
                existingOrder.Email = order.Email;
                existingOrder.Phone = order.Phone;
                existingOrder.Address = order.Address;
                existingOrder.PostalCode = order.PostalCode;
                existingOrder.DeliveryCharge = order.DeliveryCharge;
                existingOrder.TotalAmount = order.TotalAmount;
                existingOrder.IsPaymentReceived = order.IsPaymentReceived;

                // Auto-restrict COD if COD order is being returned
                if (order.Status == OrderStatus.Returned && previousStatus != OrderStatus.Returned)
                {
                    if (existingOrder.PaymentMethod == PaymentMethod.CashOnDelivery &&
                        !string.IsNullOrEmpty(existingOrder.UserId) &&
                        !existingOrder.CODPenaltyApplied)
                    {
                        var user = await _db.Users.FindAsync(existingOrder.UserId);
                        if (user != null)
                        {
                            user.IsCODAllowed = false;
                            user.CODRestrictionReason = $"Order #{existingOrder.OrderNumber ?? existingOrder.Id.ToString()} returned on {DateTime.UtcNow:yyyy-MM-dd}";
                            user.CODRestrictedAt = DateTime.UtcNow;
                            user.ReturnedOrdersCount++;
                            existingOrder.CODPenaltyApplied = true;
                            existingOrder.ReturnedAt = DateTime.UtcNow;
                            existingOrder.CustomerRefusedDelivery = true;
                        }
                    }
                }

                await _db.SaveChangesAsync();

                TempData["success"] = _localizer["OrderUpdatedSuccessfully"].Value;
                return RedirectToAction("Details", new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", order.Id);
                TempData["error"] = _localizer["ErrorUpdatingOrder"].Value;
                ViewBag.StatusList = Enum.GetValues<OrderStatus>().ToList();
                ViewBag.AreaName = "Moderator";
                ViewBag.ControllerName = "Order";
                return View("~/Areas/Admin/Views/Orders/Edit.cshtml", order);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                var previousStatus = order.Status;
                order.Status = status;

                // Award delivery points and increment SoldCount when status changes to Delivered
                if (status == OrderStatus.Delivered && previousStatus != OrderStatus.Delivered)
                {
                    order.DeliveredAt = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(order.UserId) && !order.DeliveryPointsAwarded)
                    {
                        await _rewardService.AwardOrderDeliveryPointsAsync(order.Id);
                    }

                    // Increment SoldCount for each product in the order
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.SoldCount += item.Quantity;
                        }
                    }
                    _logger.LogInformation($"SoldCount incremented for order #{order.Id} (Delivered)");

                    // Auto-mark COD payment as received when delivered
                    if (order.PaymentMethod == PaymentMethod.CashOnDelivery && !order.IsPaymentReceived)
                    {
                        order.IsPaymentReceived = true;
                        order.IsPaymentVerified = true;
                        order.PaymentVerifiedAt = DateTime.UtcNow;
                        order.PaymentVerifiedBy = "System (Auto on Delivery)";
                        order.PaymentVerificationNotes = "Auto-verified: COD payment collected on delivery";
                        order.PaymentStatus = PaymentStatus.Completed;
                        _logger.LogInformation($"COD payment auto-marked as received for order #{order.Id}");
                    }

                    // Add order items to seller monthly reports (payment must be received)
                    if (order.IsPaymentReceived)
                    {
                        foreach (var item in order.OrderItems.Where(oi => oi.SellerId != null))
                        {
                            try
                            {
                                await _monthlyReportService.AddOrderItemToReportAsync(item.Id, order.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error adding order item {OrderItemId} to monthly report", item.Id);
                            }
                        }
                    }
                }

                // Auto-restrict COD if COD order is being returned
                if (status == OrderStatus.Returned && previousStatus != OrderStatus.Returned)
                {
                    // Decrement SoldCount if the order was previously delivered
                    if (previousStatus == OrderStatus.Delivered)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            if (item.Product != null)
                            {
                                item.Product.SoldCount -= item.Quantity;
                                if (item.Product.SoldCount < 0) item.Product.SoldCount = 0;
                            }
                        }
                        _logger.LogInformation($"SoldCount decremented for order #{order.Id} (Returned after Delivery)");
                    }

                    if (order.PaymentMethod == PaymentMethod.CashOnDelivery &&
                        !string.IsNullOrEmpty(order.UserId) &&
                        !order.CODPenaltyApplied)
                    {
                        var user = await _db.Users.FindAsync(order.UserId);
                        if (user != null)
                        {
                            user.IsCODAllowed = false;
                            user.CODRestrictionReason = $"Order #{order.OrderNumber ?? order.Id.ToString()} returned on {DateTime.UtcNow:yyyy-MM-dd}";
                            user.CODRestrictedAt = DateTime.UtcNow;
                            user.ReturnedOrdersCount++;
                            order.CODPenaltyApplied = true;
                            order.ReturnedAt = DateTime.UtcNow;
                            order.CustomerRefusedDelivery = true;
                        }
                    }

                    // Process return deductions in seller monthly reports
                    foreach (var item in order.OrderItems.Where(oi => oi.SellerId != null))
                    {
                        try
                        {
                            await _monthlyReportService.ProcessReturnDeductionAsync(item.Id, order.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing return deduction for order item {OrderItemId}", item.Id);
                        }
                    }
                }

                // Process refund deductions in seller monthly reports
                if (status == OrderStatus.Refunded && previousStatus != OrderStatus.Refunded)
                {
                    foreach (var item in order.OrderItems.Where(oi => oi.SellerId != null))
                    {
                        try
                        {
                            await _monthlyReportService.ProcessRefundDeductionAsync(item.Id, order.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing refund deduction for order item {OrderItemId}", item.Id);
                        }
                    }
                }

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = $"Order status updated to {status}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
                return Json(new { success = false, message = "An error occurred while updating the order status." });
            }
        }

        // ==================== PAYMENT VERIFICATION ====================

        /// <summary>
        /// Verify MFS payment (bKash, Nagad, Rocket, Upay) and mark order as paid
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPayment(int orderId, string? notes)
        {
            try
            {
                var order = await _db.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                if (order.IsPaymentReceived)
                {
                    return Json(new { success = false, message = "Payment has already been marked as received." });
                }

                // Get current user for verification tracking
                var currentUser = await _userManager.GetUserAsync(User);
                var verifiedBy = currentUser?.FullName ?? currentUser?.Email ?? User.Identity?.Name ?? "Moderator";

                // Update payment status
                order.IsPaymentReceived = true;
                order.IsPaymentVerified = true;
                order.PaymentVerifiedAt = DateTime.UtcNow;
                order.PaymentVerifiedBy = verifiedBy;
                order.PaymentVerificationNotes = notes;
                order.PaymentStatus = PaymentStatus.Completed;
                order.UpdatedAt = DateTime.UtcNow;

                // If order is still Pending, move it to Confirmed
                if (order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Confirmed;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Payment verified for order #{order.Id} by {verifiedBy}");

                return Json(new { success = true, message = $"Payment verified successfully for Order #{order.OrderNumber ?? order.Id.ToString()}!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment for order {OrderId}", orderId);
                return Json(new { success = false, message = "An error occurred while verifying payment." });
            }
        }

        /// <summary>
        /// Confirm COD payment collection
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCODPayment(int orderId, string? collectorName, string? notes)
        {
            try
            {
                var order = await _db.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                if (order.PaymentMethod != PaymentMethod.CashOnDelivery)
                {
                    return Json(new { success = false, message = "This action is only for COD orders." });
                }

                if (order.IsPaymentReceived)
                {
                    return Json(new { success = false, message = "Payment has already been marked as received." });
                }

                // Get current user for verification tracking
                var currentUser = await _userManager.GetUserAsync(User);
                var verifiedBy = currentUser?.FullName ?? currentUser?.Email ?? User.Identity?.Name ?? "Moderator";

                // Build notes
                var verificationNotes = "COD payment confirmed manually";
                if (!string.IsNullOrEmpty(collectorName))
                {
                    verificationNotes += $". Collected by: {collectorName}";
                }
                if (!string.IsNullOrEmpty(notes))
                {
                    verificationNotes += $". Notes: {notes}";
                }

                // Update payment status
                order.IsPaymentReceived = true;
                order.IsPaymentVerified = true;
                order.PaymentVerifiedAt = DateTime.UtcNow;
                order.PaymentVerifiedBy = verifiedBy;
                order.PaymentVerificationNotes = verificationNotes;
                order.PaymentStatus = PaymentStatus.Completed;
                order.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation($"COD payment confirmed for order #{order.Id} by {verifiedBy}");

                return Json(new { success = true, message = $"Cash payment confirmed for Order #{order.OrderNumber ?? order.Id.ToString()}!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming COD payment for order {OrderId}", orderId);
                return Json(new { success = false, message = "An error occurred while confirming payment." });
            }
        }

        /// <summary>
        /// Refuse/Undo COD payment (mark as not received)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefuseCODPayment(int orderId, string reason, string? notes, bool applyPenalty)
        {
            try
            {
                var order = await _db.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                if (order.PaymentMethod != PaymentMethod.CashOnDelivery)
                {
                    return Json(new { success = false, message = "This action is only for COD orders." });
                }

                // Get current user
                var currentUser = await _userManager.GetUserAsync(User);
                var processedBy = currentUser?.FullName ?? currentUser?.Email ?? User.Identity?.Name ?? "Moderator";

                // Update payment status
                order.IsPaymentReceived = false;
                order.IsPaymentVerified = false;
                order.PaymentStatus = PaymentStatus.Failed;
                order.PaymentVerificationNotes = $"Payment refused/undone. Reason: {reason}" +
                    (!string.IsNullOrEmpty(notes) ? $". Notes: {notes}" : "") +
                    $". Processed by: {processedBy} at {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
                order.UpdatedAt = DateTime.UtcNow;

                // Apply penalty if requested
                if (applyPenalty && !string.IsNullOrEmpty(order.UserId) && !order.CODPenaltyApplied)
                {
                    var user = await _db.Users.FindAsync(order.UserId);
                    if (user != null)
                    {
                        user.IsCODAllowed = false;
                        user.CODRestrictionReason = $"Order #{order.OrderNumber ?? order.Id.ToString()} - {reason}";
                        user.CODRestrictedAt = DateTime.UtcNow;
                        user.ReturnedOrdersCount++;
                        order.CODPenaltyApplied = true;
                        order.CustomerRefusedDelivery = true;
                        _logger.LogInformation($"COD penalty applied to user {user.Email} for order #{order.Id}");
                    }
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation($"COD payment refused/undone for order #{order.Id} by {processedBy}. Reason: {reason}");

                var message = $"Payment marked as not received for Order #{order.OrderNumber ?? order.Id.ToString()}";
                if (applyPenalty)
                {
                    message += ". COD restriction applied to customer.";
                }

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refusing COD payment for order {OrderId}", orderId);
                return Json(new { success = false, message = "An error occurred while processing." });
            }
        }

        // ==================== RETURN/REFUND MANAGEMENT ====================

        public async Task<IActionResult> Returns(int? page, string? status)
        {
            var query = _db.Orders
                .Where(o => o.ReturnReason != null)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "pending":
                        query = query.Where(o => o.RefundStatus == RefundStatus.Pending || o.RefundStatus == RefundStatus.Reviewing || o.RefundStatus == RefundStatus.InProgress);
                        break;
                    case "approved":
                        query = query.Where(o => o.RefundStatus == RefundStatus.Approved);
                        break;
                    case "rejected":
                        query = query.Where(o => o.RefundStatus == RefundStatus.Rejected);
                        break;
                }
            }

            query = query.OrderByDescending(o => o.ReturnedAt ?? o.UpdatedAt);

            var returns = await query.ToListAsync();
            var pagedReturns = returns.ToPagedList(page ?? 1, 15);

            var allReturns = await _db.Orders.Where(o => o.ReturnReason != null).ToListAsync();
            ViewBag.TotalReturns = allReturns.Count;
            ViewBag.PendingReturns = allReturns.Count(o => o.RefundStatus == RefundStatus.Pending || o.RefundStatus == RefundStatus.Reviewing || o.RefundStatus == RefundStatus.InProgress);
            ViewBag.ProcessedReturns = allReturns.Count(o => o.RefundStatus == RefundStatus.Approved);
            ViewBag.CurrentStatus = status;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Order";

            return View("~/Areas/Admin/Views/Orders/Returns.cshtml", pagedReturns);
        }

        public async Task<IActionResult> ReviewReturn(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Coupon)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction("Returns");
            }

            if (string.IsNullOrEmpty(order.ReturnReason))
            {
                TempData["error"] = _localizer["NoReturnRequest"].Value;
                return RedirectToAction("Details", new { id });
            }

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var managerUsers = await _userManager.GetUsersInRoleAsync("Manager");
            var accountingUsers = adminUsers.Concat(managerUsers)
                .Select(u => new { u.Id, Name = $"{u.FirstName} {u.LastName}".Trim(), u.Email })
                .Where(u => !string.IsNullOrEmpty(u.Name))
                .Distinct()
                .OrderBy(u => u.Name)
                .ToList();
            ViewBag.AccountingUsers = accountingUsers;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Order";

            return View("~/Areas/Admin/Views/Orders/ReviewReturn.cshtml", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReturn(int id, string? adminNotes)
        {
            try
            {
                var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                if (string.IsNullOrEmpty(order.ReturnReason))
                {
                    return Json(new { success = false, message = "No return request found for this order." });
                }

                order.Status = OrderStatus.Returned;
                order.AdminNotes = string.IsNullOrEmpty(adminNotes)
                    ? $"Return approved on {DateTime.UtcNow:yyyy-MM-dd HH:mm}"
                    : $"Return approved: {adminNotes}";
                order.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Return request approved. Please process the refund." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving return for order {OrderId}", id);
                return Json(new { success = false, message = "An error occurred while approving the return." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReturn(int id, string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                return Json(new { success = false, message = "Rejection reason is required." });
            }

            try
            {
                var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                order.Status = OrderStatus.Delivered;
                order.AdminNotes = $"Return rejected on {DateTime.UtcNow:yyyy-MM-dd}: {rejectionReason}";
                order.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Return request rejected." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting return for order {OrderId}", id);
                return Json(new { success = false, message = "An error occurred while rejecting the return." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(int id, string refundMethod, decimal refundAmount, string? transactionId, string? notes)
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                if (order.PaymentStatus == PaymentStatus.Refunded)
                {
                    return Json(new { success = false, message = "This order has already been refunded." });
                }

                if (refundAmount <= 0 || refundAmount > order.TotalAmount)
                {
                    return Json(new { success = false, message = "Invalid refund amount." });
                }

                if (refundMethod == "WalletCredit" && order.User != null)
                {
                    order.User.WalletBalance += Math.Round(refundAmount);

                    var walletTransaction = new Transaction
                    {
                        OrderId = order.Id,
                        UserId = order.UserId,
                        Type = TransactionType.Refund,
                        Amount = refundAmount,
                        Notes = $"Refund for Order #{order.OrderNumber ?? order.Id.ToString()} - Credited to Wallet",
                        Status = TransactionStatus.Completed,
                        CreatedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    };
                    _db.Transactions.Add(walletTransaction);
                }
                else
                {
                    var refundTransaction = new Transaction
                    {
                        OrderId = order.Id,
                        UserId = order.UserId,
                        Type = TransactionType.Refund,
                        Amount = refundAmount,
                        Notes = $"Refund for Order #{order.OrderNumber ?? order.Id.ToString()} via {refundMethod}",
                        GatewayTransactionId = transactionId,
                        Status = TransactionStatus.Completed,
                        CreatedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    };
                    _db.Transactions.Add(refundTransaction);
                }

                order.Status = OrderStatus.Refunded;
                order.PaymentStatus = refundAmount >= order.TotalAmount
                    ? PaymentStatus.Refunded
                    : PaymentStatus.PartiallyRefunded;

                order.AdminNotes = string.IsNullOrEmpty(notes)
                    ? $"{order.AdminNotes}\nRefund of ৳{refundAmount:N2} processed via {refundMethod} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}"
                    : $"{order.AdminNotes}\nRefund: {notes}";

                order.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var message = refundMethod == "WalletCredit" && order.User != null
                    ? $"Refund of ৳{refundAmount:N2} credited to customer's wallet successfully."
                    : $"Refund of ৳{refundAmount:N2} recorded via {refundMethod}.";

                return Json(new { success = true, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for order {OrderId}", id);
                return Json(new { success = false, message = "An error occurred while processing the refund." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRefundStatus(int id, string newStatus, string? notes, decimal? approvedAmount)
        {
            if (!Enum.TryParse<RefundStatus>(newStatus, out var parsedStatus))
            {
                return Json(new { success = false, message = "Invalid refund status." });
            }

            try
            {
                var order = await _db.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                order.RefundStatus = parsedStatus;
                order.RefundStatusChangedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(notes))
                {
                    order.RefundAdminNotes = string.IsNullOrEmpty(order.RefundAdminNotes)
                        ? $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Status: {parsedStatus} - {notes}"
                        : $"{order.RefundAdminNotes}\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Status: {parsedStatus} - {notes}";
                }

                if (parsedStatus == RefundStatus.Approved && approvedAmount.HasValue)
                {
                    order.ApprovedRefundAmount = approvedAmount.Value;

                    if (order.PreferredRefundMethod == RefundMethod.WalletCredit && order.User != null)
                    {
                        order.User.WalletBalance += Math.Round(approvedAmount.Value);

                        var walletTransaction = new Transaction
                        {
                            OrderId = order.Id,
                            UserId = order.UserId,
                            Type = TransactionType.Refund,
                            Amount = approvedAmount.Value,
                            Notes = $"Refund for Order #{order.OrderNumber ?? order.Id.ToString()} - Credited to Wallet (Auto-approved)",
                            Status = TransactionStatus.Completed,
                            CreatedAt = DateTime.UtcNow,
                            CompletedAt = DateTime.UtcNow
                        };
                        _db.Transactions.Add(walletTransaction);

                        order.Status = OrderStatus.Refunded;
                        order.PaymentStatus = approvedAmount.Value >= order.TotalAmount
                            ? PaymentStatus.Refunded
                            : PaymentStatus.PartiallyRefunded;
                    }
                }

                if (parsedStatus == RefundStatus.Rejected)
                {
                    order.Status = OrderStatus.Delivered;
                }

                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var statusMessage = parsedStatus switch
                {
                    RefundStatus.Approved when order.PreferredRefundMethod == RefundMethod.WalletCredit && order.User != null
                        => $"Refund approved and ৳{approvedAmount:N2} credited to customer's wallet.",
                    RefundStatus.Approved => $"Refund approved for ৳{approvedAmount:N2}. Please process the refund manually.",
                    RefundStatus.Rejected => "Refund request rejected.",
                    _ => $"Refund status updated to {parsedStatus}."
                };

                return Json(new { success = true, message = statusMessage, newStatus = parsedStatus.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating refund status for order {OrderId}", id);
                return Json(new { success = false, message = "An error occurred while updating the refund status." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReferToAccounting(int id, string accountingPersonName, string? notes)
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                var refundableAmount = order.SubTotal > 0 ? order.SubTotal : order.OrderItems.Sum(i => i.TotalPrice);

                order.ReferredToAccounting = true;
                order.ReferredToAccountingAt = DateTime.UtcNow;
                order.ReferredToAccountingName = accountingPersonName;

                if (!string.IsNullOrEmpty(notes))
                {
                    order.RefundAdminNotes = string.IsNullOrEmpty(order.RefundAdminNotes)
                        ? $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Referred to Accounting ({accountingPersonName}) - {notes}"
                        : $"{order.RefundAdminNotes}\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Referred to Accounting ({accountingPersonName}) - {notes}";
                }
                else
                {
                    order.RefundAdminNotes = string.IsNullOrEmpty(order.RefundAdminNotes)
                        ? $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Referred to Accounting ({accountingPersonName})"
                        : $"{order.RefundAdminNotes}\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Referred to Accounting ({accountingPersonName})";
                }

                if (order.RefundStatus == RefundStatus.Pending)
                {
                    order.RefundStatus = RefundStatus.Reviewing;
                    order.RefundStatusChangedAt = DateTime.UtcNow;
                }

                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var referredUser = await _db.Users
                    .FirstOrDefaultAsync(u => (u.FirstName + " " + u.LastName).Trim() == accountingPersonName);

                if (referredUser != null)
                {
                    var currentUserName = User.Identity?.Name ?? "Moderator";
                    var notification = new UserNotification
                    {
                        UserId = referredUser.Id,
                        Type = "order",
                        Icon = "fa-file-invoice-dollar",
                        IconColor = "text-warning",
                        Title = "Refund Request Referred to You",
                        Message = $"Order #{order.OrderNumber ?? order.Id.ToString()} refund request (৳{Math.Round(refundableAmount):N0}) has been referred to you by {currentUserName}." +
                                  (!string.IsNullOrEmpty(notes) ? $" Note: {notes}" : ""),
                        Link = $"/Moderator/Order/ReviewReturn/{order.Id}",
                        OrderId = order.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    await _notificationService.CreateNotificationAsync(notification);
                }

                return Json(new { success = true, message = $"Refund request referred to {accountingPersonName}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error referring order {OrderId} to accounting", id);
                return Json(new { success = false, message = "An error occurred while referring to accounting." });
            }
        }

        // ==================== ORDER ITEM MANAGEMENT ====================

        /// <summary>
        /// Search products for adding to order
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            var products = await _db.Products
                .Where(p => p.Name.Contains(term) || (p.SKU != null && p.SKU.Contains(term)))
                .Take(10)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    sku = p.SKU ?? "",
                    price = p.DiscountPrice ?? p.Price,
                    stock = p.Stock,
                    image = p.ImageUrl ?? "images/products/noimage.jpg"
                })
                .ToListAsync();

            return Json(products);
        }

        /// <summary>
        /// Add a product to an existing order
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrderItem(int orderId, int productId, int quantity, decimal? customPrice = null)
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                var product = await _db.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Quantity must be greater than 0." });
                }

                // Check if product already exists in order
                var existingItem = order.OrderItems.FirstOrDefault(oi => oi.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    existingItem.TotalPrice = existingItem.UnitPrice * existingItem.Quantity;
                }
                else
                {
                    var unitPrice = customPrice ?? (product.DiscountPrice ?? product.Price);
                    var orderItem = new OrderItem
                    {
                        OrderId = orderId,
                        ProductId = productId,
                        ProductName = product.Name,
                        UnitPrice = unitPrice,
                        Quantity = quantity,
                        TotalPrice = unitPrice * quantity
                    };
                    _db.OrderItems.Add(orderItem);
                }

                await _db.SaveChangesAsync();
                await RecalculateOrderTotals(orderId);

                // If order is already confirmed, adjust stock
                if (order.Status == OrderStatus.Confirmed || order.Status == OrderStatus.Paid || order.Status == OrderStatus.Shipped)
                {
                    product.Stock -= quantity;
                    if (product.Stock < 0) product.Stock = 0;
                    if (product.Stock <= 0)
                    {
                        product.Status = ProductStatus.OutOfStock;
                        product.IsAvailable = false;
                    }
                    await _db.SaveChangesAsync();
                }

                _logger.LogInformation($"Moderator added {quantity}x {product.Name} to order #{orderId}");
                return Json(new { success = true, message = $"Added {quantity}x {product.Name} to order." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to order {OrderId}", orderId);
                return Json(new { success = false, message = "An error occurred while adding the item." });
            }
        }

        /// <summary>
        /// Update quantity of an order item
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderItemQuantity(int orderItemId, int newQuantity)
        {
            try
            {
                var orderItem = await _db.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

                if (orderItem == null)
                {
                    return Json(new { success = false, message = "Order item not found." });
                }

                if (newQuantity <= 0)
                {
                    return Json(new { success = false, message = "Quantity must be greater than 0. Use remove to delete the item." });
                }

                var oldQuantity = orderItem.Quantity;
                var quantityDiff = newQuantity - oldQuantity;

                // If order is confirmed, adjust stock
                if (orderItem.Order.Status == OrderStatus.Confirmed ||
                    orderItem.Order.Status == OrderStatus.Paid ||
                    orderItem.Order.Status == OrderStatus.Shipped)
                {
                    if (orderItem.Product != null)
                    {
                        orderItem.Product.Stock -= quantityDiff;
                        if (orderItem.Product.Stock < 0) orderItem.Product.Stock = 0;

                        if (orderItem.Product.Stock <= 0)
                        {
                            orderItem.Product.Status = ProductStatus.OutOfStock;
                            orderItem.Product.IsAvailable = false;
                        }
                        else if (orderItem.Product.Status == ProductStatus.OutOfStock)
                        {
                            orderItem.Product.Status = ProductStatus.Active;
                            orderItem.Product.IsAvailable = true;
                        }
                    }
                }

                orderItem.Quantity = newQuantity;
                orderItem.TotalPrice = orderItem.UnitPrice * newQuantity;

                await _db.SaveChangesAsync();
                await RecalculateOrderTotals(orderItem.OrderId);

                _logger.LogInformation($"Moderator updated order item #{orderItemId} quantity from {oldQuantity} to {newQuantity}");
                return Json(new { success = true, message = $"Quantity updated from {oldQuantity} to {newQuantity}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order item {OrderItemId}", orderItemId);
                return Json(new { success = false, message = "An error occurred while updating the quantity." });
            }
        }

        /// <summary>
        /// Remove an item from an order
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveOrderItem(int orderItemId)
        {
            try
            {
                var orderItem = await _db.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

                if (orderItem == null)
                {
                    return Json(new { success = false, message = "Order item not found." });
                }

                var orderId = orderItem.OrderId;
                var productName = orderItem.ProductName;
                var quantity = orderItem.Quantity;

                // If order is confirmed, restore stock
                if (orderItem.Order.Status == OrderStatus.Confirmed ||
                    orderItem.Order.Status == OrderStatus.Paid ||
                    orderItem.Order.Status == OrderStatus.Shipped)
                {
                    if (orderItem.Product != null)
                    {
                        orderItem.Product.Stock += quantity;
                        if (orderItem.Product.Stock > 0 && orderItem.Product.Status == ProductStatus.OutOfStock)
                        {
                            orderItem.Product.Status = ProductStatus.Active;
                            orderItem.Product.IsAvailable = true;
                        }
                    }
                }

                _db.OrderItems.Remove(orderItem);
                await _db.SaveChangesAsync();
                await RecalculateOrderTotals(orderId);

                _logger.LogInformation($"Moderator removed {productName} from order #{orderId}");
                return Json(new { success = true, message = $"Removed {productName} from order." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing order item {OrderItemId}", orderItemId);
                return Json(new { success = false, message = "An error occurred while removing the item." });
            }
        }

        /// <summary>
        /// Update order item unit price
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderItemPrice(int orderItemId, decimal newPrice)
        {
            try
            {
                var orderItem = await _db.OrderItems.FindAsync(orderItemId);
                if (orderItem == null)
                {
                    return Json(new { success = false, message = "Order item not found." });
                }

                if (newPrice < 0)
                {
                    return Json(new { success = false, message = "Price cannot be negative." });
                }

                orderItem.UnitPrice = newPrice;
                orderItem.TotalPrice = newPrice * orderItem.Quantity;

                await _db.SaveChangesAsync();
                await RecalculateOrderTotals(orderItem.OrderId);

                _logger.LogInformation($"Moderator updated order item #{orderItemId} price to {newPrice}");
                return Json(new { success = true, message = $"Price updated to ৳{newPrice:N2}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order item price {OrderItemId}", orderItemId);
                return Json(new { success = false, message = "An error occurred while updating the price." });
            }
        }

        /// <summary>
        /// Recalculate order subtotal and total
        /// </summary>
        private async Task RecalculateOrderTotals(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                order.SubTotal = order.OrderItems.Sum(oi => oi.TotalPrice);
                order.TotalAmount = order.SubTotal + order.DeliveryCharge -
                                   order.RewardDiscountAmount -
                                   order.WalletAmountUsed -
                                   order.PremiumDiscountAmount;
                if (order.TotalAmount < 0) order.TotalAmount = 0;
                await _db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Get order items for AJAX refresh
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOrderItems(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }

            var items = order.OrderItems.Select(oi => new
            {
                id = oi.Id,
                productId = oi.ProductId,
                productName = oi.ProductName,
                unitPrice = oi.UnitPrice,
                quantity = oi.Quantity,
                totalPrice = oi.TotalPrice,
                productImage = oi.Product?.ImageUrl ?? "images/products/noimage.jpg",
                productStock = oi.Product?.Stock ?? 0
            }).ToList();

            return Json(new
            {
                success = true,
                items,
                subTotal = order.SubTotal,
                deliveryCharge = order.DeliveryCharge,
                totalAmount = order.TotalAmount
            });
        }
    }
}
