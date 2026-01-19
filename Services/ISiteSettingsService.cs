using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface ISiteSettingsService
    {
        // Site Settings
        Task<SiteSettings> GetSiteSettingsAsync();
        Task UpdateSiteSettingsAsync(SiteSettings settings);
        void ClearCache();

        // Social Links
        Task<List<SocialLink>> GetActiveSocialLinksAsync();
        Task<SocialLink?> GetSocialLinkByIdAsync(int id);
        Task<List<SocialLink>> GetAllSocialLinksAsync();
        Task CreateSocialLinkAsync(SocialLink link);
        Task UpdateSocialLinkAsync(SocialLink link);
        Task DeleteSocialLinkAsync(int id);

        // Features
        Task<List<Feature>> GetActiveFeaturesBySectionAsync(FeatureSection section);
        Task<Feature?> GetFeatureByIdAsync(int id);
        Task<List<Feature>> GetAllFeaturesAsync();
        Task CreateFeatureAsync(Feature feature);
        Task UpdateFeatureAsync(Feature feature);
        Task DeleteFeatureAsync(int id);

        // Banners
        Task<List<Banner>> GetActiveBannersByLocationAsync(BannerLocation location);
        Task<Banner?> GetBannerByIdAsync(int id);
        Task<List<Banner>> GetAllBannersAsync();
        Task CreateBannerAsync(Banner banner);
        Task UpdateBannerAsync(Banner banner);
        Task DeleteBannerAsync(int id);

        // Page Content
        Task<PageContent?> GetPageContentBySlugAsync(string slug);
        Task<PageContent?> GetPageContentByIdAsync(int id);
        Task<List<PageContent>> GetAllPageContentsAsync();
        Task<List<PageContent>> GetFooterPagesAsync();
        Task<List<PageContent>> GetHeaderPagesAsync();
        Task CreatePageContentAsync(PageContent page);
        Task UpdatePageContentAsync(PageContent page);
        Task DeletePageContentAsync(int id);

        // Testimonials
        Task<List<Testimonial>> GetActiveTestimonialsAsync();
        Task<List<Testimonial>> GetFeaturedTestimonialsAsync();
        Task<Testimonial?> GetTestimonialByIdAsync(int id);
        Task<List<Testimonial>> GetAllTestimonialsAsync();
        Task CreateTestimonialAsync(Testimonial testimonial);
        Task UpdateTestimonialAsync(Testimonial testimonial);
        Task DeleteTestimonialAsync(int id);

        // FAQs
        Task<List<FAQ>> GetActiveFAQsAsync();
        Task<List<FAQ>> GetFAQsByCategoryAsync(string category);
        Task<List<string>> GetFAQCategoriesAsync();
        Task<List<FAQ>> SearchFAQsAsync(string query);
        Task<FAQ?> GetFAQByIdAsync(int id);
        Task<List<FAQ>> GetAllFAQsAsync();
        Task CreateFAQAsync(FAQ faq);
        Task UpdateFAQAsync(FAQ faq);
        Task DeleteFAQAsync(int id);

        // Contact Inquiries
        Task<ContactInquiry> CreateContactInquiryAsync(ContactInquiry inquiry);
        Task<ContactInquiry?> GetContactInquiryByIdAsync(int id);
        Task<List<ContactInquiry>> GetAllContactInquiriesAsync();
        Task<List<ContactInquiry>> GetContactInquiriesByStatusAsync(InquiryStatus status);
        Task<int> GetPendingInquiriesCountAsync();
        Task UpdateContactInquiryAsync(ContactInquiry inquiry);
        Task RespondToInquiryAsync(int id, string response, string respondedById);
        Task UpdateInquiryStatusAsync(int id, InquiryStatus status);
        Task DeleteContactInquiryAsync(int id);
    }
}
