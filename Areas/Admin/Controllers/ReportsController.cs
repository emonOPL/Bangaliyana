using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Reports Dashboard

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            // Default to last 30 days
            var endDate = toDate ?? DateTime.Now;
            var startDate = fromDate ?? endDate.AddDays(-30);

            // Overall statistics
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalSellers = await _context.Sellers.CountAsync();
            ViewBag.TotalProducts = await _context.Products.CountAsync();

            // Period statistics
            ViewBag.PeriodOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .CountAsync();

            ViewBag.PeriodRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.PeriodNewUsers = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .CountAsync();

            ViewBag.PeriodNewSellers = await _context.Sellers
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                .CountAsync();

            // Average order value
            var avgOrderValue = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status == OrderStatus.Delivered)
                .AverageAsync(o => (decimal?)o.TotalAmount) ?? 0;
            ViewBag.AvgOrderValue = avgOrderValue;

            // Chart data - Last 7 days revenue
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.Date.AddDays(-6 + i))
                .ToList();

            var revenueData = await _context.Orders
                .Where(o => o.CreatedAt >= last7Days.First() && o.Status == OrderStatus.Delivered)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            ViewBag.ChartLabels = last7Days.Select(d => d.ToString("dd MMM")).ToList();
            ViewBag.ChartData = last7Days
                .Select(d => revenueData.FirstOrDefault(r => r.Date == d)?.Revenue ?? 0)
                .ToList();

            ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = endDate.ToString("yyyy-MM-dd");

            return View();
        }

        #endregion

        #region Sales Report

        public async Task<IActionResult> SalesReport(DateTime? fromDate, DateTime? toDate, string groupBy = "day")
        {
            var endDate = toDate ?? DateTime.Now;
            var startDate = fromDate ?? endDate.AddDays(-30);

            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

            // Statistics
            var totalRevenue = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);
            var totalRefunds = orders.Where(o => o.RefundStatus == RefundStatus.Completed)
                .Sum(o => o.RefundPaidAmount ?? 0);
            var totalItemsSold = orders.Where(o => o.Status == OrderStatus.Delivered)
                .SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity);

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = orders.Count;
            ViewBag.DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered);
            ViewBag.CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);
            ViewBag.PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);
            ViewBag.AverageOrderValue = orders.Where(o => o.Status == OrderStatus.Delivered).Any()
                ? orders.Where(o => o.Status == OrderStatus.Delivered).Average(o => o.TotalAmount) : 0m;
            ViewBag.TotalRefunds = totalRefunds;
            ViewBag.NetRevenue = totalRevenue - totalRefunds;
            ViewBag.TotalItemsSold = totalItemsSold;

            // Group data for chart
            var groupedData = groupBy.ToLower() switch
            {
                "week" => orders.GroupBy(o => new { Year = o.CreatedAt.Year, Week = GetWeekOfYear(o.CreatedAt) })
                    .Select(g => new { Label = $"Week {g.Key.Week}, {g.Key.Year}", Revenue = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount), Orders = g.Count() })
                    .OrderBy(g => g.Label).ToList(),
                "month" => orders.GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new { Label = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}", Revenue = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount), Orders = g.Count() })
                    .OrderBy(g => g.Label).ToList(),
                _ => orders.GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new { Label = g.Key.ToString("dd MMM"), Revenue = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount), Orders = g.Count() })
                    .OrderBy(g => g.Label).ToList()
            };

            ViewBag.ChartLabels = groupedData.Select(g => g.Label).ToList();
            ViewBag.RevenueData = groupedData.Select(g => g.Revenue).ToList();
            ViewBag.OrdersData = groupedData.Select(g => g.Orders).ToList();

            // Sales data for table
            ViewBag.SalesData = groupedData.Select(g => new
            {
                Period = g.Label,
                Orders = g.Orders,
                Revenue = g.Revenue,
                Refunds = 0m, // Refunds per period would require more complex query
                NetRevenue = g.Revenue
            }).ToList();

            // Top selling products
            var topProducts = orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SelectMany(o => o.OrderItems.Select(oi => new { OrderId = o.Id, OrderItem = oi }))
                .GroupBy(x => new { x.OrderItem.ProductId, ProductName = x.OrderItem.Product?.Name ?? "Unknown" })
                .Select(g => new
                {
                    Name = g.Key.ProductName,
                    TotalSold = g.Sum(x => x.OrderItem.Quantity),
                    Revenue = g.Sum(x => x.OrderItem.TotalPrice),
                    OrderCount = g.Select(x => x.OrderId).Distinct().Count()
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToList();

            ViewBag.TopProducts = topProducts;
            ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = endDate.ToString("yyyy-MM-dd");
            ViewBag.GroupBy = groupBy;

            return View();
        }

        private int GetWeekOfYear(DateTime date)
        {
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }

        #endregion

        #region User Growth Report

        public async Task<IActionResult> UserGrowthReport(DateTime? fromDate, DateTime? toDate)
        {
            var endDate = toDate ?? DateTime.Now;
            var startDate = fromDate ?? endDate.AddDays(-30);

            // Overall user statistics
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalSellers = await _context.Sellers.CountAsync();
            ViewBag.PremiumMembers = await _context.Users.CountAsync(u => u.IsPremiumMember);

            // Period statistics
            var newUsers = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .CountAsync();
            ViewBag.NewUsers = newUsers;

            var newSellers = await _context.Sellers
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                .CountAsync();
            ViewBag.NewSellers = newSellers;

            // Active users (ordered in last 30 days)
            var activeUsers = await _context.Orders
                .Where(o => o.CreatedAt >= DateTime.Now.AddDays(-30))
                .Select(o => o.UserId)
                .Distinct()
                .CountAsync();
            ViewBag.ActiveUsers = activeUsers;

            // Chart data - User registrations by day
            var usersByDay = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            var sellersByDay = await _context.Sellers
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            var allDates = Enumerable.Range(0, (endDate - startDate).Days + 1)
                .Select(i => startDate.AddDays(i).Date)
                .ToList();

            ViewBag.ChartLabels = allDates.Select(d => d.ToString("dd MMM")).ToList();
            ViewBag.UsersData = allDates.Select(d => usersByDay.FirstOrDefault(u => u.Date == d)?.Count ?? 0).ToList();
            ViewBag.SellersData = allDates.Select(d => sellersByDay.FirstOrDefault(s => s.Date == d)?.Count ?? 0).ToList();

            // Recent users
            var recentUsers = await _context.Users
                .Include(u => u.Division)
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .ToListAsync();
            ViewBag.RecentUsers = recentUsers;

            // Seller user IDs for displaying seller badge
            var sellerUserIds = await _context.Sellers
                .Select(s => s.UserId)
                .ToListAsync();
            ViewBag.SellerUserIds = new HashSet<string>(sellerUserIds);
            ViewBag.TotalCustomers = ViewBag.TotalUsers - ViewBag.TotalSellers;

            // Monthly registration data for table
            var usersByMonth = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
                .ToListAsync();

            var sellerUserIdSet = new HashSet<string>(sellerUserIds);
            var sellersByMonth = await _context.Sellers
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                .GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
                .ToListAsync();

            // Build registration data with growth calculation
            var registrationData = new List<dynamic>();
            int? previousTotal = null;

            foreach (var userMonth in usersByMonth)
            {
                var sellerCount = sellersByMonth
                    .FirstOrDefault(s => s.Year == userMonth.Year && s.Month == userMonth.Month)?.Count ?? 0;
                var customerCount = userMonth.Count - sellerCount;
                if (customerCount < 0) customerCount = 0;
                var total = userMonth.Count;
                decimal? growth = previousTotal.HasValue && previousTotal > 0
                    ? ((decimal)(total - previousTotal.Value) / previousTotal.Value) * 100
                    : null;

                registrationData.Add(new
                {
                    Period = new DateTime(userMonth.Year, userMonth.Month, 1).ToString("MMM yyyy"),
                    Customers = customerCount,
                    Sellers = sellerCount,
                    Total = total,
                    Growth = growth
                });

                previousTotal = total;
            }

            ViewBag.RegistrationData = registrationData;

            ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = endDate.ToString("yyyy-MM-dd");

            return View();
        }

        #endregion

        #region Product Performance

        public async Task<IActionResult> ProductPerformance(DateTime? fromDate, DateTime? toDate, int? categoryId, string sortBy = "revenue")
        {
            var endDate = toDate ?? DateTime.Now;
            var startDate = fromDate ?? endDate.AddDays(-30);

            // Get all categories for filter
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Product statistics
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.ActiveProducts = await _context.Products.CountAsync(p => p.Status == ProductStatus.Active);
            ViewBag.LowStockProducts = await _context.Products.CountAsync(p => p.Stock > 0 && p.Stock < 10 && p.Status == ProductStatus.Active);
            ViewBag.OutOfStockProducts = await _context.Products.CountAsync(p => p.Stock == 0 && p.Status == ProductStatus.Active);

            // Product performance query
            var query = _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate &&
                             oi.Order.Status == OrderStatus.Delivered);

            if (categoryId.HasValue)
            {
                query = query.Where(oi => oi.Product.CategoryId == categoryId);
            }

            var productPerformance = await query
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.SKU, CategoryName = oi.Product.Category.Name, oi.Product.Stock, oi.Product.Status })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    Name = g.Key.Name,
                    SKU = g.Key.SKU,
                    CategoryName = g.Key.CategoryName,
                    Stock = g.Key.Stock,
                    IsActive = g.Key.Status == ProductStatus.Active,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    OrderCount = g.Select(oi => oi.OrderId).Distinct().Count()
                })
                .ToListAsync();

            // Sort
            var sortedProducts = sortBy.ToLower() switch
            {
                "sold" => productPerformance.OrderByDescending(p => p.TotalSold).ToList(),
                "orders" => productPerformance.OrderByDescending(p => p.OrderCount).ToList(),
                _ => productPerformance.OrderByDescending(p => p.Revenue).ToList()
            };

            ViewBag.BestSellers = sortedProducts.Take(20).ToList();
            ViewBag.TotalProductsSold = productPerformance.Sum(p => p.TotalSold);
            ViewBag.TotalRevenue = productPerformance.Sum(p => p.Revenue);
            ViewBag.UniqueProducts = productPerformance.Count;

            // Get sold product IDs for low performers query
            var soldProductIds = productPerformance.Select(p => p.ProductId).ToHashSet();

            // Low performing products (active products with no sales in period)
            ViewBag.LowPerformers = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Status == ProductStatus.Active && !soldProductIds.Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new
                {
                    p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    p.Price,
                    p.Stock,
                    Views = p.ViewCount,
                    p.CreatedAt
                })
                .ToListAsync();

            // Low stock items with sales data
            var lowStockProductsWithSales = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Stock < 10 && p.Status == ProductStatus.Active)
                .OrderBy(p => p.Stock)
                .Take(15)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    p.Stock
                })
                .ToListAsync();

            ViewBag.LowStockItems = lowStockProductsWithSales.Select(p => new
            {
                p.Name,
                p.CategoryName,
                p.Stock,
                TotalSold = productPerformance.FirstOrDefault(pp => pp.ProductId == p.Id)?.TotalSold ?? 0
            }).ToList();

            ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = endDate.ToString("yyyy-MM-dd");
            ViewBag.CategoryId = categoryId;
            ViewBag.SortBy = sortBy;

            return View();
        }

        #endregion

        #region Category Performance

        public async Task<IActionResult> CategoryPerformance(DateTime? fromDate, DateTime? toDate)
        {
            var endDate = toDate ?? DateTime.Now;
            var startDate = fromDate ?? endDate.AddDays(-30);

            // Category statistics
            ViewBag.TotalCategories = await _context.Categories.CountAsync();

            var categoryPerformance = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                        .ThenInclude(c => c.Parent)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate &&
                             oi.Order.Status == OrderStatus.Delivered && oi.Product.Category != null)
                .GroupBy(oi => new {
                    oi.Product.CategoryId,
                    CategoryName = oi.Product.Category.Name,
                    ParentName = oi.Product.Category.Parent != null ? oi.Product.Category.Parent.Name : null
                })
                .Select(g => new
                {
                    CategoryId = g.Key.CategoryId,
                    Name = g.Key.CategoryName,
                    ParentName = g.Key.ParentName,
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    ItemsSold = g.Sum(oi => oi.Quantity),
                    OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                    ProductCount = g.Select(oi => oi.ProductId).Distinct().Count()
                })
                .OrderByDescending(c => c.Revenue)
                .ToListAsync();

            // Calculate average price for each category
            var categoryData = categoryPerformance.Select(c => new
            {
                c.Name,
                c.ParentName,
                c.Revenue,
                c.ItemsSold,
                c.ProductCount,
                AveragePrice = c.ItemsSold > 0 ? c.Revenue / c.ItemsSold : 0m
            }).ToList();

            ViewBag.CategoryData = categoryData;
            ViewBag.TotalRevenue = categoryPerformance.Sum(c => c.Revenue);
            ViewBag.TotalItemsSold = categoryPerformance.Sum(c => c.ItemsSold);
            ViewBag.ActiveCategories = categoryPerformance.Count; // Categories with sales

            // Chart data
            ViewBag.ChartLabels = categoryPerformance.Take(10).Select(c => c.Name).ToList();
            ViewBag.ChartData = categoryPerformance.Take(10).Select(c => c.Revenue).ToList();

            // Category product counts
            var categoryCounts = await _context.Categories
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    ProductCount = c.Products.Count(p => p.Status == ProductStatus.Active)
                })
                .OrderByDescending(c => c.ProductCount)
                .ToListAsync();
            ViewBag.CategoryProductCounts = categoryCounts;

            ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = endDate.ToString("yyyy-MM-dd");

            return View();
        }

        #endregion

        #region Seller Performance

        public async Task<IActionResult> SellerPerformance(DateTime? fromDate, DateTime? toDate)
        {
            var endDate = toDate ?? DateTime.Now;
            var startDate = fromDate ?? endDate.AddDays(-30);

            // Seller statistics
            ViewBag.TotalSellers = await _context.Sellers.CountAsync();
            ViewBag.ActiveSellers = await _context.Sellers.CountAsync(s => s.Status == SellerStatus.Approved);

            // Get all seller IDs and their info for later use
            var sellerInfoDict = await _context.Sellers
                .Include(s => s.User)
                .Select(s => new
                {
                    s.Id,
                    s.ShopName,
                    OwnerName = s.User != null ? (s.User.FirstName + " " + s.User.LastName) : "Unknown",
                    s.Rating,
                    IsApproved = s.Status == SellerStatus.Approved
                })
                .ToDictionaryAsync(s => s.Id);

            var sellerPerformance = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Seller)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate &&
                             oi.Order.Status == OrderStatus.Delivered && oi.Product.Seller != null)
                .GroupBy(oi => oi.Product.SellerId)
                .Select(g => new
                {
                    SellerId = g.Key,
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    TotalSold = g.Sum(oi => oi.Quantity),
                    OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                    ProductCount = g.Select(oi => oi.ProductId).Distinct().Count()
                })
                .OrderByDescending(s => s.Revenue)
                .ToListAsync();

            // Combine with seller info
            var sellerData = sellerPerformance.Select(s =>
            {
                var sellerInfo = sellerInfoDict.TryGetValue(s.SellerId ?? 0, out var info) ? info : null;
                return new
                {
                    ShopName = sellerInfo?.ShopName ?? "Unknown",
                    OwnerName = sellerInfo?.OwnerName ?? "Unknown",
                    ProductCount = s.ProductCount,
                    OrderCount = s.OrderCount,
                    Revenue = s.Revenue,
                    Commission = s.Revenue * 0.10m,
                    Rating = sellerInfo?.Rating,
                    IsApproved = sellerInfo?.IsApproved ?? false
                };
            }).Take(50).ToList();

            ViewBag.SellerData = sellerData;
            ViewBag.TotalRevenue = sellerPerformance.Sum(s => s.Revenue);
            ViewBag.TotalOrders = sellerPerformance.Sum(s => s.OrderCount);

            // Commission calculation (assuming 10% commission)
            ViewBag.TotalCommissions = sellerPerformance.Sum(s => s.Revenue * 0.10m);

            // Average rating
            var avgRating = await _context.Sellers
                .Where(s => s.Rating > 0)
                .AverageAsync(s => (decimal?)s.Rating) ?? 0;
            ViewBag.AverageRating = avgRating;

            ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = endDate.ToString("yyyy-MM-dd");

            return View();
        }

        #endregion

        #region Export CSV

        public async Task<IActionResult> ExportCsv(string reportType, DateTime? fromDate, DateTime? toDate)
        {
            var endDate = toDate ?? DateTime.Now;
            var startDate = fromDate ?? endDate.AddDays(-30);
            var csv = new StringBuilder();

            switch (reportType.ToLower())
            {
                case "sales":
                    csv.AppendLine("Date,Order Number,Customer,Status,Amount");
                    var orders = await _context.Orders
                        .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                        .Include(o => o.User)
                        .OrderByDescending(o => o.CreatedAt)
                        .ToListAsync();
                    foreach (var o in orders)
                    {
                        csv.AppendLine($"{o.CreatedAt:yyyy-MM-dd},{o.OrderNumber},{o.User?.FirstName} {o.User?.LastName},{o.Status},{o.TotalAmount:N0}");
                    }
                    break;

                case "users":
                    csv.AppendLine("Name,Email,Phone,Registered Date,Premium,Orders Count");
                    var users = await _context.Users
                        .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                        .Select(u => new { u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.CreatedAt, u.IsPremiumMember, OrdersCount = u.Orders.Count })
                        .ToListAsync();
                    foreach (var u in users)
                    {
                        csv.AppendLine($"{u.FirstName} {u.LastName},{u.Email},{u.PhoneNumber},{u.CreatedAt:yyyy-MM-dd},{(u.IsPremiumMember ? "Yes" : "No")},{u.OrdersCount}");
                    }
                    break;

                case "products":
                    csv.AppendLine("Product,Category,Units Sold,Revenue,Stock,Status");
                    var products = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Include(oi => oi.Product).ThenInclude(p => p.Category)
                        .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate && oi.Order.Status == OrderStatus.Delivered)
                        .GroupBy(oi => new { oi.ProductId, oi.Product.Name, CategoryName = oi.Product.Category.Name, oi.Product.Stock, oi.Product.Status })
                        .Select(g => new { ProductName = g.Key.Name, CategoryName = g.Key.CategoryName, TotalSold = g.Sum(oi => oi.Quantity), TotalRevenue = g.Sum(oi => oi.TotalPrice), Stock = g.Key.Stock, Status = g.Key.Status })
                        .OrderByDescending(p => p.TotalRevenue)
                        .ToListAsync();
                    foreach (var p in products)
                    {
                        csv.AppendLine($"\"{p.ProductName}\",{p.CategoryName},{p.TotalSold},{p.TotalRevenue:N0},{p.Stock},{p.Status}");
                    }
                    break;

                case "categories":
                    csv.AppendLine("Category,Products Sold,Revenue,Orders");
                    var categories = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Include(oi => oi.Product).ThenInclude(p => p.Category)
                        .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate && oi.Order.Status == OrderStatus.Delivered && oi.Product.Category != null)
                        .GroupBy(oi => oi.Product.Category.Name)
                        .Select(g => new { CategoryName = g.Key, TotalSold = g.Sum(oi => oi.Quantity), TotalRevenue = g.Sum(oi => oi.TotalPrice), OrderCount = g.Select(oi => oi.OrderId).Distinct().Count() })
                        .OrderByDescending(c => c.TotalRevenue)
                        .ToListAsync();
                    foreach (var c in categories)
                    {
                        csv.AppendLine($"{c.CategoryName},{c.TotalSold},{c.TotalRevenue:N0},{c.OrderCount}");
                    }
                    break;

                case "sellers":
                    csv.AppendLine("Shop Name,Email,Orders,Revenue,Commission (10%)");
                    var sellers = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Include(oi => oi.Product).ThenInclude(p => p.Seller).ThenInclude(s => s.User)
                        .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate && oi.Order.Status == OrderStatus.Delivered && oi.Product.Seller != null)
                        .GroupBy(oi => new { oi.Product.Seller.ShopName, oi.Product.Seller.User.Email })
                        .Select(g => new { ShopName = g.Key.ShopName, Email = g.Key.Email, TotalRevenue = g.Sum(oi => oi.TotalPrice), OrderCount = g.Select(oi => oi.OrderId).Distinct().Count() })
                        .OrderByDescending(s => s.TotalRevenue)
                        .ToListAsync();
                    foreach (var s in sellers)
                    {
                        csv.AppendLine($"\"{s.ShopName}\",{s.Email},{s.OrderCount},{s.TotalRevenue:N0},{(s.TotalRevenue * 0.10m):N0}");
                    }
                    break;

                default:
                    return BadRequest("Invalid report type");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"{reportType}-report-{DateTime.Now:yyyyMMdd}.csv");
        }

        #endregion

        #region Export PDF

        public async Task<IActionResult> ExportPdf(string reportType, DateTime? fromDate, DateTime? toDate)
        {
            var endDate = toDate ?? DateTime.Now;
            var startDate = fromDate ?? endDate.AddDays(-30);

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(c => ReportHeader(c, reportType, startDate, endDate));
                    page.Content().Element(c => ReportContent(c, reportType, startDate, endDate).Wait());
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"{reportType}-report-{DateTime.Now:yyyyMMdd}.pdf");
        }

        private void ReportHeader(IContainer container, string reportType, DateTime startDate, DateTime endDate)
        {
            container.PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"{reportType.ToUpper()} REPORT").Bold().FontSize(18);
                    col.Item().Text($"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}").FontSize(10);
                    col.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
                row.ConstantItem(100).AlignRight().Text("BANGALIYANA").Bold().FontSize(14).FontColor(Colors.Green.Darken2);
            });
        }

        private async Task ReportContent(IContainer container, string reportType, DateTime startDate, DateTime endDate)
        {
            switch (reportType.ToLower())
            {
                case "sales":
                    await SalesReportContent(container, startDate, endDate);
                    break;
                case "users":
                    await UsersReportContent(container, startDate, endDate);
                    break;
                case "products":
                    await ProductsReportContent(container, startDate, endDate);
                    break;
                case "categories":
                    await CategoriesReportContent(container, startDate, endDate);
                    break;
                case "sellers":
                    await SellersReportContent(container, startDate, endDate);
                    break;
            }
        }

        private async Task SalesReportContent(IContainer container, DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .ToListAsync();

            container.Column(col =>
            {
                // Summary
                col.Item().PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Background(Colors.Green.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("Total Revenue").FontSize(10);
                        c.Item().Text($"৳{orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount):N0}").Bold().FontSize(16);
                    });
                    row.ConstantItem(10);
                    row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("Total Orders").FontSize(10);
                        c.Item().Text(orders.Count.ToString()).Bold().FontSize(16);
                    });
                    row.ConstantItem(10);
                    row.RelativeItem().Background(Colors.Orange.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("Avg Order Value").FontSize(10);
                        var avg = orders.Where(o => o.Status == OrderStatus.Delivered).Any() ? orders.Where(o => o.Status == OrderStatus.Delivered).Average(o => o.TotalAmount) : 0;
                        c.Item().Text($"৳{avg:N0}").Bold().FontSize(16);
                    });
                });

                // Status breakdown
                col.Item().Text("Order Status Breakdown").Bold().FontSize(12);
                col.Item().PaddingBottom(10).Text($"Delivered: {orders.Count(o => o.Status == OrderStatus.Delivered)} | Pending: {orders.Count(o => o.Status == OrderStatus.Pending)} | Cancelled: {orders.Count(o => o.Status == OrderStatus.Cancelled)}");
            });
        }

        private async Task UsersReportContent(IContainer container, DateTime startDate, DateTime endDate)
        {
            var newUsers = await _context.Users.CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate);
            var totalUsers = await _context.Users.CountAsync();
            var premiumUsers = await _context.Users.CountAsync(u => u.IsPremiumMember);

            container.Column(col =>
            {
                col.Item().PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("Total Users").FontSize(10);
                        c.Item().Text(totalUsers.ToString()).Bold().FontSize(16);
                    });
                    row.ConstantItem(10);
                    row.RelativeItem().Background(Colors.Green.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("New Users (Period)").FontSize(10);
                        c.Item().Text(newUsers.ToString()).Bold().FontSize(16);
                    });
                    row.ConstantItem(10);
                    row.RelativeItem().Background(Colors.Orange.Lighten4).Padding(10).Column(c =>
                    {
                        c.Item().Text("Premium Members").FontSize(10);
                        c.Item().Text(premiumUsers.ToString()).Bold().FontSize(16);
                    });
                });
            });
        }

        private async Task ProductsReportContent(IContainer container, DateTime startDate, DateTime endDate)
        {
            var products = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate && oi.Order.Status == OrderStatus.Delivered)
                .GroupBy(oi => oi.Product.Name)
                .Select(g => new { ProductName = g.Key, TotalSold = g.Sum(oi => oi.Quantity), TotalRevenue = g.Sum(oi => oi.TotalPrice) })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(20)
                .ToListAsync();

            container.Column(col =>
            {
                col.Item().Text("Top 20 Products by Revenue").Bold().FontSize(12);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Product").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Sold").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Revenue").Bold();
                    });

                    foreach (var p in products)
                    {
                        table.Cell().Padding(5).Text(p.ProductName ?? "Unknown");
                        table.Cell().Padding(5).Text(p.TotalSold.ToString());
                        table.Cell().Padding(5).Text($"৳{p.TotalRevenue:N0}");
                    }
                });
            });
        }

        private async Task CategoriesReportContent(IContainer container, DateTime startDate, DateTime endDate)
        {
            var categories = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product).ThenInclude(p => p.Category)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate && oi.Order.Status == OrderStatus.Delivered && oi.Product.Category != null)
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new { CategoryName = g.Key, TotalSold = g.Sum(oi => oi.Quantity), TotalRevenue = g.Sum(oi => oi.TotalPrice) })
                .OrderByDescending(c => c.TotalRevenue)
                .ToListAsync();

            container.Column(col =>
            {
                col.Item().Text("Category Performance").Bold().FontSize(12);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Category").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Sold").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Revenue").Bold();
                    });

                    foreach (var c in categories)
                    {
                        table.Cell().Padding(5).Text(c.CategoryName ?? "Unknown");
                        table.Cell().Padding(5).Text(c.TotalSold.ToString());
                        table.Cell().Padding(5).Text($"৳{c.TotalRevenue:N0}");
                    }
                });
            });
        }

        private async Task SellersReportContent(IContainer container, DateTime startDate, DateTime endDate)
        {
            var sellers = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product).ThenInclude(p => p.Seller)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate && oi.Order.Status == OrderStatus.Delivered && oi.Product.Seller != null)
                .GroupBy(oi => oi.Product.Seller.ShopName)
                .Select(g => new { ShopName = g.Key, TotalRevenue = g.Sum(oi => oi.TotalPrice), OrderCount = g.Select(oi => oi.OrderId).Distinct().Count() })
                .OrderByDescending(s => s.TotalRevenue)
                .Take(20)
                .ToListAsync();

            container.Column(col =>
            {
                col.Item().Text("Top 20 Sellers by Revenue").Bold().FontSize(12);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Shop").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Orders").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Revenue").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Commission").Bold();
                    });

                    foreach (var s in sellers)
                    {
                        table.Cell().Padding(5).Text(s.ShopName ?? "Unknown");
                        table.Cell().Padding(5).Text(s.OrderCount.ToString());
                        table.Cell().Padding(5).Text($"৳{s.TotalRevenue:N0}");
                        table.Cell().Padding(5).Text($"৳{(s.TotalRevenue * 0.10m):N0}");
                    }
                });
            });
        }

        #endregion

        #region Chart Data API

        [HttpGet]
        public async Task<IActionResult> GetChartData(string reportType, DateTime? fromDate, DateTime? toDate, int? categoryId = null, string groupBy = "day")
        {
            var endDate = toDate ?? DateTime.Now;
            var startDate = fromDate ?? endDate.AddDays(-30);

            switch (reportType.ToLower())
            {
                case "sales":
                    // Get orders for the period
                    var salesOrders = await _context.Orders
                        .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                        .ToListAsync();

                    // Group data based on groupBy parameter
                    var groupedSalesData = groupBy.ToLower() switch
                    {
                        "week" => salesOrders.GroupBy(o => new { Year = o.CreatedAt.Year, Week = GetWeekOfYear(o.CreatedAt) })
                            .Select(g => new { Label = $"Week {g.Key.Week}", Revenue = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount), Orders = g.Count() })
                            .OrderBy(g => g.Label).ToList(),
                        "month" => salesOrders.GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                            .Select(g => new { Label = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}", Revenue = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount), Orders = g.Count() })
                            .OrderBy(g => g.Label).ToList(),
                        "year" => salesOrders.GroupBy(o => o.CreatedAt.Year)
                            .Select(g => new { Label = g.Key.ToString(), Revenue = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount), Orders = g.Count() })
                            .OrderBy(g => g.Label).ToList(),
                        _ => salesOrders.GroupBy(o => o.CreatedAt.Date)
                            .Select(g => new { Label = g.Key.ToString("dd MMM"), Revenue = g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount), Orders = g.Count() })
                            .OrderBy(g => g.Label).ToList()
                    };

                    // Order status distribution
                    var statusCounts = new[]
                    {
                        salesOrders.Count(o => o.Status == OrderStatus.Pending),
                        salesOrders.Count(o => o.Status == OrderStatus.Processing),
                        salesOrders.Count(o => o.Status == OrderStatus.Shipped),
                        salesOrders.Count(o => o.Status == OrderStatus.Delivered),
                        salesOrders.Count(o => o.Status == OrderStatus.Cancelled)
                    };

                    // Payment methods distribution
                    var paymentCounts = salesOrders.GroupBy(o => o.PaymentMethod)
                        .Select(g => new { Method = g.Key.ToString(), Count = g.Count() })
                        .OrderByDescending(p => p.Count)
                        .ToList();

                    return Json(new
                    {
                        labels = groupedSalesData.Select(g => g.Label).ToArray(),
                        revenueData = groupedSalesData.Select(g => g.Revenue).ToArray(),
                        ordersData = groupedSalesData.Select(g => g.Orders).ToArray(),
                        statusLabels = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" },
                        statusData = statusCounts,
                        paymentLabels = paymentCounts.Select(p => p.Method).ToArray(),
                        paymentData = paymentCounts.Select(p => p.Count).ToArray()
                    });

                case "revenue":
                    var revenueByDay = await _context.Orders
                        .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status == OrderStatus.Delivered)
                        .GroupBy(o => o.CreatedAt.Date)
                        .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                        .OrderBy(g => g.Date)
                        .ToListAsync();

                    return Json(new
                    {
                        labels = revenueByDay.Select(r => r.Date.ToString("dd MMM")).ToArray(),
                        datasets = new[] { new { label = "Revenue", data = revenueByDay.Select(r => r.Revenue).ToArray(), borderColor = "#28a745", fill = false } }
                    });

                case "orders":
                    var ordersByDay = await _context.Orders
                        .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                        .GroupBy(o => o.CreatedAt.Date)
                        .Select(g => new { Date = g.Key, Count = g.Count() })
                        .OrderBy(g => g.Date)
                        .ToListAsync();

                    return Json(new
                    {
                        labels = ordersByDay.Select(o => o.Date.ToString("dd MMM")).ToArray(),
                        datasets = new[] { new { label = "Orders", data = ordersByDay.Select(o => o.Count).ToArray(), borderColor = "#007bff", fill = false } }
                    });

                case "users":
                    // User registration data by date
                    var allDates = Enumerable.Range(0, (endDate - startDate).Days + 1)
                        .Select(i => startDate.AddDays(i).Date)
                        .ToList();

                    var usersByDay = await _context.Users
                        .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                        .GroupBy(u => u.CreatedAt.Date)
                        .Select(g => new { Date = g.Key, Count = g.Count() })
                        .ToListAsync();

                    var sellersByDay = await _context.Sellers
                        .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
                        .GroupBy(s => s.CreatedAt.Date)
                        .Select(g => new { Date = g.Key, Count = g.Count() })
                        .ToListAsync();

                    // Division distribution (top 8)
                    var divisionData = await _context.Users
                        .Where(u => u.Division != null)
                        .GroupBy(u => u.Division.Name)
                        .Select(g => new { Division = g.Key, Count = g.Count() })
                        .OrderByDescending(d => d.Count)
                        .Take(8)
                        .ToListAsync();

                    // Gender distribution
                    var maleCount = await _context.Users.CountAsync(u => u.Gender == Gender.Male);
                    var femaleCount = await _context.Users.CountAsync(u => u.Gender == Gender.Female);
                    var otherCount = await _context.Users.CountAsync(u => u.Gender == Gender.Other || u.Gender == Gender.PreferNotToSay);
                    var notSpecifiedCount = await _context.Users.CountAsync(u => u.Gender == null);

                    return Json(new
                    {
                        labels = allDates.Select(d => d.ToString("dd MMM")).ToArray(),
                        customersData = allDates.Select(d =>
                        {
                            var users = usersByDay.FirstOrDefault(u => u.Date == d)?.Count ?? 0;
                            var sellers = sellersByDay.FirstOrDefault(s => s.Date == d)?.Count ?? 0;
                            return users - sellers > 0 ? users - sellers : 0;
                        }).ToArray(),
                        sellersData = allDates.Select(d => sellersByDay.FirstOrDefault(s => s.Date == d)?.Count ?? 0).ToArray(),
                        genderLabels = new[] { "Male", "Female", "Other", "Not Specified" },
                        genderData = new[] { maleCount, femaleCount, otherCount, notSpecifiedCount },
                        divisionLabels = divisionData.Select(d => d.Division).ToArray(),
                        divisionData = divisionData.Select(d => d.Count).ToArray()
                    });

                case "products":
                    // Product performance data
                    var productQuery = _context.OrderItems
                        .Include(oi => oi.Order)
                        .Include(oi => oi.Product)
                        .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate &&
                                     oi.Order.Status == OrderStatus.Delivered);

                    if (categoryId.HasValue)
                    {
                        productQuery = productQuery.Where(oi => oi.Product.CategoryId == categoryId);
                    }

                    var topProducts = await productQuery
                        .GroupBy(oi => oi.Product.Name)
                        .Select(g => new { Name = g.Key, Revenue = g.Sum(oi => oi.TotalPrice) })
                        .OrderByDescending(p => p.Revenue)
                        .Take(10)
                        .ToListAsync();

                    return Json(new
                    {
                        productLabels = topProducts.Select(p => p.Name).ToArray(),
                        productRevenue = topProducts.Select(p => p.Revenue).ToArray()
                    });

                case "categories":
                    // Category performance data
                    var categoryPerf = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Include(oi => oi.Product)
                            .ThenInclude(p => p.Category)
                        .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate &&
                                     oi.Order.Status == OrderStatus.Delivered && oi.Product.Category != null)
                        .GroupBy(oi => oi.Product.Category.Name)
                        .Select(g => new
                        {
                            Name = g.Key,
                            Revenue = g.Sum(oi => oi.TotalPrice),
                            Sales = g.Sum(oi => oi.Quantity)
                        })
                        .OrderByDescending(c => c.Revenue)
                        .Take(10)
                        .ToListAsync();

                    return Json(new
                    {
                        categoryLabels = categoryPerf.Select(c => c.Name).ToArray(),
                        categoryRevenue = categoryPerf.Select(c => c.Revenue).ToArray(),
                        categorySales = categoryPerf.Select(c => c.Sales).ToArray()
                    });

                case "sellers":
                    // Seller performance data
                    var sellerPerf = await _context.OrderItems
                        .Include(oi => oi.Order)
                        .Include(oi => oi.Product)
                            .ThenInclude(p => p.Seller)
                        .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate &&
                                     oi.Order.Status == OrderStatus.Delivered && oi.Product.Seller != null)
                        .GroupBy(oi => oi.Product.Seller.ShopName)
                        .Select(g => new
                        {
                            ShopName = g.Key,
                            Revenue = g.Sum(oi => oi.TotalPrice)
                        })
                        .OrderByDescending(s => s.Revenue)
                        .Take(10)
                        .ToListAsync();

                    return Json(new
                    {
                        sellerLabels = sellerPerf.Select(s => s.ShopName ?? "Unknown").ToArray(),
                        sellerRevenue = sellerPerf.Select(s => s.Revenue).ToArray(),
                        sellerCommission = sellerPerf.Select(s => s.Revenue * 0.10m).ToArray()
                    });

                default:
                    return BadRequest("Invalid report type");
            }
        }

        #endregion
    }
}
