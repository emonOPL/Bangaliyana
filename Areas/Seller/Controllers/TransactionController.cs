using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "SuperAdmin,Seller,Admin")]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public TransactionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        private async Task<Models.Seller?> GetCurrentSellerAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            return await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
        }

        // GET: /Seller/Transaction
        public async Task<IActionResult> Index(string? type, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var transactionsQuery = _context.SellerTransactions
                .Include(t => t.Order)
                .Where(t => t.SellerId == seller.Id);

            // Filter by type
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<SellerTransactionType>(type, out var transactionType))
            {
                transactionsQuery = transactionsQuery.Where(t => t.Type == transactionType);
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                transactionsQuery = transactionsQuery.Where(t => t.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.AddDays(1);
                transactionsQuery = transactionsQuery.Where(t => t.CreatedAt < endDate);
            }

            // Pagination
            var pageSize = 20;
            var totalTransactions = await transactionsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalTransactions / pageSize);

            var transactions = await transactionsQuery
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate totals for summary
            var allTransactions = await _context.SellerTransactions
                .Where(t => t.SellerId == seller.Id)
                .ToListAsync();

            var totalCommissions = allTransactions
                .Where(t => t.Type == SellerTransactionType.Commission)
                .Sum(t => Math.Abs(t.Amount));

            var totalPayouts = allTransactions
                .Where(t => t.Type == SellerTransactionType.OrderPayout)
                .Sum(t => t.Amount);

            var totalRefunds = allTransactions
                .Where(t => t.Type == SellerTransactionType.RefundDeduction)
                .Sum(t => Math.Abs(t.Amount));

            var totalWithdrawals = allTransactions
                .Where(t => t.Type == SellerTransactionType.Withdrawal)
                .Sum(t => Math.Abs(t.Amount));

            ViewBag.Seller = seller;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalTransactions = totalTransactions;
            ViewBag.Type = type;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.TotalCommissions = totalCommissions;
            ViewBag.TotalPayouts = totalPayouts;
            ViewBag.TotalRefunds = totalRefunds;
            ViewBag.TotalWithdrawals = totalWithdrawals;

            return View(transactions);
        }
    }
}
