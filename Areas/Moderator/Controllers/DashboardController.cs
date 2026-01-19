using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Moderator.Controllers
{
    [Area("Moderator")]
    [Authorize(Roles = "SuperAdmin,Admin,Moderator")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public DashboardController(ApplicationDbContext context, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            // Get dashboard statistics
            var pendingSellers = await _context.Sellers
                .CountAsync(s => s.Status == SellerStatus.Pending);

            var pendingBankRequests = await _context.SellerBankAccountChangeRequests
                .CountAsync(r => r.Status == BankChangeRequestStatus.Pending);

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);

            var pendingReturns = await _context.Orders
                .CountAsync(o => o.ReturnReason != null && o.RefundStatus == RefundStatus.Pending);

            // Only count orders that have actual return requests (ReturnReason != null)
            var pendingRefunds = await _context.Orders
                .CountAsync(o => o.ReturnReason != null && (o.RefundStatus == RefundStatus.Pending || o.RefundStatus == RefundStatus.InProgress || o.RefundStatus == RefundStatus.Reviewing || o.RefundStatus == RefundStatus.Approved));

            var openTickets = await _context.Tickets
                .CountAsync(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved);

            var unreadMessages = await _context.SellerMessages
                .CountAsync(m => !m.IsRead && !m.IsSentBySeller); // Only count unread messages from buyers

            // Get recent activities
            var recentSellers = await _context.Sellers
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Include(s => s.User)
                .ToListAsync();

            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Include(o => o.User)
                .ToListAsync();

            var recentTickets = await _context.Tickets
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Include(t => t.User)
                .ToListAsync();

            ViewBag.PendingSellers = pendingSellers;
            ViewBag.PendingBankRequests = pendingBankRequests;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.PendingReturns = pendingReturns;
            ViewBag.PendingRefunds = pendingRefunds;
            ViewBag.OpenTickets = openTickets;
            ViewBag.UnreadMessages = unreadMessages;
            ViewBag.RecentSellers = recentSellers;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.RecentTickets = recentTickets;

            return View();
        }
    }
}
