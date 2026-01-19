using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Moderator.Controllers
{
    [Area("Moderator")]
    [Authorize(Roles = "SuperAdmin,Admin,Moderator")]
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SellerController(
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

        // GET: Moderator/Seller
        public async Task<IActionResult> Index(SellerStatus? status, string? search, int page = 1)
        {
            var query = _context.Sellers
                .Include(s => s.User)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(s =>
                    s.ShopName.ToLower().Contains(search) ||
                    (s.User != null && (s.User.Email ?? "").ToLower().Contains(search)) ||
                    (s.User != null && (s.User.FullName ?? "").ToLower().Contains(search)));
            }

            ViewBag.AllCount = await _context.Sellers.CountAsync();
            ViewBag.PendingCount = await _context.Sellers.CountAsync(s => s.Status == SellerStatus.Pending);
            ViewBag.ApprovedCount = await _context.Sellers.CountAsync(s => s.Status == SellerStatus.Approved);
            ViewBag.SuspendedCount = await _context.Sellers.CountAsync(s => s.Status == SellerStatus.Suspended);
            ViewBag.RejectedCount = await _context.Sellers.CountAsync(s => s.Status == SellerStatus.Rejected);

            ViewBag.CurrentStatus = status;
            ViewBag.Search = search;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Seller";

            var pageSize = 20;
            var totalItems = await query.CountAsync();
            var sellers = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var sellerIds = sellers.Select(s => s.Id).ToList();
            var productCounts = await _context.Products
                .Where(p => p.SellerId != null && sellerIds.Contains(p.SellerId.Value))
                .GroupBy(p => p.SellerId)
                .Select(g => new { SellerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SellerId!.Value, x => x.Count);

            ViewBag.ProductCounts = productCounts;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View("~/Areas/Admin/Views/SellerManagement/Index.cshtml", sellers);
        }

        // GET: Moderator/Seller/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .Include(s => s.Products)
                .Include(s => s.BankAccounts)
                .Include(s => s.BusinessTypeEntity)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                return NotFound();
            }

            ViewBag.ProductCount = await _context.Products.CountAsync(p => p.SellerId == id);
            ViewBag.OrderCount = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == id)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();
            ViewBag.TotalRevenue = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == id && oi.Order.Status == OrderStatus.Delivered)
                .SumAsync(oi => oi.TotalPrice);
            ViewBag.RecentOrders = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == id)
                .OrderByDescending(oi => oi.Order.CreatedAt)
                .Take(5)
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .ToListAsync();

            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Seller";

            return View("~/Areas/Admin/Views/SellerManagement/Details.cshtml", seller);
        }

        // POST: Moderator/Seller/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                return NotFound();
            }

            seller.Status = SellerStatus.Approved;
            seller.ApplicationStatus = ApplicationStatus.Approved;
            seller.ApprovedAt = DateTime.UtcNow;
            seller.UpdatedAt = DateTime.UtcNow;

            // Add Seller role to user
            if (seller.User != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(seller.User);
                if (!currentRoles.Contains("Seller"))
                {
                    await _userManager.AddToRoleAsync(seller.User, "Seller");
                    seller.User.Role = UserRole.Seller;
                    await _userManager.UpdateAsync(seller.User);
                }

                // Notify seller
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = seller.UserId,
                    Type = "seller",
                    Icon = "fa-check-circle",
                    IconColor = "text-success",
                    Title = "Seller Application Approved",
                    Message = $"Congratulations! Your seller application for '{seller.ShopName}' has been approved.",
                    Link = "/Seller/Dashboard"
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = _localizer["SellerApproved"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Moderator/Seller/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                return NotFound();
            }

            seller.Status = SellerStatus.Rejected;
            seller.ApplicationStatus = ApplicationStatus.Rejected;
            seller.RejectionReason = reason;
            seller.UpdatedAt = DateTime.UtcNow;

            if (seller.User != null)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = seller.UserId,
                    Type = "seller",
                    Icon = "fa-times-circle",
                    IconColor = "text-danger",
                    Title = "Seller Application Rejected",
                    Message = $"Your seller application for '{seller.ShopName}' has been rejected. {(string.IsNullOrEmpty(reason) ? "" : $"Reason: {reason}")}",
                    Link = "/Account/BecomeSeller"
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = _localizer["SellerRejected"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Moderator/Seller/RequestModification/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestModification(int id, string notes)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null)
            {
                return NotFound();
            }

            seller.ApplicationStatus = ApplicationStatus.ModificationRequired;
            seller.ModificationNotes = notes;
            seller.ModificationRequestedAt = DateTime.UtcNow;
            seller.UpdatedAt = DateTime.UtcNow;

            await _notificationService.CreateNotificationAsync(new UserNotification
            {
                UserId = seller.UserId,
                Type = "seller",
                Icon = "fa-edit",
                IconColor = "text-warning",
                Title = "Application Modification Required",
                Message = $"Your seller application needs modifications. Please review the notes and update your application.",
                Link = "/Account/BecomeSeller"
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = _localizer["ModificationRequestSent"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Moderator/Seller/Suspend/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id, string? reason)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                return NotFound();
            }

            seller.Status = SellerStatus.Suspended;
            seller.ApplicationStatus = ApplicationStatus.Suspended;
            seller.RejectionReason = reason;
            seller.UpdatedAt = DateTime.UtcNow;

            // Remove Seller role
            if (seller.User != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(seller.User);
                if (currentRoles.Contains("Seller"))
                {
                    await _userManager.RemoveFromRoleAsync(seller.User, "Seller");
                    seller.User.Role = UserRole.User;
                    await _userManager.UpdateAsync(seller.User);
                }

                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = seller.UserId,
                    Type = "seller",
                    Icon = "fa-ban",
                    IconColor = "text-warning",
                    Title = "Seller Account Suspended",
                    Message = $"Your seller account '{seller.ShopName}' has been suspended. {(string.IsNullOrEmpty(reason) ? "" : $"Reason: {reason}")}",
                    Link = "/Account/Manage"
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = _localizer["SellerSuspended"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Moderator/Seller/Reactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                return NotFound();
            }

            seller.Status = SellerStatus.Approved;
            seller.ApplicationStatus = ApplicationStatus.Approved;
            seller.RejectionReason = null;
            seller.UpdatedAt = DateTime.UtcNow;

            // Add Seller role back
            if (seller.User != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(seller.User);
                if (!currentRoles.Contains("Seller"))
                {
                    await _userManager.AddToRoleAsync(seller.User, "Seller");
                    seller.User.Role = UserRole.Seller;
                    await _userManager.UpdateAsync(seller.User);
                }

                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = seller.UserId,
                    Type = "seller",
                    Icon = "fa-check-circle",
                    IconColor = "text-success",
                    Title = "Seller Account Reactivated",
                    Message = $"Your seller account '{seller.ShopName}' has been reactivated. You can now continue selling.",
                    Link = "/Seller/Dashboard"
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = _localizer["SellerReactivated"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Moderator/Seller/BankRequests
        public async Task<IActionResult> BankRequests(BankChangeRequestStatus? status)
        {
            var query = _context.SellerBankAccountChangeRequests
                .Include(r => r.Seller)
                .Include(r => r.ExistingBankAccount)
                .Include(r => r.ReviewedBy)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            ViewBag.PendingCount = await _context.SellerBankAccountChangeRequests.CountAsync(r => r.Status == BankChangeRequestStatus.Pending);
            ViewBag.ApprovedCount = await _context.SellerBankAccountChangeRequests.CountAsync(r => r.Status == BankChangeRequestStatus.Approved);
            ViewBag.RejectedCount = await _context.SellerBankAccountChangeRequests.CountAsync(r => r.Status == BankChangeRequestStatus.Rejected);
            ViewBag.CurrentStatus = status;
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Seller";

            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return View("~/Areas/Admin/Views/SellerManagement/BankChangeRequests.cshtml", requests);
        }

        // GET: Moderator/Seller/BankRequestDetails/5
        public async Task<IActionResult> BankRequestDetails(int id)
        {
            var request = await _context.SellerBankAccountChangeRequests
                .Include(r => r.Seller)
                    .ThenInclude(s => s!.User)
                .Include(r => r.ExistingBankAccount)
                .Include(r => r.ReviewedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "Seller";

            return View("~/Areas/Admin/Views/SellerManagement/BankChangeRequestDetails.cshtml", request);
        }

        // POST: Moderator/Seller/ApproveBankChange/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBankChange(int id, string? adminNotes)
        {
            var request = await _context.SellerBankAccountChangeRequests
                .Include(r => r.Seller)
                .Include(r => r.ExistingBankAccount)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Update or create bank account
            SellerBankAccount bankAccount;
            if (request.ExistingBankAccountId.HasValue && request.ExistingBankAccount != null)
            {
                bankAccount = request.ExistingBankAccount;
            }
            else
            {
                bankAccount = new SellerBankAccount
                {
                    SellerId = request.SellerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SellerBankAccounts.Add(bankAccount);
            }

            bankAccount.AccountType = request.NewAccountType;
            bankAccount.BankName = request.NewBankName;
            bankAccount.BranchName = request.NewBranchName;
            bankAccount.AccountHolderName = request.NewAccountHolderName;
            bankAccount.AccountNumber = request.NewAccountNumber;
            bankAccount.RoutingNumber = request.NewRoutingNumber;
            bankAccount.CheckbookPhotoUrl = request.NewCheckbookPhotoUrl;
            bankAccount.IsPrimary = true;
            bankAccount.IsVerified = true;
            bankAccount.UpdatedAt = DateTime.UtcNow;

            // Remove primary flag from other accounts
            var otherAccounts = await _context.SellerBankAccounts
                .Where(a => a.SellerId == request.SellerId && a.Id != bankAccount.Id && a.AccountType == BankAccountType.Bank)
                .ToListAsync();
            _context.SellerBankAccounts.RemoveRange(otherAccounts);

            request.Status = BankChangeRequestStatus.Approved;
            request.AdminNotes = adminNotes;
            request.ReviewedByUserId = currentUser?.Id;
            request.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify seller
            if (request.Seller != null)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = request.Seller.UserId,
                    Type = "seller",
                    Icon = "fa-check-circle",
                    IconColor = "text-success",
                    Title = "Bank Account Change Approved",
                    Message = "Your bank account change request has been approved.",
                    Link = "/Account/Manage/PaymentAccounts"
                });
            }

            TempData["Success"] = _localizer["BankChangeApproved"].Value;
            return RedirectToAction(nameof(BankRequests));
        }

        // POST: Moderator/Seller/RejectBankChange/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBankChange(int id, string adminNotes)
        {
            var request = await _context.SellerBankAccountChangeRequests
                .Include(r => r.Seller)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            request.Status = BankChangeRequestStatus.Rejected;
            request.AdminNotes = adminNotes;
            request.ReviewedByUserId = currentUser?.Id;
            request.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify seller
            if (request.Seller != null)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = request.Seller.UserId,
                    Type = "seller",
                    Icon = "fa-times-circle",
                    IconColor = "text-danger",
                    Title = "Bank Account Change Rejected",
                    Message = $"Your bank account change request has been rejected. {(string.IsNullOrEmpty(adminNotes) ? "" : $"Reason: {adminNotes}")}",
                    Link = "/Account/Manage/PaymentAccounts"
                });
            }

            TempData["Success"] = _localizer["BankChangeRejected"].Value;
            return RedirectToAction(nameof(BankRequests));
        }

        // POST: Moderator/Seller/UpdateCommission/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCommission(int id, decimal commissionRate)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null)
            {
                return NotFound();
            }

            if (commissionRate < 0 || commissionRate > 100)
            {
                TempData["Error"] = _localizer["CommissionRateInvalid"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            seller.CommissionRate = commissionRate;
            seller.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = _localizer["CommissionRateUpdated"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
