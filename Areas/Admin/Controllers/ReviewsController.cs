using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,Moderator")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ReviewsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(string? search, int? rating, string? status, int page = 1)
        {
            const int pageSize = 15;

            var reviews = _db.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .Include(r => r.Images)
                .Include(r => r.Reply)
                    .ThenInclude(rr => rr!.User)
                .AsQueryable();

            // Search filter - by product name or customer name/email
            if (!string.IsNullOrWhiteSpace(search))
            {
                reviews = reviews.Where(r =>
                    (r.Product != null && r.Product.Name.Contains(search)) ||
                    (r.User != null && (r.User.FullName.Contains(search) || r.User.Email!.Contains(search))) ||
                    (r.Comment != null && r.Comment.Contains(search)));
            }

            // Rating filter
            if (rating.HasValue && rating >= 1 && rating <= 5)
            {
                reviews = reviews.Where(r => r.Rating == rating);
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.ToLower())
                {
                    case "approved":
                        reviews = reviews.Where(r => r.IsApproved);
                        break;
                    case "pending":
                        reviews = reviews.Where(r => !r.IsApproved);
                        break;
                    case "hasreply":
                        reviews = reviews.Where(r => r.Reply != null);
                        break;
                    case "noreply":
                        reviews = reviews.Where(r => r.Reply == null);
                        break;
                }
            }

            var totalReviews = await reviews.CountAsync();
            var totalPages = (int)Math.Ceiling(totalReviews / (double)pageSize);

            var reviewList = await reviews
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pagination data
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalReviews = totalReviews;
            ViewBag.Search = search;
            ViewBag.Rating = rating;
            ViewBag.Status = status;

            return View(reviewList);
        }

        public async Task<IActionResult> Details(int id)
        {
            var review = await _db.ProductReviews
                .Include(r => r.Product)
                    .ThenInclude(p => p!.Seller)
                .Include(r => r.User)
                .Include(r => r.Order)
                .Include(r => r.Images)
                .Include(r => r.Reply)
                    .ThenInclude(rr => rr!.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                TempData["Error"] = _localizer["ReviewNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _db.ProductReviews.FindAsync(id);
            if (review == null)
            {
                TempData["Error"] = _localizer["ReviewNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            review.IsApproved = true;
            review.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["onEdit"] = _localizer["ReviewApprovedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var review = await _db.ProductReviews.FindAsync(id);
            if (review == null)
            {
                TempData["Error"] = _localizer["ReviewNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            review.IsApproved = false;
            review.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["onEdit"] = _localizer["ReviewRejectedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _db.ProductReviews
                .Include(r => r.Images)
                .Include(r => r.Reply)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                TempData["Error"] = _localizer["ReviewNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Delete review images
            if (review.Images != null && review.Images.Any())
            {
                _db.ReviewImages.RemoveRange(review.Images);
            }

            // Delete reply if exists
            if (review.Reply != null)
            {
                _db.ReviewReplies.Remove(review.Reply);
            }

            _db.ProductReviews.Remove(review);
            await _db.SaveChangesAsync();

            TempData["onDelete"] = _localizer["ReviewDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string reply)
        {
            if (string.IsNullOrWhiteSpace(reply))
            {
                TempData["Error"] = _localizer["ReplyCannotBeEmpty"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            var review = await _db.ProductReviews
                .Include(r => r.Reply)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                TempData["Error"] = _localizer["ReviewNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = _localizer["UserNotFound"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            if (review.Reply != null)
            {
                // Update existing reply
                review.Reply.Reply = reply;
                review.Reply.UpdatedAt = DateTime.UtcNow;
                TempData["onEdit"] = _localizer["ReplyUpdatedSuccessfully"].Value;
            }
            else
            {
                // Create new reply
                var reviewReply = new ReviewReply
                {
                    ProductReviewId = id,
                    UserId = userId,
                    Reply = reply,
                    CreatedAt = DateTime.UtcNow
                };
                _db.ReviewReplies.Add(reviewReply);
                TempData["onSave"] = _localizer["ReplyAddedSuccessfully"].Value;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var review = await _db.ProductReviews
                .Include(r => r.Reply)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                TempData["Error"] = _localizer["ReviewNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (review.Reply == null)
            {
                TempData["Error"] = _localizer["NoReplyFoundToDelete"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            _db.ReviewReplies.Remove(review.Reply);
            await _db.SaveChangesAsync();

            TempData["onDelete"] = _localizer["ReplyDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
