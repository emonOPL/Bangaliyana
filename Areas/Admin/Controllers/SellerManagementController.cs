using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Bangaliyana.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class SellerManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SellerManagementController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IWebHostEnvironment webHostEnvironment,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _webHostEnvironment = webHostEnvironment;
            _localizer = localizer;
        }

        // GET: Admin/SellerManagement
        public async Task<IActionResult> Index(SellerStatus? status, string? search, int page = 1)
        {
            var query = _context.Sellers
                .Include(s => s.User)
                .Include(s => s.BusinessTypeEntity)
                .AsQueryable();

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(s =>
                    s.ShopName.ToLower().Contains(search) ||
                    (s.User != null && (s.User.Email ?? "").ToLower().Contains(search)) ||
                    (s.User != null && (s.User.FullName ?? "").ToLower().Contains(search)));
            }

            // Get counts for tabs
            ViewBag.AllCount = await _context.Sellers.CountAsync();
            ViewBag.PendingCount = await _context.Sellers.CountAsync(s => s.Status == SellerStatus.Pending);
            ViewBag.ApprovedCount = await _context.Sellers.CountAsync(s => s.Status == SellerStatus.Approved);
            ViewBag.SuspendedCount = await _context.Sellers.CountAsync(s => s.Status == SellerStatus.Suspended);
            ViewBag.RejectedCount = await _context.Sellers.CountAsync(s => s.Status == SellerStatus.Rejected);

            ViewBag.CurrentStatus = status;
            ViewBag.Search = search;

            var pageSize = 20;
            var totalItems = await query.CountAsync();
            var sellers = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get product counts for each seller
            var sellerIds = sellers.Select(s => s.Id).ToList();
            var productCounts = await _context.Products
                .Where(p => p.SellerId != null && sellerIds.Contains(p.SellerId.Value))
                .GroupBy(p => p.SellerId)
                .Select(g => new { SellerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SellerId!.Value, x => x.Count);

            ViewBag.ProductCounts = productCounts;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(sellers);
        }

        // GET: Admin/SellerManagement/Details/5
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

            // Get stats
            ViewBag.ProductCount = seller.Products.Count;
            ViewBag.OrderCount = await _context.OrderItems
                .Where(oi => oi.SellerId == id)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            ViewBag.TotalRevenue = await _context.OrderItems
                .Where(oi => oi.SellerId == id && oi.Order != null && oi.Order.Status == OrderStatus.Delivered)
                .SumAsync(oi => oi.TotalPrice);

            // Recent orders
            ViewBag.RecentOrders = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.SellerId == id)
                .OrderByDescending(oi => oi.Order!.OrderDate)
                .Take(10)
                .ToListAsync();

            return View(seller);
        }

        // GET: Admin/SellerManagement/Create
        public async Task<IActionResult> Create()
        {
            // Get users who don't have a seller account yet
            var existingSellerUserIds = await _context.Sellers.Select(s => s.UserId).ToListAsync();
            var availableUsers = await _context.Users
                .Where(u => !existingSellerUserIds.Contains(u.Id))
                .OrderBy(u => u.Email)
                .Take(100)
                .ToListAsync();

            ViewBag.AvailableUsers = availableUsers;
            return View();
        }

        // POST: Admin/SellerManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string userId, string shopName, BusinessCategory businessCategory,
            string? businessPhone, string? businessEmail, string? businessAddress, decimal commissionRate = 5m)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(shopName))
            {
                TempData["error"] = _localizer["UserAndShopNameRequired"].Value;
                return RedirectToAction(nameof(Create));
            }

            if (string.IsNullOrEmpty(businessPhone) || string.IsNullOrEmpty(businessEmail) || string.IsNullOrEmpty(businessAddress))
            {
                TempData["error"] = _localizer["PhoneEmailAddressRequired"].Value;
                return RedirectToAction(nameof(Create));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Create));
            }

            // Check if already a seller
            if (await _context.Sellers.AnyAsync(s => s.UserId == userId))
            {
                TempData["error"] = _localizer["UserAlreadyHasSellerAccount"].Value;
                return RedirectToAction(nameof(Create));
            }

            // Generate slug
            var slug = GenerateSlug(shopName);
            var slugBase = slug;
            var counter = 1;
            while (await _context.Sellers.AnyAsync(s => s.ShopSlug == slug))
            {
                slug = $"{slugBase}-{counter++}";
            }

            var seller = new Models.Seller
            {
                UserId = userId,
                ShopName = shopName,
                ShopSlug = slug,
                BusinessCategory = businessCategory,
                BusinessEmail = businessEmail,
                BusinessPhone = businessPhone,
                BusinessAddress = businessAddress,
                Status = SellerStatus.Approved,
                IsVerified = false,
                CommissionRate = commissionRate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ApprovedAt = DateTime.UtcNow
            };

            _context.Sellers.Add(seller);

            // Add Seller role if not already
            if (!await _userManager.IsInRoleAsync(user, "Seller"))
            {
                await _userManager.AddToRoleAsync(user, "Seller");
            }

            await _context.SaveChangesAsync();

            // Notify user
            await _notificationService.CreateNotificationAsync(new UserNotification
            {
                UserId = userId,
                Type = "seller",
                Icon = "fa-store",
                IconColor = "text-success",
                Title = "Seller Account Created",
                Message = $"Your seller account '{shopName}' has been created. You can now start adding products!",
                Link = "/Identity/Account/Manage/Shop"
            });

            TempData["success"] = _localizer["SellerAccountCreatedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id = seller.Id });
        }

        // GET: Admin/SellerManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .Include(s => s.BusinessTypeEntity)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                return NotFound();
            }

            // Load business types for dropdown
            ViewBag.BusinessTypes = await _context.BusinessTypes
                .Where(bt => bt.IsActive)
                .OrderBy(bt => bt.DisplayOrder)
                .ThenBy(bt => bt.Name)
                .ToListAsync();

            return View(seller);
        }

        // POST: Admin/SellerManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Models.Seller model)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null)
            {
                return NotFound();
            }

            // Check slug uniqueness
            if (!string.IsNullOrEmpty(model.ShopSlug))
            {
                var slugExists = await _context.Sellers
                    .AnyAsync(s => s.ShopSlug == model.ShopSlug && s.Id != id);
                if (slugExists)
                {
                    TempData["error"] = _localizer["ShopUrlAlreadyTaken"].Value;
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }

            // Update fields
            seller.ShopName = model.ShopName;
            seller.ShopSlug = model.ShopSlug;
            seller.ShopDescription = model.ShopDescription;
            seller.BusinessTypeId = model.BusinessTypeId;
            seller.BusinessAddress = model.BusinessAddress;
            seller.BusinessPhone = model.BusinessPhone;
            seller.BusinessEmail = model.BusinessEmail;
            seller.TradeLicenseNumber = model.TradeLicenseNumber;
            seller.NIDNumber = model.NIDNumber;
            seller.ShopPolicies = model.ShopPolicies;
            seller.ReturnPolicy = model.ReturnPolicy;
            seller.ShippingInfo = model.ShippingInfo;
            seller.OperatingHours = model.OperatingHours;
            seller.FacebookUrl = model.FacebookUrl;
            seller.InstagramUrl = model.InstagramUrl;
            seller.WhatsAppNumber = model.WhatsAppNumber;
            seller.CommissionRate = model.CommissionRate;
            seller.IsVerified = model.IsVerified;
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["SellerUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/SellerManagement/Approve/5
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
            seller.ApprovedAt = DateTime.UtcNow;
            seller.UpdatedAt = DateTime.UtcNow;
            seller.RejectionReason = null;

            // Add Seller role
            if (seller.User != null && !await _userManager.IsInRoleAsync(seller.User, "Seller"))
            {
                await _userManager.AddToRoleAsync(seller.User, "Seller");
            }

            await _context.SaveChangesAsync();

            // Notify seller
            await _notificationService.CreateNotificationAsync(new UserNotification
            {
                UserId = seller.UserId,
                Type = "seller",
                Icon = "fa-check-circle",
                IconColor = "text-success",
                Title = "Seller Application Approved!",
                Message = $"Congratulations! Your seller application for '{seller.ShopName}' has been approved. You can now start selling!",
                Link = "/Identity/Account/Manage/Shop"
            });

            // Return JSON for AJAX requests
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = true, message = _localizer["SellerApprovedSuccessfully"].Value });
            }

            TempData["success"] = _localizer["SellerApprovedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/SellerManagement/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null)
            {
                return NotFound();
            }

            seller.Status = SellerStatus.Rejected;
            seller.ApplicationStatus = ApplicationStatus.Rejected;
            seller.RejectionReason = reason;
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify seller
            await _notificationService.CreateNotificationAsync(new UserNotification
            {
                UserId = seller.UserId,
                Type = "seller",
                Icon = "fa-times-circle",
                IconColor = "text-danger",
                Title = "Seller Application Rejected",
                Message = $"Unfortunately, your seller application for '{seller.ShopName}' was not approved. Reason: {reason}",
                Link = "/Identity/Account/BecomeSeller"
            });

            TempData["success"] = _localizer["SellerRejectedSuccessfully"].Value;
            return RedirectToAction(nameof(Index), new { status = SellerStatus.Pending });
        }

        // POST: Admin/SellerManagement/RequestModification/5
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

            await _context.SaveChangesAsync();

            // Notify seller
            await _notificationService.CreateNotificationAsync(new UserNotification
            {
                UserId = seller.UserId,
                Type = "seller",
                Icon = "fa-edit",
                IconColor = "text-warning",
                Title = "Application Modification Required",
                Message = $"Please review and update your seller application for '{seller.ShopName}'. Admin notes: {notes}",
                Link = "/Identity/Account/BecomeSeller"
            });

            TempData["success"] = _localizer["ModificationRequestSentSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/SellerManagement/Suspend/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id, string reason)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                return NotFound();
            }

            seller.Status = SellerStatus.Suspended;
            seller.RejectionReason = reason;
            seller.UpdatedAt = DateTime.UtcNow;

            // Remove Seller role
            if (seller.User != null && await _userManager.IsInRoleAsync(seller.User, "Seller"))
            {
                await _userManager.RemoveFromRoleAsync(seller.User, "Seller");
            }

            await _context.SaveChangesAsync();

            // Notify seller
            await _notificationService.CreateNotificationAsync(new UserNotification
            {
                UserId = seller.UserId,
                Type = "seller",
                Icon = "fa-ban",
                IconColor = "text-danger",
                Title = "Seller Account Suspended",
                Message = $"Your seller account '{seller.ShopName}' has been suspended. Reason: {reason}. Please contact support for more information.",
                Link = "/Identity/Account/Manage/SupportTickets"
            });

            TempData["success"] = _localizer["SellerSuspendedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/SellerManagement/Reactivate/5
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
            seller.RejectionReason = null;
            seller.UpdatedAt = DateTime.UtcNow;

            // Add Seller role back
            if (seller.User != null && !await _userManager.IsInRoleAsync(seller.User, "Seller"))
            {
                await _userManager.AddToRoleAsync(seller.User, "Seller");
            }

            await _context.SaveChangesAsync();

            // Notify seller
            await _notificationService.CreateNotificationAsync(new UserNotification
            {
                UserId = seller.UserId,
                Type = "seller",
                Icon = "fa-check-circle",
                IconColor = "text-success",
                Title = "Seller Account Reactivated",
                Message = $"Your seller account '{seller.ShopName}' has been reactivated. Welcome back!",
                Link = "/Identity/Account/Manage/Shop"
            });

            // Return JSON for AJAX requests
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = true, message = _localizer["SellerReactivatedSuccessfully"].Value });
            }

            TempData["success"] = _localizer["SellerReactivatedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/SellerManagement/UpdateCommission/5
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
                TempData["error"] = _localizer["CommissionRateMustBeBetween0And100"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            seller.CommissionRate = commissionRate;
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["CommissionRateUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/SellerManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                return NotFound();
            }

            // Remove Seller role
            if (seller.User != null && await _userManager.IsInRoleAsync(seller.User, "Seller"))
            {
                await _userManager.RemoveFromRoleAsync(seller.User, "Seller");
            }

            // Set products to no seller
            var products = await _context.Products.Where(p => p.SellerId == id).ToListAsync();
            foreach (var product in products)
            {
                product.SellerId = null;
            }

            _context.Sellers.Remove(seller);
            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["SellerDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // ============ BANK ACCOUNT CHANGE REQUESTS ============

        // GET: Admin/SellerManagement/BankRequests (Redirect to BankChangeRequests for backward compatibility)
        public IActionResult BankRequests(BankChangeRequestStatus? status = null)
        {
            return RedirectToAction(nameof(BankChangeRequests), new { status });
        }

        // GET: Admin/SellerManagement/BankChangeRequests
        public async Task<IActionResult> BankChangeRequests(BankChangeRequestStatus? status = null)
        {
            var query = _context.SellerBankAccountChangeRequests
                .Include(r => r.Seller)
                .Include(r => r.ExistingBankAccount)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            ViewBag.PendingCount = await _context.SellerBankAccountChangeRequests.CountAsync(r => r.Status == BankChangeRequestStatus.Pending);
            ViewBag.ApprovedCount = await _context.SellerBankAccountChangeRequests.CountAsync(r => r.Status == BankChangeRequestStatus.Approved);
            ViewBag.RejectedCount = await _context.SellerBankAccountChangeRequests.CountAsync(r => r.Status == BankChangeRequestStatus.Rejected);
            ViewBag.CurrentStatus = status;

            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        // GET: Admin/SellerManagement/BankChangeRequestDetails/5
        public async Task<IActionResult> BankChangeRequestDetails(int id)
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

            return View(request);
        }

        // POST: Admin/SellerManagement/ApproveBankChange/5
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

            var currentUserId = _userManager.GetUserId(User);

            // Update or create the bank account
            SellerBankAccount bankAccount;
            if (request.ExistingBankAccountId.HasValue && request.ExistingBankAccount != null)
            {
                // Update existing
                bankAccount = request.ExistingBankAccount;
            }
            else
            {
                // Remove any existing bank accounts (enforce single account)
                var existingAccounts = await _context.SellerBankAccounts
                    .Where(b => b.SellerId == request.SellerId && b.AccountType == BankAccountType.Bank)
                    .ToListAsync();
                _context.SellerBankAccounts.RemoveRange(existingAccounts);

                // Create new
                bankAccount = new SellerBankAccount
                {
                    SellerId = request.SellerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SellerBankAccounts.Add(bankAccount);
            }

            // Apply new values
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

            // Update request status
            request.Status = BankChangeRequestStatus.Approved;
            request.AdminNotes = adminNotes;
            request.ReviewedByUserId = currentUserId;
            request.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify seller
            await _notificationService.CreateNotificationAsync(new UserNotification
            {
                UserId = request.Seller!.UserId,
                Type = "seller",
                Icon = "fa-check-circle",
                IconColor = "text-success",
                Title = "Bank Account Change Approved",
                Message = "Your bank account change request has been approved and updated.",
                Link = "/Identity/Account/Manage/PaymentAccounts"
            });

            TempData["success"] = _localizer["BankChangeRequestApprovedSuccessfully"].Value;
            return RedirectToAction(nameof(BankChangeRequests));
        }

        // POST: Admin/SellerManagement/RejectBankChange/5
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

            var currentUserId = _userManager.GetUserId(User);

            request.Status = BankChangeRequestStatus.Rejected;
            request.AdminNotes = adminNotes;
            request.ReviewedByUserId = currentUserId;
            request.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify seller
            await _notificationService.CreateNotificationAsync(new UserNotification
            {
                UserId = request.Seller!.UserId,
                Type = "seller",
                Icon = "fa-times-circle",
                IconColor = "text-danger",
                Title = "Bank Account Change Rejected",
                Message = $"Your bank account change request was rejected. Reason: {adminNotes}",
                Link = "/Identity/Account/Manage/PaymentAccounts"
            });

            TempData["success"] = _localizer["BankChangeRequestRejectedSuccessfully"].Value;
            return RedirectToAction(nameof(BankChangeRequests));
        }

        private string GenerateSlug(string name)
        {
            return name.ToLower()
                .Replace(" ", "-")
                .Replace("@", "-")
                .Replace(".", "-")
                .Replace("_", "-")
                .Replace("--", "-")
                .Trim('-');
        }
    }
}
