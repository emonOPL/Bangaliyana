using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public class PlatformReviewModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IRewardService _rewardService;

        public PlatformReviewModel(UserManager<ApplicationUser> userManager, ApplicationDbContext db, IRewardService rewardService)
        {
            _userManager = userManager;
            _db = db;
            _rewardService = rewardService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public bool HasExistingReview { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "Please write something about us")]
            [StringLength(1000, MinimumLength = 10, ErrorMessage = "Review must be between 10 and 1000 characters")]
            [Display(Name = "Your Review")]
            public string ReviewText { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please select a rating")]
            [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
            [Display(Name = "Rating")]
            public int Rating { get; set; } = 5;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Check if user already has a user-submitted testimonial
            var existingReview = await _db.Testimonials
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsUserSubmitted);

            if (existingReview != null)
            {
                HasExistingReview = true;
                Input.ReviewText = existingReview.Content;
                Input.Rating = existingReview.Rating;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if user already has a user-submitted testimonial
            var existingReview = await _db.Testimonials
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsUserSubmitted);

            if (existingReview != null)
            {
                // Update existing testimonial
                existingReview.Content = Input.ReviewText;
                existingReview.Rating = Input.Rating;
                existingReview.UpdatedAt = DateTime.UtcNow;

                // Update cached user info
                existingReview.CustomerName = user.FullName ?? user.UserName ?? "Customer";
                existingReview.PhotoUrl = user.AvatarUrl;
                existingReview.IsVerifiedUser = user.IsVerified;

                await _db.SaveChangesAsync();
                StatusMessage = "Your review has been updated successfully!";
            }
            else
            {
                // Create new user-submitted testimonial
                var testimonial = new Testimonial
                {
                    UserId = user.Id,
                    CustomerName = user.FullName ?? user.UserName ?? "Customer",
                    CustomerLocation = user.Address,
                    PhotoUrl = user.AvatarUrl,
                    Content = Input.ReviewText,
                    Rating = Input.Rating,
                    IsActive = true,
                    IsApproved = true,
                    IsUserSubmitted = true,
                    IsVerifiedUser = user.IsVerified,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Testimonials.Add(testimonial);
                await _db.SaveChangesAsync();

                // Award platform review points for 3+ star reviews (only for new reviews)
                if (Input.Rating >= 3)
                {
                    await _rewardService.AwardPlatformReviewPointsAsync(testimonial.Id);
                }

                StatusMessage = "Thank you for your review!";
            }

            HasExistingReview = true;

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var existingReview = await _db.Testimonials
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsUserSubmitted);

            if (existingReview != null)
            {
                // First, remove or nullify related RewardPointsTransactions that reference this testimonial
                var relatedTransactions = await _db.RewardPointsTransactions
                    .Where(t => t.TestimonialId == existingReview.Id)
                    .ToListAsync();

                foreach (var transaction in relatedTransactions)
                {
                    transaction.TestimonialId = null;
                }

                _db.Testimonials.Remove(existingReview);
                await _db.SaveChangesAsync();
                StatusMessage = "Your review has been deleted.";
            }

            return RedirectToPage();
        }
    }
}
