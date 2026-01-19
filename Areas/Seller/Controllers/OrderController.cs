using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "SuperAdmin,Seller,Admin")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public OrderController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _localizer = localizer;
        }

        private async Task<Models.Seller?> GetCurrentSellerAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            return await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
        }

        /// <summary>
        /// Notify Admin and Moderator users when seller performs order actions
        /// </summary>
        private async Task NotifyAdminsAndModeratorsAsync(Order order, string action, string sellerName, string icon, string iconColor)
        {
            try
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var moderators = await _userManager.GetUsersInRoleAsync("Moderator");
                var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");

                var staffUsers = admins.Concat(moderators).Concat(superAdmins)
                    .DistinctBy(u => u.Id)
                    .ToList();

                foreach (var user in staffUsers)
                {
                    await _notificationService.CreateNotificationAsync(new UserNotification
                    {
                        UserId = user.Id,
                        Type = "order",
                        Icon = icon,
                        IconColor = iconColor,
                        Title = $"Order {action} by Seller",
                        Message = $"Order #{order.OrderNumber} has been {action.ToLower()} by {sellerName}",
                        Link = $"/Admin/Orders/Details/{order.Id}",
                        OrderId = order.Id
                    });
                }
            }
            catch (Exception)
            {
                // Log but don't fail - admin notification is best-effort
            }
        }

        private async Task<int?> GetCurrentSellerIdAsync()
        {
            var seller = await GetCurrentSellerAsync();
            return seller?.Id;
        }

        // GET: /Seller/Order
        public async Task<IActionResult> Index(string? status, string? search, int page = 1)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            // Get orders that have this seller's products
            // Single-shop-per-order: orders should have SellerId pointing to seller
            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.SellerId == sellerId);

            // For online payment orders, only show if admin has confirmed (status >= Confirmed)
            // COD orders can be seen immediately
            ordersQuery = ordersQuery.Where(o =>
                o.PaymentMethod == PaymentMethod.CashOnDelivery ||
                o.Status >= OrderStatus.Confirmed);

            // Status filter
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == orderStatus);
            }

            // Search by order number or customer name
            if (!string.IsNullOrEmpty(search))
            {
                ordersQuery = ordersQuery.Where(o =>
                    o.OrderNumber.Contains(search) ||
                    (o.User != null && o.User.FullName != null && o.User.FullName.Contains(search)) ||
                    (o.User != null && o.User.Email != null && o.User.Email.Contains(search)));
            }

            // Pagination
            var pageSize = 20;
            var totalOrders = await ordersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get counts by status
            var statusCounts = await _context.Orders
                .Where(o => o.SellerId == sellerId)
                .Where(o => o.PaymentMethod == PaymentMethod.CashOnDelivery || o.Status >= OrderStatus.Confirmed)
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.StatusCounts = statusCounts;
            ViewBag.SellerId = sellerId;

            return View(orders);
        }

        // GET: /Seller/Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Seller)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Division)
                .Include(o => o.District)
                .FirstOrDefaultAsync(o => o.Id == id && o.SellerId == sellerId);

            if (order == null)
            {
                TempData["Error"] = _localizer["OrderNotFoundOrNoAccess"].Value;
                return RedirectToAction(nameof(Index));
            }

            // For online payment orders, only allow viewing if confirmed
            if (order.PaymentMethod != PaymentMethod.CashOnDelivery && order.Status < OrderStatus.Confirmed)
            {
                TempData["Error"] = _localizer["OrderPendingAdminConfirmation"].Value;
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SellerTotal = order.OrderItems.Sum(oi => oi.TotalPrice);
            ViewBag.CanConfirm = order.Status == OrderStatus.Pending && order.PaymentMethod == PaymentMethod.CashOnDelivery;
            ViewBag.CanProcess = order.Status == OrderStatus.Confirmed;
            ViewBag.CanShip = order.Status == OrderStatus.Processing;
            ViewBag.CanDeliver = order.Status == OrderStatus.Shipped;

            return View(order);
        }

        // POST: /Seller/Order/Confirm - For COD orders only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && o.SellerId == sellerId);

            if (order == null)
            {
                TempData["Error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Only COD orders can be confirmed by seller
            if (order.PaymentMethod != PaymentMethod.CashOnDelivery)
            {
                TempData["Error"] = _localizer["OnlyCODOrdersCanBeConfirmedDirectly"].Value;
                return RedirectToAction("Details", new { id });
            }

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = _localizer["OnlyPendingOrdersCanBeConfirmed"].Value;
                return RedirectToAction("Details", new { id });
            }

            order.Status = OrderStatus.Confirmed;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify customer
            if (order.User != null)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = order.UserId!,
                    Type = "order",
                    Icon = "fa-check-circle",
                    IconColor = "text-success",
                    Title = "Order Confirmed",
                    Message = $"Your order #{order.OrderNumber} has been confirmed by the seller.",
                    Link = $"/Identity/Account/Manage/Orders?orderId={order.Id}"
                });
            }

            // Notify Admin/Moderator
            var seller = await GetCurrentSellerAsync();
            var sellerName = seller?.ShopName ?? "Unknown Seller";
            await NotifyAdminsAndModeratorsAsync(order, "Confirmed", sellerName, "fa-check-circle", "text-success");

            TempData["Success"] = _localizer["OrderConfirmedSuccess"].Value;
            return RedirectToAction("Details", new { id });
        }

        // POST: /Seller/Order/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.SellerId == sellerId);

            if (order == null)
            {
                TempData["Error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Can only cancel Pending or Confirmed orders
            if (order.Status > OrderStatus.Confirmed)
            {
                TempData["Error"] = _localizer["CannotCancelOrderAlreadyProcessing"].Value;
                return RedirectToAction("Details", new { id });
            }

            // Restore stock
            foreach (var item in order.OrderItems)
            {
                if (item.Product != null)
                {
                    item.Product.Stock += item.Quantity;
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = reason ?? "Cancelled by seller";
            order.CancelledAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify customer
            if (order.User != null)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = order.UserId!,
                    Type = "order",
                    Icon = "fa-times-circle",
                    IconColor = "text-danger",
                    Title = "Order Cancelled",
                    Message = $"Your order #{order.OrderNumber} has been cancelled. Reason: {order.CancellationReason}",
                    Link = $"/Identity/Account/Manage/Orders?orderId={order.Id}"
                });
            }

            // Notify Admin/Moderator
            var currentSeller = await GetCurrentSellerAsync();
            var currentSellerName = currentSeller?.ShopName ?? "Unknown Seller";
            await NotifyAdminsAndModeratorsAsync(order, "Cancelled", currentSellerName, "fa-times-circle", "text-danger");

            TempData["Success"] = _localizer["OrderCancelledSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: /Seller/Order/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && o.SellerId == seller.Id);

            if (order == null)
            {
                TempData["Error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Sellers can update to: Processing, Shipped, Delivered
            var allowedStatuses = new[] {
                OrderStatus.Processing,
                OrderStatus.Shipped,
                OrderStatus.Delivered
            };

            if (!allowedStatuses.Contains(status))
            {
                TempData["Error"] = _localizer["InvalidStatusOnlyProcessingShippedDelivered"].Value;
                return RedirectToAction("Details", new { id });
            }

            // Only allow forward progression
            if (status <= order.Status)
            {
                TempData["Error"] = _localizer["CannotMoveToPreviousStatus"].Value;
                return RedirectToAction("Details", new { id });
            }

            // Check valid transitions
            if (status == OrderStatus.Processing && order.Status != OrderStatus.Confirmed)
            {
                TempData["Error"] = _localizer["OrderMustBeConfirmedBeforeProcessing"].Value;
                return RedirectToAction("Details", new { id });
            }

            if (status == OrderStatus.Shipped && order.Status != OrderStatus.Processing)
            {
                TempData["Error"] = _localizer["OrderMustBeProcessingBeforeShipping"].Value;
                return RedirectToAction("Details", new { id });
            }

            if (status == OrderStatus.Delivered && order.Status != OrderStatus.Shipped)
            {
                TempData["Error"] = _localizer["OrderMustBeShippedBeforeDelivered"].Value;
                return RedirectToAction("Details", new { id });
            }

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            if (status == OrderStatus.Shipped)
            {
                order.ShippedAt = DateTime.UtcNow;
            }
            else if (status == OrderStatus.Delivered)
            {
                order.DeliveredAt = DateTime.UtcNow;
                order.DeliveryStatus = DeliveryStatus.Delivered;

                // Handle commission/payout based on payment method
                await ProcessDeliveryPayoutAsync(order, seller);
            }

            await _context.SaveChangesAsync();

            // Notify customer
            var iconMap = new Dictionary<OrderStatus, (string, string)>
            {
                { OrderStatus.Processing, ("fa-box", "text-primary") },
                { OrderStatus.Shipped, ("fa-shipping-fast", "text-info") },
                { OrderStatus.Delivered, ("fa-check-double", "text-success") }
            };

            var (icon, color) = iconMap.GetValueOrDefault(status, ("fa-info-circle", "text-secondary"));

            if (order.User != null)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = order.UserId!,
                    Type = "order",
                    Icon = icon,
                    IconColor = color,
                    Title = $"Order {status}",
                    Message = $"Your order #{order.OrderNumber} has been {status.ToString().ToLower()}.",
                    Link = $"/Identity/Account/Manage/Orders?orderId={order.Id}"
                });
            }

            // Notify Admin/Moderator
            await NotifyAdminsAndModeratorsAsync(order, status.ToString(), seller.ShopName ?? "Unknown Seller", icon, color);

            TempData["Success"] = _localizer["OrderStatusUpdatedTo"].Value + " " + status.ToString() + ".";
            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// Process commission/payout when order is delivered
        /// </summary>
        private async Task ProcessDeliveryPayoutAsync(Order order, Models.Seller seller)
        {
            // Calculate commission based on seller's rate
            var commissionRate = seller.CommissionRate / 100m;
            var payoutRate = 1 - commissionRate;
            var productTotal = order.SubTotal; // Without delivery charge

            if (order.PaymentMethod == PaymentMethod.CashOnDelivery)
            {
                // COD: Deduct commission from seller balance
                // Seller collected full amount from customer, platform takes commission
                var commission = productTotal * commissionRate;
                seller.AccountBalance -= commission;

                _context.SellerTransactions.Add(new SellerTransaction
                {
                    SellerId = seller.Id,
                    OrderId = order.Id,
                    Type = SellerTransactionType.Commission,
                    Amount = -commission,
                    BalanceAfter = seller.AccountBalance,
                    Description = $"{seller.CommissionRate}% commission for COD order #{order.OrderNumber}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // Online payment: Add payout to seller balance
                // Platform collected full amount, seller gets their share + delivery charge
                // (Seller handles delivery themselves, so delivery charge goes to them)
                var sellerEarnings = productTotal * payoutRate;
                var deliveryCharge = order.DeliveryCharge;
                var totalPayout = sellerEarnings + deliveryCharge;

                seller.AccountBalance += totalPayout;

                _context.SellerTransactions.Add(new SellerTransaction
                {
                    SellerId = seller.Id,
                    OrderId = order.Id,
                    Type = SellerTransactionType.OrderPayout,
                    Amount = totalPayout,
                    BalanceAfter = seller.AccountBalance,
                    Description = deliveryCharge > 0
                        ? $"{100 - seller.CommissionRate}% payout (৳{sellerEarnings:N0}) + delivery (৳{deliveryCharge:N0}) for order #{order.OrderNumber}"
                        : $"{100 - seller.CommissionRate}% payout for online order #{order.OrderNumber}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Update seller stats
            seller.TotalSales += productTotal;
            seller.TotalOrders += 1;
            seller.UpdatedAt = DateTime.UtcNow;
        }

        // GET: /Seller/Order/Pending
        public IActionResult Pending()
        {
            return RedirectToAction(nameof(Index), new { status = "Pending" });
        }

        // GET: /Seller/Order/Confirmed
        public IActionResult Confirmed()
        {
            return RedirectToAction(nameof(Index), new { status = "Confirmed" });
        }

        // GET: /Seller/Order/Processing
        public IActionResult Processing()
        {
            return RedirectToAction(nameof(Index), new { status = "Processing" });
        }

        // GET: /Seller/Order/Shipped
        public IActionResult Shipped()
        {
            return RedirectToAction(nameof(Index), new { status = "Shipped" });
        }

        // GET: /Seller/Order/Delivered
        public IActionResult Delivered()
        {
            return RedirectToAction(nameof(Index), new { status = "Delivered" });
        }

        /// <summary>
        /// Print shipping slip for confirmed orders
        /// </summary>
        public async Task<IActionResult> PrintShippingSlip(int id)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                TempData["error"] = _localizer["SellerProfileNotFound"].Value;
                return RedirectToAction("Index");
            }

            var order = await _context.Orders
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
                TempData["error"] = _localizer["OrderNotFound"].Value;
                return RedirectToAction("Index");
            }

            // Check if order contains seller's products
            var hasSellerProducts = order.OrderItems.Any(oi => oi.Product?.SellerId == seller.Id);
            if (!hasSellerProducts)
            {
                TempData["error"] = _localizer["YouDontHaveAccessToThisOrder"].Value;
                return RedirectToAction("Index");
            }

            // Check if order is confirmed (for non-COD, payment must also be confirmed)
            bool canPrint = false;
            if (order.PaymentMethod == PaymentMethod.CashOnDelivery)
            {
                canPrint = order.Status == OrderStatus.Confirmed ||
                           order.Status == OrderStatus.Paid ||
                           order.Status == OrderStatus.Shipped ||
                           order.Status == OrderStatus.Delivered;
            }
            else
            {
                canPrint = (order.Status == OrderStatus.Paid ||
                            order.Status == OrderStatus.Shipped ||
                            order.Status == OrderStatus.Delivered) ||
                           (order.Status == OrderStatus.Confirmed && order.PaymentStatus == PaymentStatus.Completed);
            }

            if (!canPrint)
            {
                TempData["error"] = _localizer["ShippingSlipOnlyForConfirmedOrders"].Value;
                return RedirectToAction("Details", new { id });
            }

            // Filter order items to only show seller's products
            var sellerOrderItems = order.OrderItems.Where(oi => oi.Product?.SellerId == seller.Id).ToList();

            ViewBag.Seller = seller;
            ViewBag.SellerOrderItems = sellerOrderItems;
            ViewBag.SiteSettings = await _context.SiteSettings.FirstOrDefaultAsync();

            return View(order);
        }
    }
}
