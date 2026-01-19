using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class SiteSettingsController : Controller
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SiteSettingsController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SiteSettingsController(
            ISiteSettingsService siteSettingsService,
            IWebHostEnvironment environment,
            ILogger<SiteSettingsController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _siteSettingsService = siteSettingsService;
            _environment = environment;
            _logger = logger;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _siteSettingsService.GetSiteSettingsAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SiteSettings model, IFormFile? logoFile, IFormFile? faviconFile, IFormFile? footerLogoFile, IFormFile? darkModeLogoFile, IFormFile? darkModeFooterLogoFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get existing settings to preserve images and delete old ones when replaced
                var existingSettings = await _siteSettingsService.GetSiteSettingsAsync();

                // Handle logo upload (Light Mode) - preserve existing if no new upload
                if (logoFile != null && logoFile.Length > 0)
                {
                    DeleteOldImage(existingSettings?.LogoUrl);
                    model.LogoUrl = await SaveFileAsync(logoFile, "logos");
                }
                else
                {
                    model.LogoUrl = existingSettings?.LogoUrl;
                }

                // Handle dark mode logo upload - preserve existing if no new upload
                if (darkModeLogoFile != null && darkModeLogoFile.Length > 0)
                {
                    DeleteOldImage(existingSettings?.DarkModeLogoUrl);
                    model.DarkModeLogoUrl = await SaveFileAsync(darkModeLogoFile, "logos");
                }
                else
                {
                    model.DarkModeLogoUrl = existingSettings?.DarkModeLogoUrl;
                }

                // Handle favicon upload - preserve existing if no new upload
                if (faviconFile != null && faviconFile.Length > 0)
                {
                    DeleteOldImage(existingSettings?.FaviconUrl);
                    model.FaviconUrl = await SaveFileAsync(faviconFile, "logos");
                }
                else
                {
                    model.FaviconUrl = existingSettings?.FaviconUrl;
                }

                // Handle footer logo upload (Light Mode) - preserve existing if no new upload
                if (footerLogoFile != null && footerLogoFile.Length > 0)
                {
                    DeleteOldImage(existingSettings?.FooterLogoUrl);
                    model.FooterLogoUrl = await SaveFileAsync(footerLogoFile, "logos");
                }
                else
                {
                    model.FooterLogoUrl = existingSettings?.FooterLogoUrl;
                }

                // Handle dark mode footer logo upload - preserve existing if no new upload
                if (darkModeFooterLogoFile != null && darkModeFooterLogoFile.Length > 0)
                {
                    DeleteOldImage(existingSettings?.DarkModeFooterLogoUrl);
                    model.DarkModeFooterLogoUrl = await SaveFileAsync(darkModeFooterLogoFile, "logos");
                }
                else
                {
                    model.DarkModeFooterLogoUrl = existingSettings?.DarkModeFooterLogoUrl;
                }

                await _siteSettingsService.UpdateSiteSettingsAsync(model);
                TempData["success"] = _localizer["SiteSettingsUpdatedSuccessfully"].Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site settings");
                TempData["error"] = _localizer["FailedToUpdateSiteSettings"].Value;
            }

            return RedirectToAction(nameof(Index));
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

        #region Social Links

        public async Task<IActionResult> SocialLinks()
        {
            var links = await _siteSettingsService.GetAllSocialLinksAsync();
            return View(links);
        }

        public IActionResult CreateSocialLink()
        {
            return View(new SocialLink());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSocialLink(SocialLink model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _siteSettingsService.CreateSocialLinkAsync(model);
            TempData["success"] = _localizer["SocialLinkCreatedSuccessfully"].Value;
            return RedirectToAction(nameof(SocialLinks));
        }

        public async Task<IActionResult> EditSocialLink(int id)
        {
            var link = await _siteSettingsService.GetSocialLinkByIdAsync(id);
            if (link == null)
            {
                return NotFound();
            }
            return View(link);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSocialLink(SocialLink model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _siteSettingsService.UpdateSocialLinkAsync(model);
            TempData["success"] = _localizer["SocialLinkUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(SocialLinks));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSocialLink(int id)
        {
            await _siteSettingsService.DeleteSocialLinkAsync(id);
            TempData["success"] = _localizer["SocialLinkDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(SocialLinks));
        }

        #endregion

        #region Features

        public async Task<IActionResult> Features()
        {
            var features = await _siteSettingsService.GetAllFeaturesAsync();
            return View(features);
        }

        public IActionResult CreateFeature()
        {
            return View(new Feature());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFeature(Feature model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                model.ImageUrl = await SaveFileAsync(imageFile, "features");
            }

            await _siteSettingsService.CreateFeatureAsync(model);
            TempData["success"] = _localizer["FeatureCreatedSuccessfully"].Value;
            return RedirectToAction(nameof(Features));
        }

        public async Task<IActionResult> EditFeature(int id)
        {
            var feature = await _siteSettingsService.GetFeatureByIdAsync(id);
            if (feature == null)
            {
                return NotFound();
            }
            return View(feature);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFeature(Feature model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                // Delete old image before saving new one
                var existingFeature = await _siteSettingsService.GetFeatureByIdAsync(model.Id);
                DeleteOldImage(existingFeature?.ImageUrl);
                model.ImageUrl = await SaveFileAsync(imageFile, "features");
            }

            await _siteSettingsService.UpdateFeatureAsync(model);
            TempData["success"] = _localizer["FeatureUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Features));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeature(int id)
        {
            // Delete image file before deleting feature
            var feature = await _siteSettingsService.GetFeatureByIdAsync(id);
            DeleteOldImage(feature?.ImageUrl);

            await _siteSettingsService.DeleteFeatureAsync(id);
            TempData["success"] = _localizer["FeatureDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Features));
        }

        #endregion

        #region Banners

        public async Task<IActionResult> Banners()
        {
            var banners = await _siteSettingsService.GetAllBannersAsync();
            return View(banners);
        }

        public IActionResult CreateBanner()
        {
            return View(new Banner());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBanner(Banner model, IFormFile? imageFile, IFormFile? mobileImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                model.ImageUrl = await SaveFileAsync(imageFile, "banners");
            }

            if (mobileImageFile != null && mobileImageFile.Length > 0)
            {
                model.MobileImageUrl = await SaveFileAsync(mobileImageFile, "banners");
            }

            await _siteSettingsService.CreateBannerAsync(model);
            TempData["success"] = _localizer["BannerCreatedSuccessfully"].Value;
            return RedirectToAction(nameof(Banners));
        }

        public async Task<IActionResult> EditBanner(int id)
        {
            var banner = await _siteSettingsService.GetBannerByIdAsync(id);
            if (banner == null)
            {
                return NotFound();
            }
            return View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBanner(Banner model, IFormFile? imageFile, IFormFile? mobileImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get existing banner for deleting old images
            var existingBanner = await _siteSettingsService.GetBannerByIdAsync(model.Id);

            if (imageFile != null && imageFile.Length > 0)
            {
                DeleteOldImage(existingBanner?.ImageUrl);
                model.ImageUrl = await SaveFileAsync(imageFile, "banners");
            }

            if (mobileImageFile != null && mobileImageFile.Length > 0)
            {
                DeleteOldImage(existingBanner?.MobileImageUrl);
                model.MobileImageUrl = await SaveFileAsync(mobileImageFile, "banners");
            }

            await _siteSettingsService.UpdateBannerAsync(model);
            TempData["success"] = _localizer["BannerUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Banners));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBanner(int id)
        {
            // Delete image files before deleting banner
            var banner = await _siteSettingsService.GetBannerByIdAsync(id);
            DeleteOldImage(banner?.ImageUrl);
            DeleteOldImage(banner?.MobileImageUrl);

            await _siteSettingsService.DeleteBannerAsync(id);
            TempData["success"] = _localizer["BannerDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Banners));
        }

        #endregion

        #region Page Content

        public async Task<IActionResult> Pages()
        {
            var pages = await _siteSettingsService.GetAllPageContentsAsync();
            return View(pages);
        }

        public IActionResult CreatePage()
        {
            return View(new PageContent());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePage(PageContent model, IFormFile? featuredImage)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (featuredImage != null && featuredImage.Length > 0)
            {
                model.FeaturedImageUrl = await SaveFileAsync(featuredImage, "pages");
            }

            await _siteSettingsService.CreatePageContentAsync(model);
            TempData["success"] = _localizer["PageCreatedSuccessfully"].Value;
            return RedirectToAction(nameof(Pages));
        }

        public async Task<IActionResult> EditPage(int id)
        {
            var page = await _siteSettingsService.GetPageContentByIdAsync(id);
            if (page == null)
            {
                return NotFound();
            }
            return View(page);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPage(PageContent model, IFormFile? featuredImage)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (featuredImage != null && featuredImage.Length > 0)
            {
                // Delete old image before saving new one
                var existingPage = await _siteSettingsService.GetPageContentByIdAsync(model.Id);
                DeleteOldImage(existingPage?.FeaturedImageUrl);
                model.FeaturedImageUrl = await SaveFileAsync(featuredImage, "pages");
            }

            await _siteSettingsService.UpdatePageContentAsync(model);
            TempData["success"] = _localizer["PageUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Pages));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePage(int id)
        {
            // Delete image file before deleting page
            var page = await _siteSettingsService.GetPageContentByIdAsync(id);
            DeleteOldImage(page?.FeaturedImageUrl);

            await _siteSettingsService.DeletePageContentAsync(id);
            TempData["success"] = _localizer["PageDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Pages));
        }

        #endregion

        #region Testimonials

        public async Task<IActionResult> Testimonials()
        {
            var testimonials = await _siteSettingsService.GetAllTestimonialsAsync();
            return View(testimonials);
        }

        public IActionResult CreateTestimonial()
        {
            return View(new Testimonial());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestimonial(Testimonial model, IFormFile? photoFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (photoFile != null && photoFile.Length > 0)
            {
                model.PhotoUrl = await SaveFileAsync(photoFile, "testimonials");
            }

            await _siteSettingsService.CreateTestimonialAsync(model);
            TempData["success"] = _localizer["TestimonialCreatedSuccessfully"].Value;
            return RedirectToAction(nameof(Testimonials));
        }

        public async Task<IActionResult> EditTestimonial(int id)
        {
            var testimonial = await _siteSettingsService.GetTestimonialByIdAsync(id);
            if (testimonial == null)
            {
                return NotFound();
            }
            return View(testimonial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTestimonial(Testimonial model, IFormFile? photoFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (photoFile != null && photoFile.Length > 0)
            {
                // Delete old photo before saving new one
                var existingTestimonial = await _siteSettingsService.GetTestimonialByIdAsync(model.Id);
                DeleteOldImage(existingTestimonial?.PhotoUrl);
                model.PhotoUrl = await SaveFileAsync(photoFile, "testimonials");
            }

            await _siteSettingsService.UpdateTestimonialAsync(model);
            TempData["success"] = _localizer["TestimonialUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(Testimonials));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTestimonial(int id)
        {
            // Delete photo file before deleting testimonial
            var testimonial = await _siteSettingsService.GetTestimonialByIdAsync(id);
            DeleteOldImage(testimonial?.PhotoUrl);

            await _siteSettingsService.DeleteTestimonialAsync(id);
            TempData["success"] = _localizer["TestimonialDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Testimonials));
        }

        #endregion

        #region FAQs

        public async Task<IActionResult> FAQs()
        {
            var faqs = await _siteSettingsService.GetAllFAQsAsync();
            return View(faqs);
        }

        public IActionResult CreateFAQ()
        {
            return View(new FAQ());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFAQ(FAQ model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _siteSettingsService.CreateFAQAsync(model);
            TempData["success"] = _localizer["FAQCreatedSuccessfully"].Value;
            return RedirectToAction(nameof(FAQs));
        }

        public async Task<IActionResult> EditFAQ(int id)
        {
            var faq = await _siteSettingsService.GetFAQByIdAsync(id);
            if (faq == null)
            {
                return NotFound();
            }
            return View(faq);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFAQ(FAQ model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _siteSettingsService.UpdateFAQAsync(model);
            TempData["success"] = _localizer["FAQUpdatedSuccessfully"].Value;
            return RedirectToAction(nameof(FAQs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFAQ(int id)
        {
            await _siteSettingsService.DeleteFAQAsync(id);
            TempData["success"] = _localizer["FAQDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(FAQs));
        }

        #endregion
    }
}
