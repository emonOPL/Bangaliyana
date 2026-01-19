using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum SellerStatus
    {
        Pending,
        Approved,
        Rejected,
        Suspended
    }

    public enum DocumentType
    {
        [Display(Name = "National ID (NID)")]
        NID,

        [Display(Name = "Birth Certificate")]
        BirthCertificate,

        [Display(Name = "Passport")]
        Passport
    }

    public enum ApplicationStatus
    {
        [Display(Name = "Draft")]
        Draft,

        [Display(Name = "Pending Review")]
        Pending,

        [Display(Name = "Under Review")]
        UnderReview,

        [Display(Name = "Modification Required")]
        ModificationRequired,

        [Display(Name = "Approved")]
        Approved,

        [Display(Name = "Rejected")]
        Rejected,

        [Display(Name = "Suspended")]
        Suspended
    }

    public enum MobileBankingProvider
    {
        [Display(Name = "None")]
        None,

        [Display(Name = "bKash")]
        bKash,

        [Display(Name = "Nagad")]
        Nagad,

        [Display(Name = "Rocket")]
        Rocket,

        [Display(Name = "Upay")]
        Upay
    }

    // Keep BusinessCategory enum for backward compatibility
    public enum BusinessCategory
    {
        [Display(Name = "Fashion & Clothing")]
        Fashion,
        [Display(Name = "Electronics & Gadgets")]
        Electronics,
        [Display(Name = "Groceries & Food")]
        Groceries,
        [Display(Name = "Handicrafts & Artisan")]
        Handicrafts,
        [Display(Name = "Food & Beverages")]
        Food,
        [Display(Name = "Home & Decor")]
        HomeDecor,
        [Display(Name = "Beauty & Personal Care")]
        Beauty,
        [Display(Name = "Books & Stationery")]
        Books,
        [Display(Name = "Sports & Fitness")]
        Sports,
        [Display(Name = "Toys & Games")]
        Toys,
        [Display(Name = "Jewelry & Accessories")]
        Jewelry,
        [Display(Name = "Art & Collectibles")]
        Art,
        [Display(Name = "Services")]
        Services,
        [Display(Name = "Other")]
        Other
    }

    public class Seller
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Shop Name")]
        public string ShopName { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Shop Slug")]
        public string? ShopSlug { get; set; }

        [StringLength(1000)]
        [Display(Name = "Shop Description")]
        public string? ShopDescription { get; set; }

        [StringLength(500)]
        [Display(Name = "Shop Logo URL")]
        public string? ShopLogoUrl { get; set; }

        [StringLength(500)]
        [Display(Name = "Shop Banner URL")]
        public string? ShopBannerUrl { get; set; }

        [StringLength(500)]
        [Display(Name = "Business Address")]
        public string? BusinessAddress { get; set; }

        [StringLength(20)]
        [Phone]
        [Display(Name = "Business Phone")]
        public string? BusinessPhone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Business Email")]
        public string? BusinessEmail { get; set; }

        [StringLength(100)]
        [Display(Name = "Trade License Number")]
        public string? TradeLicenseNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Trade License Document URL")]
        public string? TradeLicenseDocUrl { get; set; }

        [StringLength(100)]
        [Display(Name = "NID Number")]
        public string? NIDNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "NID Document URL")]
        public string? NIDDocUrl { get; set; }

        // ============ PERSONAL INFORMATION (for seller application) ============
        [StringLength(500)]
        [Display(Name = "Passport Size Photo URL")]
        public string? PassportPhotoUrl { get; set; }

        [StringLength(200)]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [StringLength(200)]
        [Display(Name = "Father's Name")]
        public string? FatherName { get; set; }

        [StringLength(200)]
        [Display(Name = "Mother's Name")]
        public string? MotherName { get; set; }

        [StringLength(500)]
        [Display(Name = "Permanent Address")]
        public string? PermanentAddress { get; set; }

        [StringLength(500)]
        [Display(Name = "Present Address")]
        public string? PresentAddress { get; set; }

        // ============ DOCUMENT INFORMATION ============
        [Display(Name = "Document Type")]
        public DocumentType DocumentType { get; set; } = DocumentType.NID;

        [StringLength(50)]
        [Display(Name = "Document Number")]
        public string? DocumentNumber { get; set; }  // UNIQUE constraint - NID/Birth Certificate/Passport number

        [StringLength(500)]
        [Display(Name = "Document Photo URL")]
        public string? DocumentPhotoUrl { get; set; }

        // ============ MOBILE BANKING ACCOUNT (Optional) ============
        [Display(Name = "Mobile Banking Provider")]
        public MobileBankingProvider MobileBankingProvider { get; set; } = MobileBankingProvider.None;

        [StringLength(20)]
        [Phone]
        [Display(Name = "Mobile Banking Number")]
        public string? MobileBankingNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Mobile Banking Account Name")]
        public string? MobileBankingAccountName { get; set; }

        // ============ BUSINESS TYPE (FK to BusinessType table) ============
        [Display(Name = "Business Type")]
        public int? BusinessTypeId { get; set; }

        [ForeignKey("BusinessTypeId")]
        public virtual BusinessType? BusinessTypeEntity { get; set; }

        // ============ APPLICATION STATUS ============
        [Display(Name = "Application Status")]
        public ApplicationStatus ApplicationStatus { get; set; } = ApplicationStatus.Draft;

        [StringLength(1000)]
        [Display(Name = "Modification Notes")]
        public string? ModificationNotes { get; set; }

        [Display(Name = "Modification Requested At")]
        public DateTime? ModificationRequestedAt { get; set; }

        [Display(Name = "Application Step")]
        public int ApplicationStep { get; set; } = 1;  // 1-4 for multi-step wizard

        // Business Category (kept for backward compatibility)
        [Display(Name = "Business Category")]
        public BusinessCategory BusinessCategory { get; set; } = BusinessCategory.Other;

        // Shop Policies
        [StringLength(2000)]
        [Display(Name = "Shop Policies")]
        public string? ShopPolicies { get; set; }

        [StringLength(2000)]
        [Display(Name = "Return Policy")]
        public string? ReturnPolicy { get; set; }

        [StringLength(2000)]
        [Display(Name = "Shipping Info")]
        public string? ShippingInfo { get; set; }

        // Social Links
        [StringLength(200)]
        [Url]
        [Display(Name = "Facebook URL")]
        public string? FacebookUrl { get; set; }

        [StringLength(200)]
        [Url]
        [Display(Name = "Instagram URL")]
        public string? InstagramUrl { get; set; }

        [StringLength(20)]
        [Display(Name = "WhatsApp Number")]
        public string? WhatsAppNumber { get; set; }

        // Operating Hours
        [StringLength(500)]
        [Display(Name = "Operating Hours")]
        public string? OperatingHours { get; set; }

        // Seller Application (for self-registration)
        [StringLength(1000)]
        [Display(Name = "Application Note")]
        public string? ApplicationNote { get; set; }

        [Display(Name = "Applied At")]
        public DateTime? AppliedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        [Display(Name = "Status")]
        public SellerStatus Status { get; set; } = SellerStatus.Pending;

        [Display(Name = "Is Verified")]
        public bool IsVerified { get; set; } = false;

        [Display(Name = "Commission Rate (%)")]
        [Range(0, 100)]
        public decimal CommissionRate { get; set; } = 5m;

        [Display(Name = "Account Balance")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AccountBalance { get; set; } = 0m;

        [Display(Name = "Total Sales")]
        public decimal TotalSales { get; set; } = 0;

        [Display(Name = "Total Orders")]
        public int TotalOrders { get; set; } = 0;

        [Display(Name = "Rating")]
        [Range(0, 5)]
        public decimal Rating { get; set; } = 0;

        [Display(Name = "Review Count")]
        public int ReviewCount { get; set; } = 0;

        [Display(Name = "Follower Count")]
        public int FollowerCount { get; set; } = 0;

        // ============ INVENTORY ALERT SETTINGS ============
        [Display(Name = "Low Stock Alert Threshold")]
        [Range(1, 100)]
        public int LowStockThreshold { get; set; } = 5;

        [Display(Name = "Enable Low Stock Email Alerts")]
        public bool EnableLowStockEmailAlerts { get; set; } = true;

        [Display(Name = "Enable Low Stock In-App Alerts")]
        public bool EnableLowStockInAppAlerts { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        // ============ SUBSCRIPTION ============
        [Display(Name = "Current Subscription")]
        public int? CurrentSubscriptionId { get; set; }

        [ForeignKey("CurrentSubscriptionId")]
        public virtual SellerSubscription? CurrentSubscription { get; set; }

        // Navigation properties
        public virtual ICollection<Products> Products { get; set; } = new List<Products>();
        public virtual ICollection<SellerBankAccount> BankAccounts { get; set; } = new List<SellerBankAccount>();
        public virtual ICollection<SellerBankAccountChangeRequest> BankAccountChangeRequests { get; set; } = new List<SellerBankAccountChangeRequest>();
        public virtual ICollection<SellerTransaction> Transactions { get; set; } = new List<SellerTransaction>();
        public virtual ICollection<ShopFollower> Followers { get; set; } = new List<ShopFollower>();
        public virtual ICollection<SellerSubscription> Subscriptions { get; set; } = new List<SellerSubscription>();
    }
}
