using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get dashboard statistics
            var totalUsers = await _context.Users.CountAsync();
            var totalSellers = await _context.Sellers.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();

            var pendingSellers = await _context.Sellers
                .CountAsync(s => s.Status == SellerStatus.Pending);

            var pendingBankRequests = await _context.SellerBankAccountChangeRequests
                .CountAsync(r => r.Status == BankChangeRequestStatus.Pending);

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);

            var pendingReturns = await _context.Orders
                .CountAsync(o => o.ReturnReason != null && o.RefundStatus == RefundStatus.Pending);

            var pendingRefunds = await _context.Orders
                .CountAsync(o => o.RefundStatus == RefundStatus.Pending || o.RefundStatus == RefundStatus.InProgress);

            var openTickets = await _context.Tickets
                .CountAsync(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved);

            var unreadMessages = await _context.SellerMessages
                .CountAsync(m => !m.IsRead);

            // Revenue statistics
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.TotalAmount);

            var thisMonthRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered && o.CreatedAt.Month == DateTime.Now.Month && o.CreatedAt.Year == DateTime.Now.Year)
                .SumAsync(o => o.TotalAmount);

            // Get recent activities
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

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

            var recentProducts = await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Include(p => p.Category)
                .Select(p => new { p.Id, p.Name, p.Price, p.Status, CategoryName = p.Category != null ? p.Category.Name : "" })
                .ToListAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalSellers = totalSellers;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalCategories = totalCategories;
            ViewBag.PendingSellers = pendingSellers;
            ViewBag.PendingBankRequests = pendingBankRequests;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.PendingReturns = pendingReturns;
            ViewBag.PendingRefunds = pendingRefunds;
            ViewBag.OpenTickets = openTickets;
            ViewBag.UnreadMessages = unreadMessages;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.ThisMonthRevenue = thisMonthRevenue;
            ViewBag.RecentUsers = recentUsers;
            ViewBag.RecentSellers = recentSellers;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.RecentProducts = recentProducts;

            return View();
        }
    }
}
