using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISellerSubscriptionService _subscriptionService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SubscriptionController(ApplicationDbContext db, ISellerSubscriptionService subscriptionService, IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _subscriptionService = subscriptionService;
            _localizer = localizer;
        }

        // GET: Admin/Subscription - List all subscription plans
        public async Task<IActionResult> Index()
        {
            var plans = await _subscriptionService.GetAllPlansAsync(includeInactive: true);

            // Get statistics
            ViewBag.TotalSubscribers = await _subscriptionService.GetTotalSubscribersCountAsync();
            ViewBag.ActiveSubscribers = await _subscriptionService.GetActiveSubscribersCountAsync();
            ViewBag.MonthlyRevenue = await _subscriptionService.GetTotalSubscriptionRevenueAsync(
                DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
            ViewBag.SubscriptionsByPlan = await _subscriptionService.GetSubscriptionsByPlanAsync();

            return View(plans);
        }

        // GET: Admin/Subscription/Create
        public IActionResult Create()
        {
            var plan = new SellerSubscriptionPlan
            {
                CommissionRate = 5m,
                MaxProducts = 10,
                BadgeColor = "bg-primary",
                IconClass = "fas fa-star"
            };
            return View(plan);
        }

        // POST: Admin/Subscription/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SellerSubscriptionPlan plan)
        {
            if (ModelState.IsValid)
            {
                await _subscriptionService.CreatePlanAsync(plan);
                TempData["success"] = _localizer["SubscriptionPlanCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            return View(plan);
        }

        // GET: Admin/Subscription/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var plan = await _subscriptionService.GetPlanByIdAsync(id);
            if (plan == null)
            {
                TempData["error"] = _localizer["SubscriptionPlanNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }
            return View(plan);
        }

        // POST: Admin/Subscription/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SellerSubscriptionPlan plan)
        {
            if (id != plan.Id)
            {
                TempData["error"] = _localizer["InvalidPlanId"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                await _subscriptionService.UpdatePlanAsync(plan);
                TempData["success"] = _localizer["SubscriptionPlanUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            return View(plan);
        }

        // POST: Admin/Subscription/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _subscriptionService.DeletePlanAsync(id);
            if (result)
            {
                TempData["success"] = _localizer["SubscriptionPlanDeletedSuccessfully"].Value;
            }
            else
            {
                TempData["error"] = _localizer["CannotDeletePlanWithActiveSubscriptions"].Value;
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Subscription/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            await _subscriptionService.TogglePlanStatusAsync(id);
            TempData["success"] = _localizer["PlanStatusUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Subscription/Subscribers - View all subscribers
        public async Task<IActionResult> Subscribers(int? planId, string? status, int page = 1)
        {
            var query = _db.SellerSubscriptions
                .Include(s => s.Seller)
                .Include(s => s.Plan)
                .AsQueryable();

            if (planId.HasValue)
            {
                query = query.Where(s => s.PlanId == planId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<SubscriptionStatus>(status, out var subscriptionStatus))
                {
                    query = query.Where(s => s.Status == subscriptionStatus);
                }
            }

            int pageSize = 20;
            var totalSubscriptions = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalSubscriptions / (double)pageSize);

            var subscriptions = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Plans = await _subscriptionService.GetAllPlansAsync(includeInactive: true);
            ViewBag.CurrentPlanId = planId;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalSubscriptions;

            return View(subscriptions);
        }

        // GET: Admin/Subscription/SellerDetails/5 - View a seller's subscription history
        public async Task<IActionResult> SellerDetails(int id)
        {
            var seller = await _db.Sellers
                .Include(s => s.User)
                .Include(s => s.CurrentSubscription)
                    .ThenInclude(s => s!.Plan)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null)
            {
                TempData["error"] = _localizer["SellerNotFound"].Value;
                return RedirectToAction(nameof(Subscribers));
            }

            var history = await _subscriptionService.GetSellerSubscriptionHistoryAsync(id);
            var availablePlans = await _subscriptionService.GetAllPlansAsync();

            ViewBag.Seller = seller;
            ViewBag.AvailablePlans = availablePlans;
            return View(history);
        }

        // POST: Admin/Subscription/CancelSubscription/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSubscription(int id, string? reason)
        {
            var result = await _subscriptionService.CancelSubscriptionAsync(id, reason);
            if (result)
            {
                TempData["success"] = _localizer["SubscriptionCancelledSuccessfully"].Value;
            }
            else
            {
                TempData["error"] = _localizer["FailedToCancelSubscription"].Value;
            }
            return RedirectToAction(nameof(Subscribers));
        }

        // POST: Admin/Subscription/AssignPlan - Manually assign a plan to a seller
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPlan(int sellerId, int planId)
        {
            try
            {
                await _subscriptionService.CreateSubscriptionAsync(sellerId, planId, "Manual Assignment", "ADMIN-" + DateTime.UtcNow.Ticks);
                TempData["success"] = _localizer["PlanAssignedSuccessfully"].Value;
            }
            catch (Exception ex)
            {
                TempData["error"] = _localizer["FailedToAssignPlan"].Value + ": " + ex.Message;
            }
            return RedirectToAction(nameof(SellerDetails), new { id = sellerId });
        }

        // GET: Admin/Subscription/SeedPlans - Seed default subscription plans
        public async Task<IActionResult> SeedPlans()
        {
            var existingPlans = await _db.SellerSubscriptionPlans.AnyAsync();
            if (existingPlans)
            {
                TempData["info"] = _localizer["PlansAlreadyExist"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Seed default plans
            var plans = new List<SellerSubscriptionPlan>
            {
                new SellerSubscriptionPlan
                {
                    Name = "Basic",
                    Description = "Perfect for new sellers starting their journey",
                    MonthlyPrice = 0,
                    MaxProducts = 10,
                    CommissionRate = 5m,
                    HasPrioritySupport = false,
                    HasFeaturedListing = false,
                    HasAdvancedAnalytics = false,
                    HasPromotionalTools = false,
                    SortOrder = 1,
                    BadgeColor = "bg-secondary",
                    IconClass = "fas fa-seedling",
                    IsActive = true,
                    IsFeatured = false
                },
                new SellerSubscriptionPlan
                {
                    Name = "Standard",
                    Description = "Great for growing businesses with more products",
                    MonthlyPrice = 500,
                    MaxProducts = 50,
                    CommissionRate = 3m,
                    HasPrioritySupport = true,
                    HasFeaturedListing = false,
                    HasAdvancedAnalytics = true,
                    HasPromotionalTools = false,
                    SortOrder = 2,
                    BadgeColor = "bg-primary",
                    IconClass = "fas fa-rocket",
                    IsActive = true,
                    IsFeatured = true
                },
                new SellerSubscriptionPlan
                {
                    Name = "Premium",
                    Description = "Unlimited products and maximum exposure for serious sellers",
                    MonthlyPrice = 1500,
                    MaxProducts = -1, // Unlimited
                    CommissionRate = 1m,
                    HasPrioritySupport = true,
                    HasFeaturedListing = true,
                    HasAdvancedAnalytics = true,
                    HasPromotionalTools = true,
                    SortOrder = 3,
                    BadgeColor = "bg-warning",
                    IconClass = "fas fa-crown",
                    IsActive = true,
                    IsFeatured = false
                }
            };

            _db.SellerSubscriptionPlans.AddRange(plans);
            await _db.SaveChangesAsync();

            TempData["success"] = _localizer["DefaultPlansSeededSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }
    }
}
