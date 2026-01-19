using System.Text.Json;
using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,Moderator,Seller")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<OrdersController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRewardService _rewardService;
        private readonly INotificationService _notificationService;
        private readonly ISellerMonthlyReportService _monthlyReportService;
        private readonly IStockAlertService _stockAlertService;

        public OrdersController(
            ApplicationDbContext db,
            ILogger<OrdersController> logger,
            UserManager<ApplicationUser> userManager,
            IRewardService rewardService,
            INotificationService notificationService,
            ISellerMonthlyReportService monthlyReportService,
            IStockAlertService stockAlertService)
        {
            _db = db;
            _logger = logger;
            _userManager = userManager;
            _rewardService = rewardService;
            _notificationService = notificationService;
            _monthlyReportService = monthlyReportService;
            _stockAlertService = stockAlertService;
        }

        // Helper method to get current user's seller ID
        private async Task<int?> GetCurrentSellerIdAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            return seller?.Id;
        }

        // Helper method to check if current user is Admin or Moderator (not Seller)
        private bool IsAdminOrModerator()
        {
            return User.IsInRole("SuperAdmin") || User.IsInRole("Admin") || User.IsInRole("Moderator");
        }

        // Helper method to check if an order contains products from the seller
        private async Task<bool> OrderContainsSellerProductsAsync(int orderId, int sellerId)
        {
            return await _db.OrderItems
                .Include(oi => oi.Product)
                .AnyAsync(oi => oi.OrderId == orderId && oi.Product.SellerId == sellerId);
        }

        // Helper method to get order IDs that contain seller's products
        private async Task<List<int>> GetSellerOrderIdsAsync(int sellerId)
        {
            return await _db.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SellerId == sellerId)
                .Select(oi => oi.OrderId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<IActionResult> Index(int? page, string? status, string? orderId, DateTime? orderDate)
        {
            var query = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // If Seller, filter to only orders containing their products
            // Also restrict: Sellers can only see COD orders when pending (not other payment methods in pending)
            if (!IsAdminOrModerator())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId != null)
                {
                    var sellerOrderIds = await GetSellerOrderIdsAsync(sellerId.Value);
                    query = query.Where(o => sellerOrderIds.Contains(o.Id));

                    // Sellers can only see pending orders if they are COD
                    // Non-COD pending orders are hidden from sellers (Admin/Moderator handles payment verification)
                    query = query.Where(o => o.Status != OrderStatus.Pending || o.PaymentMethod == PaymentMethod.CashOnDelivery);
                }
                else
                {
                    // Seller without a seller profile - show no orders
                    query = query.Where(o => false);
                }
            }

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

            // Order by most recent first
            query = query.OrderByDescending(o => o.OrderDate);

            var orders = await query.ToListAsync();
            var pagedOrders = orders.ToPagedList(page ?? 1, 10);

            // Pass filter values to view
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentOrderId = orderId;
            ViewBag.CurrentOrderDate = orderDate?.ToString("yyyy-MM-dd");
            ViewBag.StatusList = Enum.GetValues<OrderStatus>().Select(s => s.ToString()).ToList();
            ViewBag.IsAdmin = IsAdminOrModerator();

            return View(pagedOrders);
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
                TempData["error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            // Sellers can only view orders containing their products
            if (!IsAdminOrModerator())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null || !await OrderContainsSellerProductsAsync(id, sellerId.Value))
                {
                    TempData["error"] = "You can only view orders containing your products.";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.IsAdmin = IsAdminOrModerator();
            return View(order);
        }

        [Authorize(Roles = "SuperAdmin,Admin,Moderator")]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            ViewBag.StatusList = Enum.GetValues<OrderStatus>().ToList();
            ViewBag.IsAdmin = IsAdminOrModerator();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin,Moderator")]
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
                    TempData["error"] = "Order not found.";
                    return RedirectToAction("Index");
                }

                ViewBag.StatusList = Enum.GetValues<OrderStatus>().ToList();
                ViewBag.IsAdmin = IsAdminOrModerator();
                return View(existingOrder);
            }

            try
            {
                var existingOrder = await _db.Orders.FirstOrDefaultAsync(o => o.Id == order.Id);
                if (existingOrder == null)
                {
                    TempData["error"] = "Order not found.";
                    return RedirectToAction("Index");
                }

                // Check if status is changing to Returned for COD order - auto restrict COD
                var previousStatus = existingOrder.Status;

                // Update allowed fields
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

                TempData["success"] = $"Order #{order.Id} updated successfully.";
                return RedirectToAction("Details", new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", order.Id);
                TempData["error"] = "An error occurred while updating the order.";
                ViewBag.StatusList = Enum.GetValues<OrderStatus>().ToList();
                return View(order);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            try
            {
                // Sellers can only update status on orders containing their products
                // and only to specific statuses
                if (!IsAdminOrModerator())
                {
                    var sellerId = await GetCurrentSellerIdAsync();
                    if (sellerId == null || !await OrderContainsSellerProductsAsync(id, sellerId.Value))
                    {
                        return Json(new { success = false, message = "You can only update orders containing your products." });
                    }

                    // Sellers can only change to these specific statuses
                    var allowedStatuses = new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Cancelled,
                                                   OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered };
                    if (!allowedStatuses.Contains(status))
                    {
                        return Json(new { success = false, message = "You are not authorized to set this status." });
                    }
                }

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

                // Stock adjustment: Decrease stock when order is Confirmed (from Pending)
                if (status == OrderStatus.Confirmed && previousStatus == OrderStatus.Pending)
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.Stock -= item.Quantity;
                            if (item.Product.Stock < 0) item.Product.Stock = 0;

                            // Update product status if out of stock
                            if (item.Product.Stock <= 0)
                            {
                                item.Product.Status = ProductStatus.OutOfStock;
                                item.Product.IsAvailable = false;
                            }
                        }
                    }
                    _logger.LogInformation($"Stock decreased for order #{order.Id} (Confirmed)");
                }

                // Stock adjustment: Restore stock when order is Cancelled (from Confirmed/Paid/Shipped)
                if (status == OrderStatus.Cancelled &&
                    (previousStatus == OrderStatus.Confirmed || previousStatus == OrderStatus.Paid || previousStatus == OrderStatus.Shipped))
                {
                    var currentUserId = _userManager.GetUserId(User);
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            var oldStock = item.Product.Stock;
                            item.Product.Stock += item.Quantity;

                            // Update product status if was out of stock
                            if (item.Product.Stock > 0 && item.Product.Status == ProductStatus.OutOfStock)
                            {
                                item.Product.Status = ProductStatus.Active;
                                item.Product.IsAvailable = true;
                            }

                            // Record stock change
                            await _stockAlertService.RecordStockChangeAsync(
                                item.ProductId, item.ProductVariantId, oldStock, item.Product.Stock,
                                StockChangeReason.OrderCancelled, $"Order #{order.OrderNumber} cancelled", order.Id, currentUserId);
                        }
                    }
                    order.CancelledAt = DateTime.UtcNow;
                    _logger.LogInformation($"Stock restored for order #{order.Id} (Cancelled)");
                }

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
                var verifiedBy = currentUser?.FullName ?? currentUser?.Email ?? User.Identity?.Name ?? "Admin";

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
                var verifiedBy = currentUser?.FullName ?? currentUser?.Email ?? User.Identity?.Name ?? "Admin";

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
                var processedBy = currentUser?.FullName ?? currentUser?.Email ?? User.Identity?.Name ?? "Admin";

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

        /// <summary>
        /// List all return requests with filters
        /// </summary>
        public async Task<IActionResult> Returns(int? page, string? status)
        {
            // Only Admin can access return management
            if (!IsAdminOrModerator())
            {
                TempData["error"] = "Access denied. Only administrators can manage returns.";
                return RedirectToAction("Index");
            }

            var query = _db.Orders
                .Where(o => o.ReturnReason != null)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Filter by return status (using RefundStatus)
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

            // Stats (using RefundStatus)
            var allReturns = await _db.Orders.Where(o => o.ReturnReason != null).ToListAsync();
            ViewBag.TotalReturns = allReturns.Count;
            ViewBag.PendingReturns = allReturns.Count(o => o.RefundStatus == RefundStatus.Pending || o.RefundStatus == RefundStatus.Reviewing || o.RefundStatus == RefundStatus.InProgress);
            ViewBag.ProcessedReturns = allReturns.Count(o => o.RefundStatus == RefundStatus.Approved);
            ViewBag.CurrentStatus = status;

            return View(pagedReturns);
        }

        /// <summary>
        /// View detailed return request information
        /// </summary>
        public async Task<IActionResult> ReviewReturn(int id)
        {
            if (!IsAdminOrModerator())
            {
                TempData["error"] = "Access denied. Only administrators can review returns.";
                return RedirectToAction("Index");
            }

            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Coupon)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["error"] = "Order not found.";
                return RedirectToAction("Returns");
            }

            if (string.IsNullOrEmpty(order.ReturnReason))
            {
                TempData["error"] = "This order has no return request.";
                return RedirectToAction("Details", new { id });
            }

            // Get admin and manager users for the accounting referral dropdown
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var managerUsers = await _userManager.GetUsersInRoleAsync("Manager");
            var accountingUsers = adminUsers.Concat(managerUsers)
                .Select(u => new { u.Id, Name = $"{u.FirstName} {u.LastName}".Trim(), u.Email })
                .Where(u => !string.IsNullOrEmpty(u.Name))
                .Distinct()
                .OrderBy(u => u.Name)
                .ToList();
            ViewBag.AccountingUsers = accountingUsers;

            return View(order);
        }

        /// <summary>
        /// Approve a return request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReturn(int id, string? adminNotes)
        {
            if (!IsAdminOrModerator())
            {
                return Json(new { success = false, message = "Access denied." });
            }

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

                // Update order status
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

        /// <summary>
        /// Reject a return request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReturn(int id, string rejectionReason)
        {
            if (!IsAdminOrModerator())
            {
                return Json(new { success = false, message = "Access denied." });
            }

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

                // Revert to Delivered status and keep return reason for record
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

        /// <summary>
        /// Process refund for an approved return
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(int id, string refundMethod, decimal refundAmount,
            string? transactionId, string? notes, string? paidFromAccount, string? receiverNumber)
        {
            if (!IsAdminOrModerator())
            {
                return Json(new { success = false, message = "Access denied." });
            }

            try
            {
                var order = await _db.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
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

                // Parse refund method enum
                RefundMethod? parsedRefundMethod = refundMethod switch
                {
                    "WalletCredit" => RefundMethod.WalletCredit,
                    "Bkash" => RefundMethod.Bkash,
                    "Nagad" => RefundMethod.Nagad,
                    "Upay" => RefundMethod.Upay,
                    "Rocket" => RefundMethod.Rocket,
                    "BankTransfer" => RefundMethod.BankTransfer,
                    _ => null
                };

                // Process refund based on method
                if (refundMethod == "WalletCredit" && order.User != null)
                {
                    // Add to user's wallet (round to nearest integer)
                    order.User.WalletBalance += Math.Round(refundAmount);

                    // Record transaction
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

                    _logger.LogInformation("Wallet refund of ৳{Amount} processed for order {OrderId}, User {UserId}",
                        refundAmount, order.Id, order.UserId);
                }
                else
                {
                    // For other methods (bKash, Nagad, Bank Transfer, etc.)
                    // Just record the transaction - actual transfer is manual
                    var refundTransaction = new Transaction
                    {
                        OrderId = order.Id,
                        UserId = order.UserId,
                        Type = TransactionType.Refund,
                        Amount = refundAmount,
                        Notes = $"Refund for Order #{order.OrderNumber ?? order.Id.ToString()} via {GetRefundMethodDisplay(refundMethod)}",
                        GatewayTransactionId = transactionId,
                        Status = TransactionStatus.Completed,
                        CreatedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    };
                    _db.Transactions.Add(refundTransaction);
                }

                // Update order status
                order.Status = OrderStatus.Refunded;

                // Calculate refundable amount (SubTotal - all discounts, delivery charge is non-refundable)
                var subTotal = order.SubTotal > 0 ? order.SubTotal : order.OrderItems.Sum(i => i.TotalPrice);
                var totalDiscounts = order.DiscountAmount + order.RewardDiscountAmount + order.PremiumDiscountAmount;
                var refundableAmount = subTotal - totalDiscounts;

                // If refund amount is equal to or greater than refundable amount, mark as fully Refunded
                order.PaymentStatus = refundAmount >= refundableAmount
                    ? PaymentStatus.Refunded
                    : PaymentStatus.PartiallyRefunded;

                // Save refund payment details
                order.RefundPaymentMethod = parsedRefundMethod;
                order.RefundPaidFromAccount = paidFromAccount;
                order.RefundReceiverNumber = receiverNumber ?? order.RefundAccountNumber;
                order.RefundTransactionId = transactionId;
                order.RefundPaidAmount = refundAmount;
                order.RefundPaymentDate = DateTime.UtcNow;
                order.RefundPaymentNotes = notes;

                // Update refund status to Completed
                order.RefundStatus = RefundStatus.Completed;
                order.RefundStatusChangedAt = DateTime.UtcNow;

                order.AdminNotes = string.IsNullOrEmpty(notes)
                    ? $"{order.AdminNotes}\nRefund of ৳{refundAmount:N2} processed via {GetRefundMethodDisplay(refundMethod)} on {DateTime.UtcNow:yyyy-MM-dd HH:mm}"
                    : $"{order.AdminNotes}\nRefund: {notes}";

                order.UpdatedAt = DateTime.UtcNow;

                // Close related support conversations
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var relatedConversations = await _db.SellerConversations
                    .Where(c => c.OrderId == order.Id && !c.IsClosedByStaff)
                    .ToListAsync();

                foreach (var conversation in relatedConversations)
                {
                    conversation.IsClosedByStaff = true;
                    conversation.ClosedByUserId = userId;
                }

                await _db.SaveChangesAsync();

                var message = refundMethod == "WalletCredit" && order.User != null
                    ? $"Refund of ৳{refundAmount:N2} credited to customer's wallet successfully."
                    : $"Refund of ৳{refundAmount:N2} processed via {GetRefundMethodDisplay(refundMethod)}.";

                return Json(new { success = true, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for order {OrderId}", id);
                return Json(new { success = false, message = "An error occurred while processing the refund." });
            }
        }

        /// <summary>
        /// Update refund payment details for an order
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRefundPaymentDetails(int id, string? refundPaymentMethod, decimal? refundPaidAmount,
            string? paidFromAccount, string? receiverNumber, string? transactionId, string? notes)
        {
            if (!IsAdminOrModerator())
            {
                return Json(new { success = false, message = "Access denied." });
            }

            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                // Parse refund method enum
                RefundMethod? parsedRefundMethod = refundPaymentMethod switch
                {
                    "WalletCredit" => RefundMethod.WalletCredit,
                    "Bkash" => RefundMethod.Bkash,
                    "Nagad" => RefundMethod.Nagad,
                    "Upay" => RefundMethod.Upay,
                    "Rocket" => RefundMethod.Rocket,
                    "BankTransfer" => RefundMethod.BankTransfer,
                    _ => order.RefundPaymentMethod // Keep existing if not specified
                };

                // Update refund payment details
                order.RefundPaymentMethod = parsedRefundMethod;
                order.RefundPaidFromAccount = paidFromAccount;
                order.RefundReceiverNumber = receiverNumber;
                order.RefundTransactionId = transactionId;
                order.RefundPaymentNotes = notes;

                if (refundPaidAmount.HasValue && refundPaidAmount > 0)
                {
                    order.RefundPaidAmount = refundPaidAmount;

                    // Recalculate and update PaymentStatus based on refund amount
                    var subTotal = order.SubTotal > 0 ? order.SubTotal : order.OrderItems.Sum(i => i.TotalPrice);
                    var totalDiscounts = order.DiscountAmount + order.RewardDiscountAmount + order.PremiumDiscountAmount;
                    var refundableAmount = subTotal - totalDiscounts;

                    // If refund amount is equal to or greater than refundable amount, mark as fully Refunded
                    order.PaymentStatus = refundPaidAmount >= refundableAmount
                        ? PaymentStatus.Refunded
                        : PaymentStatus.PartiallyRefunded;

                    // Also update Order Status and Refund Status since refund payment details exist
                    order.Status = OrderStatus.Refunded;
                    if (order.RefundStatus != RefundStatus.Completed)
                    {
                        order.RefundStatus = RefundStatus.Completed;
                        order.RefundStatusChangedAt = DateTime.UtcNow;
                    }
                }

                order.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Refund payment details updated for order {OrderId}", id);

                return Json(new { success = true, message = "Refund payment details updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating refund payment details for order {OrderId}", id);
                return Json(new { success = false, message = "An error occurred while updating refund payment details." });
            }
        }

        /// <summary>
        /// Process replacement order for a return
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReplacement(int id, string? notes)
        {
            if (!IsAdminOrModerator())
            {
                return Json(new { success = false, message = "Access denied." });
            }

            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                // Create a new replacement order (clone)
                var replacementOrder = new Order
                {
                    OrderNumber = $"R-{order.OrderNumber ?? order.Id.ToString()}",
                    UserId = order.UserId,
                    CustomerName = order.CustomerName,
                    Email = order.Email,
                    Phone = order.Phone,
                    Address = order.Address,
                    City = order.City,
                    PostalCode = order.PostalCode,
                    DivisionId = order.DivisionId,
                    DistrictId = order.DistrictId,
                    UpazilaId = order.UpazilaId,
                    SubTotal = order.SubTotal,
                    DeliveryCharge = 0, // Free delivery for replacement
                    TotalAmount = order.SubTotal, // No delivery charge
                    PaymentMethod = order.PaymentMethod,
                    Status = OrderStatus.Confirmed,
                    PaymentStatus = PaymentStatus.Completed, // Already paid via original order
                    IsPaymentReceived = true,
                    Notes = $"Replacement for Order #{order.OrderNumber ?? order.Id.ToString()}",
                    AdminNotes = notes,
                    FreeShippingApplied = true,
                    OrderDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Orders.Add(replacementOrder);
                await _db.SaveChangesAsync();

                // Clone order items
                foreach (var item in order.OrderItems)
                {
                    var replacementItem = new OrderItem
                    {
                        OrderId = replacementOrder.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    };
                    _db.OrderItems.Add(replacementItem);
                }

                // Update original order
                order.Status = OrderStatus.Refunded;
                order.AdminNotes = $"{order.AdminNotes}\nReplacement order #{replacementOrder.Id} created on {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
                order.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Replacement order #{replacementOrder.Id} created successfully.",
                    replacementOrderId = replacementOrder.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing replacement for order {OrderId}", id);
                return Json(new { success = false, message = "An error occurred while creating the replacement order." });
            }
        }

        // Helper method to get return images as list
        public List<string> GetReturnImages(Order order)
        {
            if (string.IsNullOrEmpty(order.ReturnImages))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(order.ReturnImages) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        // Helper method to get display name for refund method
        private string GetRefundMethodDisplay(string method)
        {
            return method switch
            {
                "WalletCredit" => "Wallet Credit",
                "Bkash" => "bKash",
                "Nagad" => "Nagad",
                "Upay" => "Upay",
                "Rocket" => "Rocket",
                "BankTransfer" => "Bank Transfer",
                _ => method
            };
        }

        // Helper method to format return reason for display
        public string FormatReturnReasonForDisplay(string? returnReason)
        {
            if (string.IsNullOrEmpty(returnReason))
                return "No reason specified";

            var reasonMappings = new Dictionary<string, string>
            {
                { "Damaged", "Damaged Product" },
                { "WrongItem", "Wrong Item Received" },
                { "NotAsDescribed", "Product Not as Described" },
                { "QualityIssue", "Quality Not Satisfactory" },
                { "SizeIssue", "Size/Fit Issue" },
                { "ChangedMind", "Changed Mind" },
                { "Other", "Other" }
            };

            foreach (var mapping in reasonMappings)
            {
                if (returnReason.StartsWith(mapping.Key + ":"))
                {
                    return mapping.Value + returnReason.Substring(mapping.Key.Length);
                }
                else if (returnReason == mapping.Key)
                {
                    return mapping.Value;
                }
            }

            return returnReason;
        }

        /// <summary>
        /// Update refund status for a return request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRefundStatus(int id, string newStatus, string? notes, decimal? approvedAmount)
        {
            if (!IsAdminOrModerator())
            {
                return Json(new { success = false, message = "Access denied." });
            }

            // Parse the status string to enum
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

                var previousStatus = order.RefundStatus;
                order.RefundStatus = parsedStatus;
                order.RefundStatusChangedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(notes))
                {
                    order.RefundAdminNotes = string.IsNullOrEmpty(order.RefundAdminNotes)
                        ? $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Status: {parsedStatus} - {notes}"
                        : $"{order.RefundAdminNotes}\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Status: {parsedStatus} - {notes}";
                }

                // If approved, set approved amount
                if (parsedStatus == RefundStatus.Approved && approvedAmount.HasValue)
                {
                    order.ApprovedRefundAmount = approvedAmount.Value;

                    // If refund method is WalletCredit and user exists, automatically process wallet refund
                    if (order.PreferredRefundMethod == RefundMethod.WalletCredit && order.User != null)
                    {
                        order.User.WalletBalance += Math.Round(approvedAmount.Value);

                        // Record transaction
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

                        // Update order status
                        order.Status = OrderStatus.Refunded;
                        order.PaymentStatus = approvedAmount.Value >= order.TotalAmount
                            ? PaymentStatus.Refunded
                            : PaymentStatus.PartiallyRefunded;

                        _logger.LogInformation("Wallet refund of ৳{Amount} auto-processed for order {OrderId}, User {UserId}",
                            approvedAmount.Value, order.Id, order.UserId);
                    }

                    // Deduct from seller balance
                    if (order.SellerId.HasValue)
                    {
                        var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.Id == order.SellerId.Value);
                        if (seller != null)
                        {
                            // Use seller's commission rate for penalty calculation
                            var commissionRate = seller.CommissionRate / 100m;
                            var payoutRate = 1 - commissionRate;

                            // Deduct full refund amount from seller (main cost + penalty)
                            // Main cost deduction = refund amount * payoutRate (what seller received)
                            // Penalty = refund amount * commissionRate (platform's loss, charged to seller)
                            // Total = refund amount (100%)
                            var totalDeduction = approvedAmount.Value;
                            seller.AccountBalance -= totalDeduction;

                            // Create seller transaction record
                            _db.SellerTransactions.Add(new SellerTransaction
                            {
                                SellerId = seller.Id,
                                OrderId = order.Id,
                                Type = SellerTransactionType.RefundDeduction,
                                Amount = -totalDeduction,
                                BalanceAfter = seller.AccountBalance,
                                Description = $"Refund deduction ({100 - seller.CommissionRate}% payout + {seller.CommissionRate}% penalty) for order #{order.OrderNumber}",
                                CreatedAt = DateTime.UtcNow
                            });

                            seller.UpdatedAt = DateTime.UtcNow;

                            _logger.LogInformation("Seller {SellerId} balance deducted by ৳{Amount} for refund on order {OrderId}",
                                seller.Id, totalDeduction, order.Id);
                        }
                    }
                }

                // If rejected, update order status
                if (parsedStatus == RefundStatus.Rejected)
                {
                    order.Status = OrderStatus.Delivered;
                }

                // If rejected, close related support conversations
                // Note: Completed status is only set via ProcessRefund which handles conversation closing
                if (parsedStatus == RefundStatus.Rejected)
                {
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var relatedConversations = await _db.SellerConversations
                        .Where(c => c.OrderId == order.Id && !c.IsClosedByStaff)
                        .ToListAsync();

                    foreach (var conversation in relatedConversations)
                    {
                        conversation.IsClosedByStaff = true;
                        conversation.ClosedByUserId = userId;
                    }

                    if (relatedConversations.Any())
                    {
                        _logger.LogInformation("Closed {Count} support conversations for order {OrderId} due to refund rejection",
                            relatedConversations.Count, order.Id);
                    }
                }

                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var statusMessage = parsedStatus switch
                {
                    RefundStatus.Approved when order.PreferredRefundMethod == RefundMethod.WalletCredit && order.User != null
                        => $"Refund approved and ৳{approvedAmount:N2} credited to customer's wallet.",
                    RefundStatus.Approved => $"Refund approved for ৳{approvedAmount:N2}. Please process the refund using Quick Approve & Process.",
                    RefundStatus.Rejected => "Refund request rejected. Related support issues closed.",
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

        /// <summary>
        /// Refer refund request to accounting team
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReferToAccounting(int id, string accountingPersonName, string? notes)
        {
            if (!IsAdminOrModerator())
            {
                return Json(new { success = false, message = "Access denied." });
            }

            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                // Calculate refundable amount (SubTotal only, excluding delivery)
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

                // Update status to Reviewing if still pending
                if (order.RefundStatus == RefundStatus.Pending)
                {
                    order.RefundStatus = RefundStatus.Reviewing;
                    order.RefundStatusChangedAt = DateTime.UtcNow;
                }

                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Send notification to the referred accounting team member
                var referredUser = await _db.Users
                    .FirstOrDefaultAsync(u => (u.FirstName + " " + u.LastName).Trim() == accountingPersonName);

                if (referredUser != null)
                {
                    var currentUserName = User.Identity?.Name ?? "Admin";
                    var notification = new UserNotification
                    {
                        UserId = referredUser.Id,
                        Type = "order",
                        Icon = "fa-file-invoice-dollar",
                        IconColor = "text-warning",
                        Title = "Refund Request Referred to You",
                        Message = $"Order #{order.OrderNumber ?? order.Id.ToString()} refund request (৳{Math.Round(refundableAmount):N0}) has been referred to you by {currentUserName}." +
                                  (!string.IsNullOrEmpty(notes) ? $" Note: {notes}" : ""),
                        Link = $"/Admin/Orders/ReviewReturn/{order.Id}",
                        OrderId = order.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    await _notificationService.CreateNotificationAsync(notification);

                    _logger.LogInformation("Notification sent to {UserName} for refund referral of order {OrderId}",
                        accountingPersonName, order.Id);
                }

                return Json(new { success = true, message = $"Refund request referred to {accountingPersonName}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error referring order {OrderId} to accounting", id);
                return Json(new { success = false, message = "An error occurred while referring to accounting." });
            }
        }

        /// <summary>
        /// Get refund status display name
        /// </summary>
        private string GetRefundStatusDisplay(RefundStatus status)
        {
            return status switch
            {
                RefundStatus.Pending => "Pending",
                RefundStatus.Reviewing => "Reviewing",
                RefundStatus.InProgress => "In Progress",
                RefundStatus.Approved => "Approved",
                RefundStatus.Rejected => "Rejected",
                RefundStatus.Completed => "Completed",
                _ => status.ToString()
            };
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
                // Only Admin can modify order items
                if (!IsAdminOrModerator())
                {
                    return Json(new { success = false, message = "Only administrators can modify order items." });
                }

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
                    // Update quantity instead
                    existingItem.Quantity += quantity;
                    existingItem.TotalPrice = existingItem.UnitPrice * existingItem.Quantity;
                }
                else
                {
                    // Add new item
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

                // Recalculate order totals
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

                _logger.LogInformation($"Added {quantity}x {product.Name} to order #{orderId}");
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
                // Only Admin can modify order items
                if (!IsAdminOrModerator())
                {
                    return Json(new { success = false, message = "Only administrators can modify order items." });
                }

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
                        // If increasing quantity, decrease stock; if decreasing quantity, increase stock
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

                _logger.LogInformation($"Updated order item #{orderItemId} quantity from {oldQuantity} to {newQuantity}");
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
                // Only Admin can modify order items
                if (!IsAdminOrModerator())
                {
                    return Json(new { success = false, message = "Only administrators can modify order items." });
                }

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

                _logger.LogInformation($"Removed {productName} from order #{orderId}");
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
                // Only Admin can modify order items
                if (!IsAdminOrModerator())
                {
                    return Json(new { success = false, message = "Only administrators can modify order items." });
                }

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

                _logger.LogInformation($"Updated order item #{orderItemId} price to {newPrice}");
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

        /// <summary>
        /// Print shipping slip for confirmed orders
        /// </summary>
        public async Task<IActionResult> PrintShippingSlip(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p!.Seller)
                .Include(o => o.Division)
                .Include(o => o.District)
                .Include(o => o.Upazila)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            // Check access for sellers
            if (User.IsInRole("Seller") && !IsAdminOrModerator())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null || !await OrderContainsSellerProductsAsync(id, sellerId.Value))
                {
                    TempData["error"] = "You don't have access to this order.";
                    return RedirectToAction("Index");
                }
            }

            // Check if order is confirmed (for non-COD, payment must also be confirmed)
            bool canPrint = false;
            if (order.PaymentMethod == PaymentMethod.CashOnDelivery)
            {
                // COD orders can be printed once confirmed
                canPrint = order.Status == OrderStatus.Confirmed ||
                           order.Status == OrderStatus.Paid ||
                           order.Status == OrderStatus.Shipped ||
                           order.Status == OrderStatus.Delivered;
            }
            else
            {
                // Non-COD orders need both payment and order confirmed
                canPrint = (order.Status == OrderStatus.Paid ||
                            order.Status == OrderStatus.Shipped ||
                            order.Status == OrderStatus.Delivered) ||
                           (order.Status == OrderStatus.Confirmed && order.PaymentStatus == PaymentStatus.Completed);
            }

            if (!canPrint)
            {
                TempData["error"] = "Shipping slip can only be printed for confirmed orders. For online payment orders, payment must also be confirmed.";
                return RedirectToAction("Details", new { id });
            }

            // Get seller info for the order
            var sellerIds = order.OrderItems
                .Where(oi => oi.Product?.SellerId != null)
                .Select(oi => oi.Product!.SellerId)
                .Distinct()
                .ToList();

            var sellers = await _db.Sellers
                .Include(s => s.User)
                .Where(s => sellerIds.Contains(s.Id))
                .ToListAsync();

            ViewBag.Sellers = sellers;
            ViewBag.SiteSettings = await _db.SiteSettings.FirstOrDefaultAsync();

            return View(order);
        }

        /// <summary>
        /// Get product variants for variant selection
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProductVariants(int productId)
        {
            var variants = await _db.ProductVariants
                .Where(v => v.ProductId == productId && v.IsAvailable)
                .Select(v => new
                {
                    id = v.Id,
                    size = v.Size ?? "",
                    color = v.Color ?? "",
                    stock = v.Stock,
                    additionalPrice = v.AdditionalPrice,
                    sku = v.SKU ?? ""
                })
                .ToListAsync();

            return Json(new { success = true, variants });
        }

        /// <summary>
        /// Update order item variant
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderItemVariant(int orderItemId, int? variantId)
        {
            try
            {
                // Only Admin can modify order items
                if (!IsAdminOrModerator())
                {
                    return Json(new { success = false, message = "Only administrators can modify order items." });
                }

                var orderItem = await _db.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

                if (orderItem == null)
                {
                    return Json(new { success = false, message = "Order item not found." });
                }

                // Get the new variant info
                ProductVariant? newVariant = null;
                string newVariantInfo = "";
                decimal additionalPrice = 0;

                if (variantId.HasValue && variantId.Value > 0)
                {
                    newVariant = await _db.ProductVariants.FindAsync(variantId.Value);
                    if (newVariant == null)
                    {
                        return Json(new { success = false, message = "Variant not found." });
                    }

                    // Build variant info string
                    var parts = new List<string>();
                    if (!string.IsNullOrEmpty(newVariant.Size)) parts.Add($"Size: {newVariant.Size}");
                    if (!string.IsNullOrEmpty(newVariant.Color)) parts.Add($"Color: {newVariant.Color}");
                    newVariantInfo = string.Join(", ", parts);
                    additionalPrice = newVariant.AdditionalPrice;
                }

                // Calculate new unit price (base price + variant additional price)
                var basePrice = orderItem.Product?.HasDiscount == true && orderItem.Product.DiscountPrice.HasValue
                    ? orderItem.Product.DiscountPrice.Value
                    : (orderItem.Product?.Price ?? orderItem.UnitPrice);

                var newUnitPrice = basePrice + additionalPrice;

                // Update order item
                orderItem.ProductVariantId = variantId;
                orderItem.VariantInfo = string.IsNullOrEmpty(newVariantInfo) ? null : newVariantInfo;
                orderItem.UnitPrice = newUnitPrice;
                orderItem.TotalPrice = newUnitPrice * orderItem.Quantity;

                await _db.SaveChangesAsync();
                await RecalculateOrderTotals(orderItem.OrderId);

                _logger.LogInformation($"Updated order item #{orderItemId} variant to {newVariantInfo ?? "None"}");
                return Json(new
                {
                    success = true,
                    message = $"Variant updated to {(string.IsNullOrEmpty(newVariantInfo) ? "None" : newVariantInfo)}.",
                    variantInfo = newVariantInfo,
                    unitPrice = newUnitPrice,
                    totalPrice = orderItem.TotalPrice
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order item variant {OrderItemId}", orderItemId);
                return Json(new { success = false, message = "An error occurred while updating the variant." });
            }
        }
    }
}