using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Bangaliyana.Services
{
    public class SiteSettingsService : ISiteSettingsService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SiteSettingsService> _logger;

        private const string SITE_SETTINGS_CACHE_KEY = "SiteSettings";
        private const string SOCIAL_LINKS_CACHE_KEY = "SocialLinks";
        private const int CACHE_DURATION_MINUTES = 30;

        public SiteSettingsService(
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<SiteSettingsService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public void ClearCache()
        {
            _cache.Remove(SITE_SETTINGS_CACHE_KEY);
            _cache.Remove(SOCIAL_LINKS_CACHE_KEY);
            _logger.LogInformation("Site settings cache cleared");
        }

        #region Site Settings

        public async Task<SiteSettings> GetSiteSettingsAsync()
        {
            if (_cache.TryGetValue(SITE_SETTINGS_CACHE_KEY, out SiteSettings? cached) && cached != null)
            {
                return cached;
            }

            var settings = await _db.SiteSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                // Create default settings if none exist
                settings = new SiteSettings
                {
                    SiteName = "Bangaliyana",
                    SiteTagline = "Authentic Bangladeshi Style, Woven for You",
                    SiteDescription = "Modern e-commerce platform for authentic Bangladeshi products",
                    SiteKeywords = "e-commerce, Bangladesh, online shopping, products, fashion",
                    CompanyName = "Bangaliyana",
                    FoundingYear = 2020,
                    ContactEmail = "info@bangaliyana.com",
                    CurrencySymbol = "৳",
                    CurrencyCode = "BDT",
                    DefaultDeliveryCharge = 100m,
                    PrimaryColor = "#6366f1",
                    SecondaryColor = "#8b5cf6",
                    AccentColor = "#06b6d4",
                    EnableDarkMode = true,
                    FooterAboutText = "Your Home for Authentic Bangladeshi Fashion. Quality guaranteed, satisfaction delivered.",
                    CopyrightText = "All rights reserved."
                };

                _db.SiteSettings.Add(settings);
                await _db.SaveChangesAsync();
            }

            _cache.Set(SITE_SETTINGS_CACHE_KEY, settings, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return settings;
        }

        public async Task UpdateSiteSettingsAsync(SiteSettings settings)
        {
            settings.UpdatedAt = DateTime.UtcNow;
            _db.SiteSettings.Update(settings);
            await _db.SaveChangesAsync();
            ClearCache();
        }

        #endregion

        #region Social Links

        public async Task<List<SocialLink>> GetActiveSocialLinksAsync()
        {
            if (_cache.TryGetValue(SOCIAL_LINKS_CACHE_KEY, out List<SocialLink>? cached) && cached != null)
            {
                return cached;
            }

            var links = await _db.SocialLinks
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            _cache.Set(SOCIAL_LINKS_CACHE_KEY, links, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return links;
        }

        public async Task<SocialLink?> GetSocialLinkByIdAsync(int id)
        {
            return await _db.SocialLinks.FindAsync(id);
        }

        public async Task<List<SocialLink>> GetAllSocialLinksAsync()
        {
            return await _db.SocialLinks.OrderBy(s => s.DisplayOrder).ToListAsync();
        }

        public async Task CreateSocialLinkAsync(SocialLink link)
        {
            link.CreatedAt = DateTime.UtcNow;
            link.UpdatedAt = DateTime.UtcNow;
            _db.SocialLinks.Add(link);
            await _db.SaveChangesAsync();
            _cache.Remove(SOCIAL_LINKS_CACHE_KEY);
        }

        public async Task UpdateSocialLinkAsync(SocialLink link)
        {
            link.UpdatedAt = DateTime.UtcNow;
            _db.SocialLinks.Update(link);
            await _db.SaveChangesAsync();
            _cache.Remove(SOCIAL_LINKS_CACHE_KEY);
        }

        public async Task DeleteSocialLinkAsync(int id)
        {
            var link = await _db.SocialLinks.FindAsync(id);
            if (link != null)
            {
                _db.SocialLinks.Remove(link);
                await _db.SaveChangesAsync();
                _cache.Remove(SOCIAL_LINKS_CACHE_KEY);
            }
        }

        #endregion

        #region Features

        public async Task<List<Feature>> GetActiveFeaturesBySectionAsync(FeatureSection section)
        {
            return await _db.Features
                .Where(f => f.IsActive && f.Section == section)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();
        }

        public async Task<Feature?> GetFeatureByIdAsync(int id)
        {
            return await _db.Features.FindAsync(id);
        }

        public async Task<List<Feature>> GetAllFeaturesAsync()
        {
            return await _db.Features.OrderBy(f => f.Section).ThenBy(f => f.DisplayOrder).ToListAsync();
        }

        public async Task CreateFeatureAsync(Feature feature)
        {
            feature.CreatedAt = DateTime.UtcNow;
            feature.UpdatedAt = DateTime.UtcNow;
            _db.Features.Add(feature);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateFeatureAsync(Feature feature)
        {
            feature.UpdatedAt = DateTime.UtcNow;
            _db.Features.Update(feature);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteFeatureAsync(int id)
        {
            var feature = await _db.Features.FindAsync(id);
            if (feature != null)
            {
                _db.Features.Remove(feature);
                await _db.SaveChangesAsync();
            }
        }

        #endregion

        #region Banners

        public async Task<List<Banner>> GetActiveBannersByLocationAsync(BannerLocation location)
        {
            var now = DateTime.UtcNow;
            return await _db.Banners
                .Where(b => b.IsActive && b.Location == location)
                .Where(b => !b.StartDate.HasValue || b.StartDate <= now)
                .Where(b => !b.EndDate.HasValue || b.EndDate >= now)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();
        }

        public async Task<Banner?> GetBannerByIdAsync(int id)
        {
            return await _db.Banners.FindAsync(id);
        }

        public async Task<List<Banner>> GetAllBannersAsync()
        {
            return await _db.Banners.OrderBy(b => b.Location).ThenBy(b => b.DisplayOrder).ToListAsync();
        }

        public async Task CreateBannerAsync(Banner banner)
        {
            banner.CreatedAt = DateTime.UtcNow;
            banner.UpdatedAt = DateTime.UtcNow;
            _db.Banners.Add(banner);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateBannerAsync(Banner banner)
        {
            banner.UpdatedAt = DateTime.UtcNow;
            _db.Banners.Update(banner);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteBannerAsync(int id)
        {
            var banner = await _db.Banners.FindAsync(id);
            if (banner != null)
            {
                _db.Banners.Remove(banner);
                await _db.SaveChangesAsync();
            }
        }

        #endregion

        #region Page Content

        public async Task<PageContent?> GetPageContentBySlugAsync(string slug)
        {
            return await _db.PageContents
                .FirstOrDefaultAsync(p => p.Slug.ToLower() == slug.ToLower() && p.IsActive);
        }

        public async Task<PageContent?> GetPageContentByIdAsync(int id)
        {
            return await _db.PageContents.FindAsync(id);
        }

        public async Task<List<PageContent>> GetAllPageContentsAsync()
        {
            return await _db.PageContents.OrderBy(p => p.DisplayOrder).ToListAsync();
        }

        public async Task<List<PageContent>> GetFooterPagesAsync()
        {
            return await _db.PageContents
                .Where(p => p.IsActive && p.ShowInFooter)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<PageContent>> GetHeaderPagesAsync()
        {
            return await _db.PageContents
                .Where(p => p.IsActive && p.ShowInHeader)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
        }

        public async Task CreatePageContentAsync(PageContent page)
        {
            page.CreatedAt = DateTime.UtcNow;
            page.UpdatedAt = DateTime.UtcNow;
            _db.PageContents.Add(page);
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePageContentAsync(PageContent page)
        {
            page.UpdatedAt = DateTime.UtcNow;
            _db.PageContents.Update(page);
            await _db.SaveChangesAsync();
        }

        public async Task DeletePageContentAsync(int id)
        {
            var page = await _db.PageContents.FindAsync(id);
            if (page != null)
            {
                _db.PageContents.Remove(page);
                await _db.SaveChangesAsync();
            }
        }

        #endregion

        #region Testimonials

        public async Task<List<Testimonial>> GetActiveTestimonialsAsync()
        {
            return await _db.Testimonials
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<Testimonial>> GetFeaturedTestimonialsAsync()
        {
            return await _db.Testimonials
                .Where(t => t.IsActive && t.IsFeatured)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();
        }

        public async Task<Testimonial?> GetTestimonialByIdAsync(int id)
        {
            return await _db.Testimonials.FindAsync(id);
        }

        public async Task<List<Testimonial>> GetAllTestimonialsAsync()
        {
            return await _db.Testimonials.OrderBy(t => t.DisplayOrder).ToListAsync();
        }

        public async Task CreateTestimonialAsync(Testimonial testimonial)
        {
            testimonial.CreatedAt = DateTime.UtcNow;
            testimonial.UpdatedAt = DateTime.UtcNow;
            _db.Testimonials.Add(testimonial);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateTestimonialAsync(Testimonial testimonial)
        {
            var existing = await _db.Testimonials.FindAsync(testimonial.Id);
            if (existing != null)
            {
                existing.CustomerName = testimonial.CustomerName;
                existing.CustomerTitle = testimonial.CustomerTitle;
                existing.CustomerLocation = testimonial.CustomerLocation;
                existing.PhotoUrl = testimonial.PhotoUrl ?? existing.PhotoUrl;
                existing.Content = testimonial.Content;
                existing.Rating = testimonial.Rating;
                existing.DisplayOrder = testimonial.DisplayOrder;
                existing.IsFeatured = testimonial.IsFeatured;
                existing.IsActive = testimonial.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task DeleteTestimonialAsync(int id)
        {
            var testimonial = await _db.Testimonials.FindAsync(id);
            if (testimonial != null)
            {
                _db.Testimonials.Remove(testimonial);
                await _db.SaveChangesAsync();
            }
        }

        #endregion

        #region FAQs

        public async Task<List<FAQ>> GetActiveFAQsAsync()
        {
            return await _db.FAQs
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<FAQ>> GetFAQsByCategoryAsync(string category)
        {
            return await _db.FAQs
                .Where(f => f.IsActive && f.Category == category)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<string>> GetFAQCategoriesAsync()
        {
            return await _db.FAQs
                .Where(f => f.IsActive && !string.IsNullOrEmpty(f.Category))
                .Select(f => f.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<List<FAQ>> SearchFAQsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetActiveFAQsAsync();

            var lowerQuery = query.ToLower();
            return await _db.FAQs
                .Where(f => f.IsActive &&
                    (f.Question.ToLower().Contains(lowerQuery) ||
                     f.Answer.ToLower().Contains(lowerQuery) ||
                     (f.Category != null && f.Category.ToLower().Contains(lowerQuery))))
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();
        }

        public async Task<FAQ?> GetFAQByIdAsync(int id)
        {
            return await _db.FAQs.FindAsync(id);
        }

        public async Task<List<FAQ>> GetAllFAQsAsync()
        {
            return await _db.FAQs.OrderBy(f => f.Category).ThenBy(f => f.DisplayOrder).ToListAsync();
        }

        public async Task CreateFAQAsync(FAQ faq)
        {
            faq.CreatedAt = DateTime.UtcNow;
            faq.UpdatedAt = DateTime.UtcNow;
            _db.FAQs.Add(faq);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateFAQAsync(FAQ faq)
        {
            faq.UpdatedAt = DateTime.UtcNow;
            _db.FAQs.Update(faq);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteFAQAsync(int id)
        {
            var faq = await _db.FAQs.FindAsync(id);
            if (faq != null)
            {
                _db.FAQs.Remove(faq);
                await _db.SaveChangesAsync();
            }
        }

        #endregion

        #region Contact Inquiries

        public async Task<ContactInquiry> CreateContactInquiryAsync(ContactInquiry inquiry)
        {
            inquiry.CreatedAt = DateTime.UtcNow;
            inquiry.UpdatedAt = DateTime.UtcNow;
            inquiry.Status = InquiryStatus.New;
            _db.ContactInquiries.Add(inquiry);
            await _db.SaveChangesAsync();
            return inquiry;
        }

        public async Task<ContactInquiry?> GetContactInquiryByIdAsync(int id)
        {
            return await _db.ContactInquiries
                .Include(i => i.User)
                .Include(i => i.RespondedBy)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<ContactInquiry>> GetAllContactInquiriesAsync()
        {
            return await _db.ContactInquiries
                .Include(i => i.User)
                .Include(i => i.RespondedBy)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ContactInquiry>> GetContactInquiriesByStatusAsync(InquiryStatus status)
        {
            return await _db.ContactInquiries
                .Include(i => i.User)
                .Where(i => i.Status == status)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetPendingInquiriesCountAsync()
        {
            return await _db.ContactInquiries
                .CountAsync(i => i.Status == InquiryStatus.New || i.Status == InquiryStatus.InProgress);
        }

        public async Task UpdateContactInquiryAsync(ContactInquiry inquiry)
        {
            inquiry.UpdatedAt = DateTime.UtcNow;
            _db.ContactInquiries.Update(inquiry);
            await _db.SaveChangesAsync();
        }

        public async Task RespondToInquiryAsync(int id, string response, string respondedById)
        {
            var inquiry = await _db.ContactInquiries.FindAsync(id);
            if (inquiry != null)
            {
                inquiry.AdminResponse = response;
                inquiry.RespondedById = respondedById;
                inquiry.RespondedAt = DateTime.UtcNow;
                inquiry.Status = InquiryStatus.Responded;
                inquiry.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task UpdateInquiryStatusAsync(int id, InquiryStatus status)
        {
            var inquiry = await _db.ContactInquiries.FindAsync(id);
            if (inquiry != null)
            {
                inquiry.Status = status;
                inquiry.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task DeleteContactInquiryAsync(int id)
        {
            var inquiry = await _db.ContactInquiries.FindAsync(id);
            if (inquiry != null)
            {
                _db.ContactInquiries.Remove(inquiry);
                await _db.SaveChangesAsync();
            }
        }

        #endregion
    }
}
