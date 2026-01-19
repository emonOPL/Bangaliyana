using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
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

        // GET: Customer/QA - List user's own questions
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var questions = await _context.ProductQuestions
                .Include(q => q.Product)
                .Include(q => q.AnsweredBy)
                .Where(q => q.UserId == user.Id)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return View(questions);
        }

        // GET: Customer/QA/Edit/5 - Edit own question (only if not answered yet)
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var question = await _context.ProductQuestions
                .Include(q => q.Product)
                .FirstOrDefaultAsync(q => q.Id == id && q.UserId == user.Id && !q.IsAnswered);

            if (question == null)
            {
                TempData["error"] = _localizer["QuestionNotFoundOrCannotBeEdited"].Value;
                return RedirectToAction(nameof(Index));
            }

            return View(question);
        }

        // POST: Customer/QA/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string questionText)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var question = await _context.ProductQuestions
                .FirstOrDefaultAsync(q => q.Id == id && q.UserId == user.Id && !q.IsAnswered);

            if (question == null)
            {
                TempData["error"] = _localizer["QuestionNotFoundOrCannotBeEdited"].Value;
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(questionText))
            {
                TempData["error"] = _localizer["QuestionCannotBeEmpty"].Value;
                return RedirectToAction(nameof(Edit), new { id });
            }

            question.Question = questionText.Trim();
            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["QuestionUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: Customer/QA/Delete/5 - Delete own question
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var question = await _context.ProductQuestions
                .FirstOrDefaultAsync(q => q.Id == id && q.UserId == user.Id);

            if (question == null)
            {
                TempData["error"] = _localizer["QuestionNotFoundOrNoPermission"].Value;
                return RedirectToAction(nameof(Index));
            }

            _context.ProductQuestions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["success"] = _localizer["QuestionDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }
    }
}
