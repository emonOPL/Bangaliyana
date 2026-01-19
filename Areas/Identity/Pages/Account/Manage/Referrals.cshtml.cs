using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Bangaliyana.Models;
using Bangaliyana.Services;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public class ReferralsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRewardService _rewardService;

        public ReferralsModel(UserManager<ApplicationUser> userManager, IRewardService rewardService)
        {
            _userManager = userManager;
            _rewardService = rewardService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ReferralCode { get; set; } = string.Empty;
        public string ReferralLink { get; set; } = string.Empty;
        public bool HasUsedReferralCode { get; set; }
        public int TotalReferrals { get; set; }
        public int TotalReferralPointsEarned { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [StringLength(8, MinimumLength = 8, ErrorMessage = "Referral code must be exactly 8 characters.")]
            [Display(Name = "Referral Code")]
            public string ReferralCode { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadDataAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostApplyCodeAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (string.IsNullOrWhiteSpace(Input.ReferralCode))
            {
                StatusMessage = "Error: Please enter a referral code.";
                await LoadDataAsync(user);
                return Page();
            }

            if (user.HasUsedReferralCode)
            {
                StatusMessage = "Error: You have already used a referral code.";
                await LoadDataAsync(user);
                return Page();
            }

            var result = await _rewardService.ApplyReferralCodeAsync(user.Id, Input.ReferralCode);

            if (result.Success)
            {
                StatusMessage = result.Message;
            }
            else
            {
                StatusMessage = $"Error: {result.Message}";
            }

            await LoadDataAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateCodeAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!string.IsNullOrEmpty(user.ReferralCode))
            {
                StatusMessage = "You already have a referral code.";
                await LoadDataAsync(user);
                return Page();
            }

            var code = await _rewardService.GenerateReferralCodeAsync(user.Id);

            if (!string.IsNullOrEmpty(code))
            {
                StatusMessage = "Your referral code has been generated!";
            }
            else
            {
                StatusMessage = "Error: Failed to generate referral code. Please try again.";
            }

            // Reload user to get updated data
            user = await _userManager.GetUserAsync(User);
            await LoadDataAsync(user!);
            return Page();
        }

        private async Task LoadDataAsync(ApplicationUser user)
        {
            ReferralCode = user.ReferralCode ?? string.Empty;
            HasUsedReferralCode = user.HasUsedReferralCode;

            if (!string.IsNullOrEmpty(ReferralCode))
            {
                ReferralLink = $"{Request.Scheme}://{Request.Host}/Identity/Account/Register?ref={ReferralCode}";
            }

            var stats = await _rewardService.GetReferralStatsAsync(user.Id);
            TotalReferrals = stats.TotalReferrals;
            TotalReferralPointsEarned = stats.TotalPointsEarned;
        }
    }
}
