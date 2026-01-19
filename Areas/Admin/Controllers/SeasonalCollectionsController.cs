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
    public class SeasonalCollectionsController : Controller
    {
        private readonly ISeasonalCollectionService _collectionService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SeasonalCollectionsController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SeasonalCollectionsController(
            ISeasonalCollectionService collectionService,
            IWebHostEnvironment environment,
            ILogger<SeasonalCollectionsController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _collectionService = collectionService;
            _environment = environment;
            _logger = logger;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var collections = await _collectionService.GetAllCollectionsAsync();
            return View(collections);
        }

        public IActionResult Create()
        {
            PopulateSeasonTypeDropdown();
            return View(new SeasonalCollection { Year = DateTime.UtcNow.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SeasonalCollection model, IFormFile? bannerImage, IFormFile? mobileBannerImage, IFormFile? thumbnailImage)
        {
            if (!ModelState.IsValid)
            {
                PopulateSeasonTypeDropdown(model.SeasonType);
                return View(model);
            }

            try
            {
                if (bannerImage != null && bannerImage.Length > 0)
                {
                    model.BannerImageUrl = await SaveFileAsync(bannerImage, "seasonal-collections");
                }

                if (mobileBannerImage != null && mobileBannerImage.Length > 0)
                {
                    model.MobileBannerImageUrl = await SaveFileAsync(mobileBannerImage, "seasonal-collections");
                }

                if (thumbnailImage != null && thumbnailImage.Length > 0)
                {
                    model.ThumbnailImageUrl = await SaveFileAsync(thumbnailImage, "seasonal-collections");
                }

                await _collectionService.CreateCollectionAsync(model);
                TempData["success"] = _localizer["SeasonalCollectionCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating seasonal collection");
                TempData["error"] = _localizer["FailedToCreateSeasonalCollection"].Value;
                PopulateSeasonTypeDropdown(model.SeasonType);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var collection = await _collectionService.GetCollectionByIdAsync(id);
            if (collection == null)
            {
                return NotFound();
            }

            PopulateSeasonTypeDropdown(collection.SeasonType);
            return View(collection);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SeasonalCollection model, IFormFile? bannerImage, IFormFile? mobileBannerImage, IFormFile? thumbnailImage)
        {
            if (!ModelState.IsValid)
            {
                PopulateSeasonTypeDropdown(model.SeasonType);
                return View(model);
            }

            try
            {
                var existingCollection = await _collectionService.GetCollectionByIdAsync(model.Id);

                if (bannerImage != null && bannerImage.Length > 0)
                {
                    DeleteOldImage(existingCollection?.BannerImageUrl);
                    model.BannerImageUrl = await SaveFileAsync(bannerImage, "seasonal-collections");
                }
                else
                {
                    model.BannerImageUrl = existingCollection?.BannerImageUrl;
                }

                if (mobileBannerImage != null && mobileBannerImage.Length > 0)
                {
                    DeleteOldImage(existingCollection?.MobileBannerImageUrl);
                    model.MobileBannerImageUrl = await SaveFileAsync(mobileBannerImage, "seasonal-collections");
                }
                else
                {
                    model.MobileBannerImageUrl = existingCollection?.MobileBannerImageUrl;
                }

                if (thumbnailImage != null && thumbnailImage.Length > 0)
                {
                    DeleteOldImage(existingCollection?.ThumbnailImageUrl);
                    model.ThumbnailImageUrl = await SaveFileAsync(thumbnailImage, "seasonal-collections");
                }
                else
                {
                    model.ThumbnailImageUrl = existingCollection?.ThumbnailImageUrl;
                }

                await _collectionService.UpdateCollectionAsync(model);
                TempData["success"] = _localizer["SeasonalCollectionUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating seasonal collection");
                TempData["error"] = _localizer["FailedToUpdateSeasonalCollection"].Value;
                PopulateSeasonTypeDropdown(model.SeasonType);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var collection = await _collectionService.GetCollectionByIdAsync(id);
                DeleteOldImage(collection?.BannerImageUrl);
                DeleteOldImage(collection?.MobileBannerImageUrl);
                DeleteOldImage(collection?.ThumbnailImageUrl);

                await _collectionService.DeleteCollectionAsync(id);
                TempData["success"] = _localizer["SeasonalCollectionDeletedSuccessfully"].Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting seasonal collection");
                TempData["error"] = _localizer["FailedToDeleteSeasonalCollection"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        #region Helpers

        private void PopulateSeasonTypeDropdown(SeasonType? selectedType = null)
        {
            ViewBag.SeasonTypes = Enum.GetValues<SeasonType>()
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
