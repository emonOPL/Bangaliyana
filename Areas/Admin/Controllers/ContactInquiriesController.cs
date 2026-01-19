using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Bangaliyana.Models;
using Bangaliyana.Services;
using System.Security.Claims;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminRole")]
    public class ContactInquiriesController : Controller
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ContactInquiriesController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ContactInquiriesController(
            ISiteSettingsService siteSettingsService,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            ILogger<ContactInquiriesController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _siteSettingsService = siteSettingsService;
            _emailService = emailService;
            _userManager = userManager;
            _logger = logger;
            _localizer = localizer;
        }

        /// <summary>
        /// List all contact inquiries
        /// </summary>
        public async Task<IActionResult> Index(InquiryStatus? status)
        {
            List<ContactInquiry> inquiries;

            if (status.HasValue)
            {
                inquiries = await _siteSettingsService.GetContactInquiriesByStatusAsync(status.Value);
                ViewBag.FilteredStatus = status.Value;
            }
            else
            {
                inquiries = await _siteSettingsService.GetAllContactInquiriesAsync();
            }

            // Get stats
            var allInquiries = await _siteSettingsService.GetAllContactInquiriesAsync();
            ViewBag.TotalCount = allInquiries.Count;
            ViewBag.NewCount = allInquiries.Count(i => i.Status == InquiryStatus.New);
            ViewBag.InProgressCount = allInquiries.Count(i => i.Status == InquiryStatus.InProgress);
            ViewBag.RespondedCount = allInquiries.Count(i => i.Status == InquiryStatus.Responded);
            ViewBag.ClosedCount = allInquiries.Count(i => i.Status == InquiryStatus.Closed);

            return View(inquiries);
        }

        /// <summary>
        /// View inquiry details
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var inquiry = await _siteSettingsService.GetContactInquiryByIdAsync(id);
            if (inquiry == null)
            {
                return NotFound();
            }

            // Mark as in progress if new
            if (inquiry.Status == InquiryStatus.New)
            {
                await _siteSettingsService.UpdateInquiryStatusAsync(id, InquiryStatus.InProgress);
                inquiry.Status = InquiryStatus.InProgress;
            }

            return View(inquiry);
        }

        /// <summary>
        /// Respond to inquiry
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(int id, string response, bool sendEmail)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                TempData["Error"] = _localizer["ResponseCannotBeEmpty"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var inquiry = await _siteSettingsService.GetContactInquiryByIdAsync(id);
            if (inquiry == null)
            {
                return NotFound();
            }

            await _siteSettingsService.RespondToInquiryAsync(id, response, userId);

            // Send email if requested
            if (sendEmail)
            {
                try
                {
                    var emailBody = $@"
                        <h2>Response to Your Inquiry</h2>
                        <p>Dear {inquiry.FullName},</p>
                        <p>Thank you for contacting us. Here is our response to your inquiry:</p>
                        <hr/>
                        <h4>Your Original Message:</h4>
                        <p><strong>Subject:</strong> {inquiry.Subject}</p>
                        <p>{inquiry.Message}</p>
                        <hr/>
                        <h4>Our Response:</h4>
                        <p>{response}</p>
                        <br/>
                        <p>If you have any further questions, please don't hesitate to contact us again.</p>
                        <p>Best regards,<br/>The Support Team</p>
                    ";

                    await _emailService.SendEmailAsync(inquiry.Email, "Re: " + inquiry.Subject, emailBody);
                    TempData["Success"] = _localizer["ResponseSavedAndEmailSent"].Value;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send response email for inquiry {Id}", id);
                    TempData["Warning"] = _localizer["ResponseSavedButEmailFailed"].Value;
                }
            }
            else
            {
                TempData["Success"] = _localizer["ResponseSavedSuccessfully"].Value;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Update inquiry status
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, InquiryStatus status)
        {
            await _siteSettingsService.UpdateInquiryStatusAsync(id, status);
            TempData["Success"] = _localizer["StatusUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Delete inquiry
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _siteSettingsService.DeleteContactInquiryAsync(id);
            TempData["Success"] = _localizer["InquiryDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }
    }
}
