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
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IShopRatingService _shopRatingService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IShopRatingService shopRatingService,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _shopRatingService = shopRatingService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            // Get statistics
            var productCount = await _context.Products
                .CountAsync(p => p.SellerId == seller.Id);

            var orderCount = await _context.OrderItems
                .Where(oi => oi.SellerId == seller.Id)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            var totalRevenue = await _context.OrderItems
                .Where(oi => oi.SellerId == seller.Id && oi.Order != null && oi.Order.Status == OrderStatus.Delivered)
                .SumAsync(oi => oi.TotalPrice);

            var pendingOrders = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.SellerId == seller.Id && oi.Order != null &&
                       (oi.Order.Status == OrderStatus.Pending || oi.Order.Status == OrderStatus.Processing))
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            // Recent orders
            var recentOrders = await _context.OrderItems
                .Include(oi => oi.Order)
                    .ThenInclude(o => o!.User)
                .Include(oi => oi.Product)
                .Where(oi => oi.SellerId == seller.Id)
                .OrderByDescending(oi => oi.Order!.OrderDate)
                .Take(5)
                .ToListAsync();

            // Get shop rating
            var (rating, reviewCount) = await _shopRatingService.GetShopRatingAsync(seller.Id);

            // Low stock products
            var lowStockProducts = await _context.Products
                .Where(p => p.SellerId == seller.Id && p.Stock > 0 && p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .Take(5)
                .ToListAsync();

            // Sales by month (last 6 months)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var monthlySales = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.SellerId == seller.Id &&
                       oi.Order != null &&
                       oi.Order.Status == OrderStatus.Delivered &&
                       oi.Order.OrderDate >= sixMonthsAgo)
                .GroupBy(oi => new { oi.Order!.OrderDate.Year, oi.Order.OrderDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(oi => oi.TotalPrice)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // Daily sales (last 7 days)
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var dailySales = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.SellerId == seller.Id &&
                       oi.Order != null &&
                       oi.Order.Status == OrderStatus.Delivered &&
                       oi.Order.OrderDate >= sevenDaysAgo)
                .GroupBy(oi => oi.Order!.OrderDate.Date)
                .Select(g => new {
                    Date = g.Key,
                    Total = g.Sum(oi => oi.TotalPrice)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Weekly sales (last 4 weeks)
            var fourWeeksAgo = DateTime.UtcNow.AddDays(-28);
            var weeklySalesRaw = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.SellerId == seller.Id &&
                       oi.Order != null &&
                       oi.Order.Status == OrderStatus.Delivered &&
                       oi.Order.OrderDate >= fourWeeksAgo)
                .Select(oi => new { oi.Order!.OrderDate, oi.TotalPrice })
                .ToListAsync();

            var weeklySales = weeklySalesRaw
                .GroupBy(x => GetWeekNumber(x.OrderDate))
                .Select(g => new {
                    Week = g.Key,
                    Total = g.Sum(x => x.TotalPrice)
                })
                .OrderBy(x => x.Week)
                .ToList();

            // Order status breakdown
            var orderStatusBreakdown = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.SellerId == seller.Id && oi.Order != null)
                .GroupBy(oi => oi.Order!.Status)
                .Select(g => new {
                    Status = g.Key.ToString(),
                    Count = g.Select(oi => oi.OrderId).Distinct().Count()
                })
                .ToListAsync();

            // Account Balance and Recent Transactions
            var recentTransactions = await _context.SellerTransactions
                .Where(t => t.SellerId == seller.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.Seller = seller;
            ViewBag.ProductCount = productCount;
            ViewBag.OrderCount = orderCount;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.Rating = rating;
            ViewBag.ReviewCount = reviewCount;
            ViewBag.LowStockProducts = lowStockProducts;
            ViewBag.MonthlySales = monthlySales;
            ViewBag.DailySales = dailySales;
            ViewBag.WeeklySales = weeklySales;
            ViewBag.OrderStatusBreakdown = orderStatusBreakdown;
            ViewBag.AccountBalance = seller.AccountBalance;
            ViewBag.RecentTransactions = recentTransactions;
            ViewBag.CommissionRate = seller.CommissionRate;

            return View(seller);
        }

        // Helper method to get week number
        private static int GetWeekNumber(DateTime date)
        {
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }
    }
}
