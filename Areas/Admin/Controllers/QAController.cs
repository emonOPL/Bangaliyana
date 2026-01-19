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

        // GET: Admin/QA
        public async Task<IActionResult> Index(string? status = null, string? search = null)
        {
            var questionsQuery = _context.ProductQuestions
                .Include(q => q.Product)
                    .ThenInclude(p => p!.Seller)
                .Include(q => q.User)
                .Include(q => q.AnsweredBy)
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

            // Search by product name or question content
            if (!string.IsNullOrWhiteSpace(search))
            {
                filteredQuery = filteredQuery.Where(q =>
                    (q.Product != null && q.Product.Name.Contains(search)) ||
                    q.Question.Contains(search) ||
                    (q.Answer != null && q.Answer.Contains(search)));
            }

            var questions = await filteredQuery.ToListAsync();

            // Get counts for tabs
            ViewBag.AllCount = await questionsQuery.CountAsync();
            ViewBag.PendingCount = await questionsQuery.CountAsync(q => !q.IsAnswered);
            ViewBag.AnsweredCount = await questionsQuery.CountAsync(q => q.IsAnswered);
            ViewBag.CurrentStatus = status ?? "all";
            ViewBag.Search = search;

            return View(questions);
        }

        // GET: Admin/QA/Answer/5
        public async Task<IActionResult> Answer(int id)
        {
            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                    .ThenInclude(p => p!.Seller)
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                TempData["error"] = _localizer["QuestionNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            return View(question);
        }

        // POST: Admin/QA/Answer/5
        // Admin answer is automatically visible on product page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Answer(int id, string answer)
        {
            var user = await _userManager.GetUserAsync(User);

            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                TempData["error"] = _localizer["QuestionNotFound"].Value;
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

            TempData["success"] = _localizer["AnswerSubmittedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/QA/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                    .ThenInclude(p => p!.Seller)
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                TempData["error"] = _localizer["QuestionNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            return View(question);
        }

        // POST: Admin/QA/Edit/5 - Admin can edit both question and answer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string questionText, string? answer, bool isApproved)
        {
            var user = await _userManager.GetUserAsync(User);

            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                TempData["error"] = _localizer["QuestionNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(questionText))
            {
                TempData["error"] = _localizer["QuestionCannotBeEmpty"].Value;
                return RedirectToAction(nameof(Edit), new { id });
            }

            question.Question = questionText.Trim();
            question.IsApproved = isApproved;

            // Update answer if provided
            if (!string.IsNullOrWhiteSpace(answer))
            {
                question.Answer = answer.Trim();
                question.IsAnswered = true;
                question.AnsweredById = user?.Id;
                question.AnsweredAt = DateTime.UtcNow;
            }
            else if (question.IsAnswered)
            {
                // Keep existing answer if field is empty but was previously answered
            }

            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["QAUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/QA/DeleteAnswer/5 - Delete answer only (keep question)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnswer(int id)
        {
            var question = await _context.ProductQuestions
                .FirstOrDefaultAsync(q => q.Id == id && q.IsAnswered);

            if (question == null)
            {
                TempData["error"] = _localizer["QANotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Clear the answer but keep the question
            question.Answer = null;
            question.IsAnswered = false;
            question.IsApproved = false;
            question.AnsweredById = null;
            question.AnsweredAt = null;

            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["AnswerDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/QA/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.ProductQuestions
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                TempData["error"] = _localizer["QuestionNotFound"].Value;
                return RedirectToAction(nameof(Index));
            }

            _context.ProductQuestions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["QADeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/QA/ToggleApproval/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var question = await _context.ProductQuestions
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return Json(new { success = false, error = "Question not found" });
            }

            question.IsApproved = !question.IsApproved;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isApproved = question.IsApproved });
        }

        // POST: Admin/QA/BulkApprove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return Json(new { success = false, error = "No questions selected" });
            }

            var questions = await _context.ProductQuestions
                .Where(q => ids.Contains(q.Id))
                .ToListAsync();

            foreach (var q in questions)
            {
                q.IsApproved = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, count = questions.Count });
        }

        // POST: Admin/QA/BulkDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return Json(new { success = false, error = "No questions selected" });
            }

            var questions = await _context.ProductQuestions
                .Where(q => ids.Contains(q.Id))
                .ToListAsync();

            _context.ProductQuestions.RemoveRange(questions);
            await _context.SaveChangesAsync();

            return Json(new { success = true, count = questions.Count });
        }
    }
}
