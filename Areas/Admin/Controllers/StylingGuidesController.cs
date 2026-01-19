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
    public class StylingGuidesController : Controller
    {
        private readonly IStylingGuideService _stylingGuideService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<StylingGuidesController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public StylingGuidesController(
            IStylingGuideService stylingGuideService,
            IWebHostEnvironment environment,
            ILogger<StylingGuidesController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _stylingGuideService = stylingGuideService;
            _environment = environment;
            _logger = logger;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var guides = await _stylingGuideService.GetAllGuidesAsync();
            return View(guides);
        }

        public IActionResult Create()
        {
            PopulateGuideTypeDropdown();
            return View(new StylingGuide());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StylingGuide model, IFormFile? coverImage, IFormFile? thumbnailImage)
        {
            if (!ModelState.IsValid)
            {
                PopulateGuideTypeDropdown(model.GuideType);
                return View(model);
            }

            try
            {
                if (coverImage != null && coverImage.Length > 0)
                {
                    model.CoverImageUrl = await SaveFileAsync(coverImage, "styling-guides");
                }

                if (thumbnailImage != null && thumbnailImage.Length > 0)
                {
                    model.ThumbnailImageUrl = await SaveFileAsync(thumbnailImage, "styling-guides");
                }

                await _stylingGuideService.CreateGuideAsync(model);
                TempData["success"] = _localizer["StylingGuideCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating styling guide");
                TempData["error"] = _localizer["FailedToCreateStylingGuide"].Value;
                PopulateGuideTypeDropdown(model.GuideType);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var guide = await _stylingGuideService.GetGuideByIdAsync(id);
            if (guide == null)
            {
                return NotFound();
            }

            PopulateGuideTypeDropdown(guide.GuideType);
            return View(guide);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StylingGuide model, IFormFile? coverImage, IFormFile? thumbnailImage)
        {
            if (!ModelState.IsValid)
            {
                PopulateGuideTypeDropdown(model.GuideType);
                return View(model);
            }

            try
            {
                var existingGuide = await _stylingGuideService.GetGuideByIdAsync(model.Id);

                if (coverImage != null && coverImage.Length > 0)
                {
                    DeleteOldImage(existingGuide?.CoverImageUrl);
                    model.CoverImageUrl = await SaveFileAsync(coverImage, "styling-guides");
                }
                else
                {
                    model.CoverImageUrl = existingGuide?.CoverImageUrl;
                }

                if (thumbnailImage != null && thumbnailImage.Length > 0)
                {
                    DeleteOldImage(existingGuide?.ThumbnailImageUrl);
                    model.ThumbnailImageUrl = await SaveFileAsync(thumbnailImage, "styling-guides");
                }
                else
                {
                    model.ThumbnailImageUrl = existingGuide?.ThumbnailImageUrl;
                }

                await _stylingGuideService.UpdateGuideAsync(model);
                TempData["success"] = _localizer["StylingGuideUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating styling guide");
                TempData["error"] = _localizer["FailedToUpdateStylingGuide"].Value;
                PopulateGuideTypeDropdown(model.GuideType);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var guide = await _stylingGuideService.GetGuideByIdAsync(id);
                DeleteOldImage(guide?.CoverImageUrl);
                DeleteOldImage(guide?.ThumbnailImageUrl);

                await _stylingGuideService.DeleteGuideAsync(id);
                TempData["success"] = _localizer["StylingGuideDeletedSuccessfully"].Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting styling guide");
                TempData["error"] = _localizer["FailedToDeleteStylingGuide"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        #region Helpers

        private void PopulateGuideTypeDropdown(StylingGuideType? selectedType = null)
        {
            ViewBag.GuideTypes = Enum.GetValues<StylingGuideType>()
                .Select(t => new SelectListItem
                {
                    Value = ((int)t).ToString(),
                    Text = t.ToString(),
                    Selected = selectedType.HasValue && t == selectedType.Value
                })
                .ToList();
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
