using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class PromotionalCampaignsController : Controller
    {
        private readonly IPromotionalCampaignService _campaignService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public PromotionalCampaignsController(
            IPromotionalCampaignService campaignService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResources> localizer)
        {
            _campaignService = campaignService;
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var campaigns = await _campaignService.GetAllCampaignsAsync();
            return View(campaigns);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Coupons = new SelectList(
                await _context.Coupons
                    .Where(c => c.IsActive && c.EndDate > DateTime.UtcNow)
                    .OrderBy(c => c.Code)
                    .ToListAsync(),
                "Id",
                "Code"
            );
            ViewBag.TargetAudiences = GetTargetAudienceSelectList();
            return View(new PromotionalCampaign());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionalCampaign campaign)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                campaign.CreatedByUserId = userId!;
                campaign.Status = campaign.IsImmediate ? CampaignStatus.Draft : CampaignStatus.Scheduled;

                await _campaignService.CreateCampaignAsync(campaign);
                TempData["success"] = _localizer["CampaignCreatedSuccessfully"].Value;

                if (campaign.IsImmediate)
                {
                    return RedirectToAction(nameof(Send), new { id = campaign.Id });
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Coupons = new SelectList(
                await _context.Coupons
                    .Where(c => c.IsActive && c.EndDate > DateTime.UtcNow)
                    .OrderBy(c => c.Code)
                    .ToListAsync(),
                "Id",
                "Code"
            );
            ViewBag.TargetAudiences = GetTargetAudienceSelectList();
            return View(campaign);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var campaign = await _campaignService.GetCampaignAsync(id);
            if (campaign == null)
            {
                return NotFound();
            }

            if (campaign.Status == CampaignStatus.Sent)
            {
                TempData["error"] = _localizer["CannotEditSentCampaign"].Value;
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Coupons = new SelectList(
                await _context.Coupons
                    .Where(c => c.IsActive && c.EndDate > DateTime.UtcNow)
                    .OrderBy(c => c.Code)
                    .ToListAsync(),
                "Id",
                "Code",
                campaign.CouponId
            );
            ViewBag.TargetAudiences = GetTargetAudienceSelectList();
            return View(campaign);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PromotionalCampaign campaign)
        {
            var existingCampaign = await _context.PromotionalCampaigns.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == campaign.Id);

            if (existingCampaign == null)
            {
                return NotFound();
            }

            if (existingCampaign.Status == CampaignStatus.Sent)
            {
                TempData["error"] = _localizer["CannotEditSentCampaign"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                campaign.CreatedByUserId = existingCampaign.CreatedByUserId;
                campaign.CreatedAt = existingCampaign.CreatedAt;
                campaign.Status = existingCampaign.Status;

                await _campaignService.UpdateCampaignAsync(campaign);
                TempData["success"] = _localizer["CampaignUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Coupons = new SelectList(
                await _context.Coupons
                    .Where(c => c.IsActive && c.EndDate > DateTime.UtcNow)
                    .OrderBy(c => c.Code)
                    .ToListAsync(),
                "Id",
                "Code",
                campaign.CouponId
            );
            ViewBag.TargetAudiences = GetTargetAudienceSelectList();
            return View(campaign);
        }

        public async Task<IActionResult> Send(int id)
        {
            var campaign = await _campaignService.GetCampaignAsync(id);
            if (campaign == null)
            {
                return NotFound();
            }

            if (campaign.Status == CampaignStatus.Sent)
            {
                TempData["error"] = _localizer["CampaignAlreadySent"].Value;
                return RedirectToAction(nameof(Index));
            }

            var targetCount = await _campaignService.GetTargetUserCountAsync(
                campaign.TargetAudience,
                campaign.MinOrderCount,
                campaign.MinTotalSpent
            );

            ViewBag.TargetCount = targetCount;
            return View(campaign);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendConfirm(int id)
        {
            try
            {
                var recipientCount = await _campaignService.SendCampaignAsync(id);
                TempData["success"] = _localizer["CampaignSentSuccessfully"].Value;
            }
            catch (InvalidOperationException ex)
            {
                TempData["error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["error"] = _localizer["ErrorSendingCampaign"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Analytics(int id)
        {
            var campaign = await _campaignService.GetCampaignAsync(id);
            if (campaign == null)
            {
                return NotFound();
            }

            var analytics = await _campaignService.GetCampaignAnalyticsAsync(id);
            ViewBag.Campaign = campaign;
            return View(analytics);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var campaign = await _campaignService.GetCampaignAsync(id);
            if (campaign == null)
            {
                return NotFound();
            }

            if (campaign.Status == CampaignStatus.Sent)
            {
                TempData["error"] = _localizer["CannotDeleteSentCampaign"].Value;
                return RedirectToAction(nameof(Index));
            }

            await _campaignService.DeleteCampaignAsync(id);
            TempData["success"] = _localizer["CampaignDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetTargetCount(TargetAudience audience, int? minOrderCount, decimal? minTotalSpent)
        {
            var count = await _campaignService.GetTargetUserCountAsync(audience, minOrderCount, minTotalSpent);
            return Json(new { count });
        }

        private SelectList GetTargetAudienceSelectList()
        {
            var audiences = Enum.GetValues<TargetAudience>()
                .Select(a => new SelectListItem
                {
                    Value = ((int)a).ToString(),
                    Text = a switch
                    {
                        TargetAudience.All => "All Users",
                        TargetAudience.NewUsers => "New Users (Last 30 days)",
                        TargetAudience.ReturningUsers => "Returning Customers",
                        TargetAudience.PremiumMembers => "Premium Members",
                        TargetAudience.InactiveUsers => "Inactive Users (60+ days)",
                        TargetAudience.HighSpenders => "High Spenders",
                        _ => a.ToString()
                    }
                })
                .ToList();

            return new SelectList(audiences, "Value", "Text");
        }
    }
}
