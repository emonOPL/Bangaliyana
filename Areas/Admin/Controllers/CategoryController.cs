using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Text.RegularExpressions;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public CategoryController(ApplicationDbContext db, IWebHostEnvironment env, IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _env = env;
            _localizer = localizer;
        }

        // GET: Admin/Category
        public async Task<IActionResult> Index()
        {
            // Get all root categories with their children loaded
            var categories = await _db.Categories
                .Include(c => c.Children)
                .Include(c => c.Products)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Build full paths for display
            foreach (var category in categories)
            {
                category.FullPath = await GetCategoryPathString(category.Id);
            }

            // Get only root categories for tree view
            var rootCategories = categories.Where(c => c.ParentId == null).ToList();
            ViewBag.AllCategories = categories;

            return View(rootCategories);
        }

        // GET: Admin/Category/Create
        public async Task<IActionResult> Create(int? parentId)
        {
            ViewBag.ParentCategories = await GetParentCategorySelectList();

            var category = new Category();
            if (parentId.HasValue)
            {
                var parent = await _db.Categories.FindAsync(parentId.Value);
                if (parent != null)
                {
                    category.ParentId = parentId;
                    category.Level = parent.Level + 1;
                }
            }

            return View(category);
        }

        // POST: Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, IFormFile? imageFile)
        {
            // Check for duplicate name at same level
            var existingCategory = await _db.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower()
                    && c.ParentId == category.ParentId);

            if (existingCategory != null)
            {
                ViewBag.ParentCategories = await GetParentCategorySelectList();
                ViewBag.onError = _localizer["CategoryNameAlreadyExists"].Value;
                return View(category);
            }

            if (ModelState.IsValid)
            {
                // Set level based on parent
                if (category.ParentId.HasValue)
                {
                    var parent = await _db.Categories.FindAsync(category.ParentId.Value);
                    if (parent != null)
                    {
                        category.Level = parent.Level + 1;
                    }
                }
                else
                {
                    category.Level = 0;
                }

                // Generate slug if not provided
                if (string.IsNullOrEmpty(category.Slug))
                {
                    category.Slug = GenerateSlug(category.Name);
                }

                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    category.ImageUrl = await SaveImageAsync(imageFile);
                }

                category.CreatedAt = DateTime.UtcNow;
                category.UpdatedAt = DateTime.UtcNow;

                _db.Categories.Add(category);
                await _db.SaveChangesAsync();

                TempData["onSave"] = _localizer["CategoryCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ParentCategories = await GetParentCategorySelectList();
            return View(category);
        }

        // GET: Admin/Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _db.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            // Get parent categories - only show categories from previous level
            // Level 0 categories can only have Root as parent (no categories shown)
            // Level 1 categories can only have Level 0 categories as parent
            // Level N categories can only have Level N-1 categories as parent
            var targetLevel = category.Level - 1;

            if (targetLevel >= 0)
            {
                ViewBag.ParentCategories = await _db.Categories
                    .Where(c => c.Level == targetLevel)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            else
            {
                ViewBag.ParentCategories = new List<Category>();
            }

            return View(category);
        }

        // POST: Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category, IFormFile? imageFile)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            // Check for duplicate name at same level (excluding current category)
            var existingCategory = await _db.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower()
                    && c.ParentId == category.ParentId
                    && c.Id != category.Id);

            if (existingCategory != null)
            {
                await SetParentCategoriesForEdit(category.Id);
                ViewBag.onError = _localizer["CategoryNameAlreadyExists"].Value;
                return View(category);
            }

            // Prevent setting parent to itself or its descendants
            if (category.ParentId.HasValue)
            {
                var descendantIds = await GetDescendantIds(category.Id);
                if (descendantIds.Contains(category.ParentId.Value) || category.ParentId.Value == category.Id)
                {
                    await SetParentCategoriesForEdit(category.Id);
                    ViewBag.onError = _localizer["CannotSetParentToSelf"].Value;
                    return View(category);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCat = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

                    // Update level based on new parent
                    if (category.ParentId.HasValue)
                    {
                        var parent = await _db.Categories.FindAsync(category.ParentId.Value);
                        if (parent != null)
                        {
                            category.Level = parent.Level + 1;
                        }
                    }
                    else
                    {
                        category.Level = 0;
                    }

                    // Generate slug if not provided
                    if (string.IsNullOrEmpty(category.Slug))
                    {
                        category.Slug = GenerateSlug(category.Name);
                    }

                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingCat?.ImageUrl))
                        {
                            DeleteImage(existingCat.ImageUrl);
                        }
                        category.ImageUrl = await SaveImageAsync(imageFile);
                    }
                    else
                    {
                        category.ImageUrl = existingCat?.ImageUrl;
                    }

                    category.CreatedAt = existingCat?.CreatedAt ?? DateTime.UtcNow;
                    category.UpdatedAt = DateTime.UtcNow;

                    _db.Update(category);
                    await _db.SaveChangesAsync();

                    // Update children levels recursively
                    await UpdateChildrenLevels(category.Id, category.Level);

                    TempData["onEdit"] = _localizer["CategoryUpdatedSuccessfully"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            await SetParentCategoriesForEdit(category.Id);
            return View(category);
        }

        // GET: Admin/Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _db.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            category.FullPath = await GetCategoryPathString(category.Id);
            ViewBag.ProductCount = await GetTotalProductCount(category.Id);

            return View(category);
        }

        // POST: Admin/Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories
                .Include(c => c.Children)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            // Check if category has children
            if (category.Children.Any())
            {
                TempData["onError"] = _localizer["CannotDeleteCategoryWithSubcategories"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Check if category has products
            if (category.Products.Any())
            {
                TempData["onError"] = _localizer["CannotDeleteCategoryWithProducts"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Delete image if exists
            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                DeleteImage(category.ImageUrl);
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["onDelete"] = _localizer["CategoryDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // AJAX: Get children of a category (for lazy loading)
        [HttpGet]
        public async Task<IActionResult> GetChildren(int parentId)
        {
            var children = await _db.Categories
                .Where(c => c.ParentId == parentId)
                .Include(c => c.Children)
                .Include(c => c.Products)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Level,
                    c.IsActive,
                    c.IsFeatured,
                    HasChildren = c.Children.Any(),
                    ProductCount = c.Products.Count,
                    c.ImageUrl,
                    c.IconClass
                })
                .ToListAsync();

            return Json(children);
        }

        // AJAX: Get categories by parent (for cascading dropdowns)
        [HttpGet]
        public async Task<IActionResult> GetCategoriesByParent(int? parentId)
        {
            var categories = await _db.Categories
                .Where(c => c.ParentId == parentId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    HasChildren = c.Children.Any(ch => ch.IsActive)
                })
                .ToListAsync();

            return Json(categories);
        }

        // AJAX: Get category path (for breadcrumbs)
        [HttpGet]
        public async Task<IActionResult> GetCategoryPath(int id)
        {
            var path = await GetCategoryPathList(id);
            return Json(path.Select(c => new { c.Id, c.Name }));
        }

        // AJAX: Move category to new parent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(int id, int? newParentId)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = _localizer["CategoryNotFound"].Value });
            }

            // Prevent moving to itself or its descendants
            if (newParentId.HasValue)
            {
                var descendantIds = await GetDescendantIds(id);
                if (descendantIds.Contains(newParentId.Value) || newParentId.Value == id)
                {
                    return Json(new { success = false, message = _localizer["CannotMoveCategoryToSelf"].Value });
                }
            }

            // Update parent and level
            category.ParentId = newParentId;
            if (newParentId.HasValue)
            {
                var parent = await _db.Categories.FindAsync(newParentId.Value);
                category.Level = (parent?.Level ?? 0) + 1;
            }
            else
            {
                category.Level = 0;
            }

            category.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Update children levels
            await UpdateChildrenLevels(id, category.Level);

            return Json(new { success = true, message = _localizer["CategoryMovedSuccessfully"].Value });
        }

        // AJAX: Toggle category active status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = _localizer["CategoryNotFound"].Value });
            }

            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, isActive = category.IsActive });
        }

        #region Helper Methods

        private async Task SetParentCategoriesForEdit(int categoryId)
        {
            var cat = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId);
            var targetLevel = (cat?.Level ?? 1) - 1;
            ViewBag.ParentCategories = targetLevel >= 0
                ? await _db.Categories.Where(c => c.Level == targetLevel).OrderBy(c => c.Name).ToListAsync()
                : new List<Category>();
        }

        private async Task<List<dynamic>> GetParentCategorySelectList()
        {
            return await _db.Categories
                .OrderBy(c => c.Level)
                .ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.Level })
                .Cast<dynamic>()
                .ToListAsync();
        }

        private async Task<List<int>> GetDescendantIds(int categoryId)
        {
            var ids = new List<int>();
            var children = await _db.Categories
                .Where(c => c.ParentId == categoryId)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var childId in children)
            {
                ids.Add(childId);
                ids.AddRange(await GetDescendantIds(childId));
            }

            return ids;
        }

        private async Task<List<Category>> GetCategoryPathList(int categoryId)
        {
            var path = new List<Category>();
            var category = await _db.Categories.FindAsync(categoryId);

            while (category != null)
            {
                path.Insert(0, category);
                category = category.ParentId.HasValue
                    ? await _db.Categories.FindAsync(category.ParentId)
                    : null;
            }

            return path;
        }

        private async Task<string> GetCategoryPathString(int categoryId)
        {
            var path = await GetCategoryPathList(categoryId);
            return string.Join(" > ", path.Select(c => c.Name));
        }

        private async Task<int> GetTotalProductCount(int categoryId)
        {
            var count = await _db.Products.CountAsync(p => p.CategoryId == categoryId);
            var children = await _db.Categories
                .Where(c => c.ParentId == categoryId)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var childId in children)
            {
                count += await GetTotalProductCount(childId);
            }

            return count;
        }

        private async Task UpdateChildrenLevels(int parentId, int parentLevel)
        {
            var children = await _db.Categories
                .Where(c => c.ParentId == parentId)
                .ToListAsync();

            foreach (var child in children)
            {
                child.Level = parentLevel + 1;
                child.UpdatedAt = DateTime.UtcNow;
                await UpdateChildrenLevels(child.Id, child.Level);
            }

            await _db.SaveChangesAsync();
        }

        private string GenerateSlug(string name)
        {
            var slug = name.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');
            return slug;
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "categories");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/images/categories/" + uniqueFileName;
        }

        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            var filePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        private bool CategoryExists(int id)
        {
            return _db.Categories.Any(c => c.Id == id);
        }

        #endregion
    }
}
