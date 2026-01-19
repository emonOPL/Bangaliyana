using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    public enum UserRole
    {
        User,
        Seller,
        Moderator,
        Admin
    }

    public enum Gender
    {
        Male,
        Female,
        Other,
        PreferNotToSay
    }

    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public Gender? Gender { get; set; }

        [Display(Name = "User Role")]
        public UserRole Role { get; set; } = UserRole.User;

        [StringLength(500)]
        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Is Verified")]
        public bool IsVerified { get; set; } = false;

        // Address Information - Division/District/Upazila/Union based
        [Display(Name = "Division")]
        public int? DivisionId { get; set; }

        [Display(Name = "District")]
        public int? DistrictId { get; set; }

        [Display(Name = "Upazila")]
        public int? UpazilaId { get; set; }

        [StringLength(200)]
        [Display(Name = "Textual Address")]
        public string? Address { get; set; }

        [StringLength(10)]
        [RegularExpression(@"^\d*$", ErrorMessage = "PostalCodeMustBeDigitsOnly")]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        // Navigation properties for address
        public virtual Division? Division { get; set; }
        public virtual District? District { get; set; }
        public virtual Upazila? Upazila { get; set; }

        [Display(Name = "Profile Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Updated")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Phone Verification Code")]
        public string? PhoneVerificationCode { get; set; }

        [Display(Name = "Phone Verification Expiry")]
        public DateTime? PhoneVerificationExpiry { get; set; }

        // COD Restriction fields
        [Display(Name = "COD Allowed")]
        public bool IsCODAllowed { get; set; } = true;

        [Display(Name = "COD Restriction Reason")]
        [StringLength(500)]
        public string? CODRestrictionReason { get; set; }

        [Display(Name = "COD Restricted At")]
        public DateTime? CODRestrictedAt { get; set; }

        [Display(Name = "Failed COD Orders Count")]
        public int FailedCODOrdersCount { get; set; } = 0;

        [Display(Name = "Returned Orders Count")]
        public int ReturnedOrdersCount { get; set; } = 0;

        // Reward Points System
        [StringLength(8)]
        [Display(Name = "Referral Code")]
        public string? ReferralCode { get; set; }

        [StringLength(450)]
        [Display(Name = "Referred By User ID")]
        public string? ReferredByUserId { get; set; }

        [Display(Name = "Has Used Referral Code")]
        public bool HasUsedReferralCode { get; set; } = false;

        [Display(Name = "Total Reward Points")]
        public int TotalRewardPoints { get; set; } = 0;

        [Display(Name = "Referral Code Generated At")]
        public DateTime? ReferralCodeGeneratedAt { get; set; }

        // Daily Login Reward
        [Display(Name = "Last Daily Reward Date")]
        public DateTime? LastDailyRewardDate { get; set; }

        // Premium Membership
        [Display(Name = "Is Premium Member")]
        public bool IsPremiumMember { get; set; } = false;

        [Display(Name = "Premium Expires At")]
        public DateTime? PremiumExpiresAt { get; set; }

        [Display(Name = "Premium Discount Expires At")]
        public DateTime? PremiumDiscountExpiresAt { get; set; }

        // Wallet
        [Display(Name = "Wallet Balance")]
        [DataType(DataType.Currency)]
        public decimal WalletBalance { get; set; } = 0m;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
        public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public virtual ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
        public virtual Seller? Seller { get; set; }
        public virtual ICollection<UserMenuPermission> MenuPermissions { get; set; } = new List<UserMenuPermission>();
        public virtual ICollection<RewardPointsTransaction> PointsTransactions { get; set; } = new List<RewardPointsTransaction>();
        public virtual ICollection<UserRedeemedReward> RedeemedRewards { get; set; } = new List<UserRedeemedReward>();

        // Full name property for display
        public string FullName => $"{FirstName} {LastName}";
    }
}