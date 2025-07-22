using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Mvc;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SpecialTagsController : Controller
    {
        private ApplicationDbContext _db;

        public SpecialTagsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var data = _db.SpecialTags.ToList();
            return View(data);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SpecialTags specialTags)
        {
            if (ModelState.IsValid) {
                _db.SpecialTags.Add(specialTags);
                await _db.SaveChangesAsync();
                TempData["onSave"] = "Special Tag has been created.";
                return RedirectToAction("Index");
            }
            return View(specialTags);
        }

        public IActionResult Edit(int? id) {
            if (id == null) { 
                return NotFound();
            }
            var specialTag = _db.SpecialTags.Find(id);
            if (specialTag == null) {
                return NotFound();
            }
            return View(specialTag);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SpecialTags specialTags)
        {
            if (ModelState.IsValid)
            {
                _db.Update(specialTags);
                await _db.SaveChangesAsync();
                TempData["onEdit"] = "Special Tag has been updated.";
                return RedirectToAction("Index");
            }
            return View(specialTags);
        }

        public IActionResult Details(int? id) {
            if (id == null) { 
                return NotFound();
            }
            var specialTags = _db.SpecialTags.Find(id);
            if (specialTags == null) { 
                return NotFound();
            }
            return View(specialTags);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int? id) {
            if (id == null) {
                return NotFound();
            }
            var specialTag = await _db.SpecialTags.FindAsync(id);
            if (specialTag == null) {
                return NotFound();
            }
            if (ModelState.IsValid) {
                _db.Remove(specialTag);
                await _db.SaveChangesAsync();
                TempData["onDelete"] = "Special Tag has been deleted.";
            }
            return RedirectToAction("Index"); 
        }
    }
}
