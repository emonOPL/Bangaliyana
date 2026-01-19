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

        private async Task<int?> GetCurrentSellerIdAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            return seller?.Id;
        }

        public async Task<IActionResult> Index(string? search, int? rating, int page = 1)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            const int pageSize = 15;

            // Get reviews for products belonging to this seller
            var reviews = _db.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .Include(r => r.Images)
                .Include(r => r.Reply)
                    .ThenInclude(rr => rr!.User)
                .Where(r => r.Product != null && r.Product.SellerId == sellerId)
                .AsQueryable();

            // Search filter - by product name or customer name
            if (!string.IsNullOrWhiteSpace(search))
            {
                reviews = reviews.Where(r =>
                    (r.Product != null && r.Product.Name.Contains(search)) ||
                    (r.User != null && r.User.FullName.Contains(search)) ||
                    (r.Comment != null && r.Comment.Contains(search)));
            }

            // Rating filter
            if (rating.HasValue && rating >= 1 && rating <= 5)
            {
                reviews = reviews.Where(r => r.Rating == rating);
            }

            var totalReviews = await reviews.CountAsync();
            var totalPages = (int)Math.Ceiling(totalReviews / (double)pageSize);

            var reviewList = await reviews
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate average rating for seller's products
            var avgRating = await _db.ProductReviews
                .Where(r => r.Product != null && r.Product.SellerId == sellerId)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            // Pagination data
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalReviews = totalReviews;
            ViewBag.Search = search;
            ViewBag.Rating = rating;
            ViewBag.AverageRating = avgRating;

            return View(reviewList);
        }

        public async Task<IActionResult> Details(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var review = await _db.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .Include(r => r.Order)
                .Include(r => r.Images)
                .Include(r => r.Reply)
                    .ThenInclude(rr => rr!.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.Product != null && r.Product.SellerId == sellerId);

            if (review == null)
            {
                TempData["Error"] = _localizer["ReviewNotFoundOrNoPermission"].Value;
                return RedirectToAction(nameof(Index));
            }

            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string reply)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            if (string.IsNullOrWhiteSpace(reply))
            {
                TempData["Error"] = _localizer["ReplyCannotBeEmpty"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            var review = await _db.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.Reply)
                .FirstOrDefaultAsync(r => r.Id == id && r.Product != null && r.Product.SellerId == sellerId);

            if (review == null)
            {
                TempData["Error"] = _localizer["ReviewNotFoundOrNoPermissionToReply"].Value;
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
                // Update existing reply only if it belongs to this seller
                if (review.Reply.UserId == userId)
                {
                    review.Reply.Reply = reply;
                    review.Reply.UpdatedAt = DateTime.UtcNow;
                    TempData["onEdit"] = _localizer["ReplyUpdatedSuccess"].Value;
                }
                else
                {
                    TempData["Error"] = _localizer["ReviewAlreadyHasReplyFromSomeoneElse"].Value;
                    return RedirectToAction(nameof(Details), new { id });
                }
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
                TempData["onSave"] = _localizer["ReplyAddedSuccess"].Value;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var userId = _userManager.GetUserId(User);

            var review = await _db.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.Reply)
                .FirstOrDefaultAsync(r => r.Id == id && r.Product != null && r.Product.SellerId == sellerId);

            if (review == null)
            {
                TempData["Error"] = _localizer["ReviewNotFoundOrNoPermission"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (review.Reply == null)
            {
                TempData["Error"] = _localizer["NoReplyFoundToDelete"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            // Only allow deleting own reply
            if (review.Reply.UserId != userId)
            {
                TempData["Error"] = _localizer["YouCanOnlyDeleteYourOwnReply"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            _db.ReviewReplies.Remove(review.Reply);
            await _db.SaveChangesAsync();

            TempData["onDelete"] = _localizer["ReplyDeletedSuccess"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
