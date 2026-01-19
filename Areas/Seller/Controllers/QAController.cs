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
    public class QAController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public QAController(
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == user.Id);
        }

        // GET: Seller/QA
        public async Task<IActionResult> Index(string? status = null)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            // Get questions for this seller's products only
            var questionsQuery = _context.ProductQuestions
                .Include(q => q.Product)
                .Include(q => q.User)
                .Include(q => q.AnsweredBy)
                .Where(q => q.Product != null && q.Product.SellerId == seller.Id)
                .OrderByDescending(q => q.CreatedAt);

            // Filter by status
            IQueryable<ProductQuestion> filteredQuery = status switch
            {
                "pending" => questionsQuery.Where(q => !q.IsAnswered),
                "answered" => questionsQuery.Where(q => q.IsAnswered),
                "approved" => questionsQuery.Where(q => q.IsApproved),
                "unapproved" => questionsQuery.Where(q => !q.IsApproved),
                _ => questionsQuery
            };

            var questions = await filteredQuery.ToListAsync();

            // Get counts for tabs
            ViewBag.AllCount = await questionsQuery.CountAsync();
            ViewBag.PendingCount = await questionsQuery.CountAsync(q => !q.IsAnswered);
            ViewBag.AnsweredCount = await questionsQuery.CountAsync(q => q.IsAnswered);
            ViewBag.CurrentStatus = status ?? "all";

            return View(questions);
        }

        // GET: Seller/QA/Answer/5
        public async Task<IActionResult> Answer(int id)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return NotFound();

            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.Id == id && q.Product != null && q.Product.SellerId == seller.Id);

            if (question == null)
            {
                TempData["error"] = _localizer["QuestionNotFoundOrNoAccess"].Value;
                return RedirectToAction(nameof(Index));
            }

            return View(question);
        }

        // POST: Seller/QA/Answer/5
        // When answered, automatically visible on product page (IsApproved = true)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Answer(int id, string answer)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .FirstOrDefaultAsync(q => q.Id == id && q.Product != null && q.Product.SellerId == seller.Id);

            if (question == null)
            {
                TempData["error"] = _localizer["QuestionNotFoundOrNoAccess"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(answer))
            {
                TempData["error"] = _localizer["AnswerCannotBeEmpty"].Value;
                return RedirectToAction(nameof(Answer), new { id });
            }

            question.Answer = answer.Trim();
            question.IsAnswered = true;
            question.IsApproved = true; // Automatically visible when answered
            question.AnsweredById = user?.Id;
            question.AnsweredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["AnswerSubmittedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        // GET: Seller/QA/Edit/5 - Edit answer only
        public async Task<IActionResult> Edit(int id)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return NotFound();

            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.Id == id && q.Product != null && q.Product.SellerId == seller.Id && q.IsAnswered);

            if (question == null)
            {
                TempData["error"] = _localizer["QANotFoundOrNoAccess"].Value;
                return RedirectToAction(nameof(Index));
            }

            return View(question);
        }

        // POST: Seller/QA/Edit/5 - Edit answer only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string answer)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .FirstOrDefaultAsync(q => q.Id == id && q.Product != null && q.Product.SellerId == seller.Id && q.IsAnswered);

            if (question == null)
            {
                TempData["error"] = _localizer["QANotFoundOrNoAccess"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(answer))
            {
                TempData["error"] = _localizer["AnswerCannotBeEmpty"].Value;
                return RedirectToAction(nameof(Edit), new { id });
            }

            question.Answer = answer.Trim();
            question.AnsweredById = user?.Id;
            question.AnsweredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["AnswerUpdatedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Seller/QA/DeleteAnswer/5 - Delete answer only (not the question)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnswer(int id)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return NotFound();

            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .FirstOrDefaultAsync(q => q.Id == id && q.Product != null && q.Product.SellerId == seller.Id && q.IsAnswered);

            if (question == null)
            {
                TempData["error"] = _localizer["QANotFoundOrNoAccess"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Clear the answer but keep the question
            question.Answer = null;
            question.IsAnswered = false;
            question.IsApproved = false;
            question.AnsweredById = null;
            question.AnsweredAt = null;

            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["AnswerDeletedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Seller/QA/ToggleApproval/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var seller = await GetCurrentSellerAsync();
            if (seller == null) return Json(new { success = false, error = "Not authorized" });

            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .FirstOrDefaultAsync(q => q.Id == id && q.Product != null && q.Product.SellerId == seller.Id && q.IsAnswered);

            if (question == null)
            {
                return Json(new { success = false, error = "Q&A not found" });
            }

            question.IsApproved = !question.IsApproved;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isApproved = question.IsApproved });
        }
    }
}
