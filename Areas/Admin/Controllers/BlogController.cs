using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class BlogController : Controller
    {
        private readonly IBlogService _blogService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BlogController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public BlogController(
            IBlogService blogService,
            IWebHostEnvironment environment,
            ILogger<BlogController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _blogService = blogService;
            _environment = environment;
            _logger = logger;
            _localizer = localizer;
        }

        #region Blog Posts

        public async Task<IActionResult> Index()
        {
            var posts = await _blogService.GetAllPostsAsync();
            return View(posts);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesDropdown();
            return View(new BlogPost());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost model, IFormFile? featuredImage, IFormFile? thumbnailImage)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesDropdown(model.BlogCategoryId);
                return View(model);
            }

            try
            {
                if (featuredImage != null && featuredImage.Length > 0)
                {
                    model.FeaturedImageUrl = await SaveFileAsync(featuredImage, "blog");
                }

                if (thumbnailImage != null && thumbnailImage.Length > 0)
                {
                    model.ThumbnailImageUrl = await SaveFileAsync(thumbnailImage, "blog");
                }

                await _blogService.CreatePostAsync(model);
                TempData["success"] = _localizer["BlogPostCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog post");
                TempData["error"] = _localizer["FailedToCreateBlogPost"].Value;
                await PopulateCategoriesDropdown(model.BlogCategoryId);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var post = await _blogService.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            await PopulateCategoriesDropdown(post.BlogCategoryId);
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BlogPost model, IFormFile? featuredImage, IFormFile? thumbnailImage)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategoriesDropdown(model.BlogCategoryId);
                return View(model);
            }

            try
            {
                var existingPost = await _blogService.GetPostByIdAsync(model.Id);

                if (featuredImage != null && featuredImage.Length > 0)
                {
                    DeleteOldImage(existingPost?.FeaturedImageUrl);
                    model.FeaturedImageUrl = await SaveFileAsync(featuredImage, "blog");
                }
                else
                {
                    model.FeaturedImageUrl = existingPost?.FeaturedImageUrl;
                }

                if (thumbnailImage != null && thumbnailImage.Length > 0)
                {
                    DeleteOldImage(existingPost?.ThumbnailImageUrl);
                    model.ThumbnailImageUrl = await SaveFileAsync(thumbnailImage, "blog");
                }
                else
                {
                    model.ThumbnailImageUrl = existingPost?.ThumbnailImageUrl;
                }

                await _blogService.UpdatePostAsync(model);
                TempData["success"] = _localizer["BlogPostUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog post");
                TempData["error"] = _localizer["FailedToUpdateBlogPost"].Value;
                await PopulateCategoriesDropdown(model.BlogCategoryId);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var post = await _blogService.GetPostByIdAsync(id);
                DeleteOldImage(post?.FeaturedImageUrl);
                DeleteOldImage(post?.ThumbnailImageUrl);

                await _blogService.DeletePostAsync(id);
                TempData["success"] = _localizer["BlogPostDeletedSuccessfully"].Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blog post");
                TempData["error"] = _localizer["FailedToDeleteBlogPost"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Blog Categories

        public async Task<IActionResult> Categories()
        {
            var categories = await _blogService.GetAllCategoriesAsync();
            return View(categories);
        }

        public IActionResult CreateCategory()
        {
            return View(new BlogCategory());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(BlogCategory model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _blogService.CreateCategoryAsync(model);
                TempData["success"] = _localizer["BlogCategoryCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blog category");
                TempData["error"] = _localizer["FailedToCreateBlogCategory"].Value;
                return View(model);
            }
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _blogService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(BlogCategory model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _blogService.UpdateCategoryAsync(model);
                TempData["success"] = _localizer["BlogCategoryUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog category");
                TempData["error"] = _localizer["FailedToUpdateBlogCategory"].Value;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                await _blogService.DeleteCategoryAsync(id);
                TempData["success"] = _localizer["BlogCategoryDeletedSuccessfully"].Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blog category");
                TempData["error"] = _localizer["FailedToDeleteBlogCategory"].Value;
            }

            return RedirectToAction(nameof(Categories));
        }

        #endregion

        #region Helpers

        private async Task PopulateCategoriesDropdown(int? selectedId = null)
        {
            var categories = await _blogService.GetAllCategoriesAsync();
            ViewBag.Categories = new SelectList(categories.Where(c => c.IsActive), "Id", "Name", selectedId);
        }

        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"images/{folder}/{uniqueFileName}";
        }

        private void DeleteOldImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old image: {ImageUrl}", imageUrl);
            }
        }

        #endregion
    }
}
