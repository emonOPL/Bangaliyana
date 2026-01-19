using X.PagedList;

namespace Bangaliyana.Models.ViewModels
{
    public class HomePageViewModel
    {
        // Site Settings
        public SiteSettings SiteSettings { get; set; } = new();

        // Hero Slider Banners
        public IEnumerable<Banner> HeroBanners { get; set; } = new List<Banner>();

        // Categories
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IEnumerable<Category> FeaturedCategories { get; set; } = new List<Category>();

        // Products
        public IPagedList<Products> AllProducts { get; set; } = null!;
        public IEnumerable<Products> TrendingProducts { get; set; } = new List<Products>();
        public IEnumerable<Products> BestSellingProducts { get; set; } = new List<Products>();
        public IEnumerable<Products> NewArrivals { get; set; } = new List<Products>();
        public IEnumerable<Products> FeaturedProducts { get; set; } = new List<Products>();
        public IEnumerable<Products> FlashSaleProducts { get; set; } = new List<Products>();
        public IEnumerable<Products> PersonalizedProducts { get; set; } = new List<Products>();

        // Banners
        public IEnumerable<Banner> PromotionalBanners { get; set; } = new List<Banner>();
        public IEnumerable<Banner> OfferBanners { get; set; } = new List<Banner>();

        // Features
        public IEnumerable<Feature> HomeFeatures { get; set; } = new List<Feature>();

        // Testimonials
        public IEnumerable<Testimonial> Testimonials { get; set; } = new List<Testimonial>();

        // Flash Sale Info
        public DateTime? FlashSaleEndTime { get; set; }
        public bool HasActiveFlashSale => FlashSaleEndTime.HasValue && FlashSaleEndTime.Value > DateTime.UtcNow;
    }
}
