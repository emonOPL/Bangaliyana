using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Linq;

namespace Bangaliyana.Models
{
    public class CheckoutViewModel
    {
        // Address Selection Mode
        [Display(Name = "Use Saved Address")]
        public bool UseSavedAddress { get; set; } = true;

        [Display(Name = "Selected Address")]
        public int? SelectedAddressId { get; set; }

        // List of user's saved addresses
        public List<UserAddress> SavedAddresses { get; set; } = new List<UserAddress>();

        [Required(ErrorMessage = "NameIsRequired")]
        [Display(Name = "Full Name")]
        public string CustomerName { get; set; } = null!;

        [Required, EmailAddress(ErrorMessage = "InvalidEmail")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "PhoneIsRequired")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "PhoneMustBe11Digits")]
        [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "PhoneMustStartWithValidPrefix")]
        public string Phone { get; set; } = null!;

        // Address Information - Division/District/Upazila/Union based
        [Display(Name = "Division")]
        public int? DivisionId { get; set; }

        [Display(Name = "District")]
        public int? DistrictId { get; set; }

        [Display(Name = "Upazila")]
        public int? UpazilaId { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; } = null!;

        [StringLength(10)]
        [RegularExpression(@"^\d*$", ErrorMessage = "PostalCodeMustBeDigitsOnly")]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        // Delivery Instructions
        [StringLength(500)]
        [Display(Name = "Delivery Instructions")]
        public string? DeliveryInstructions { get; set; }

        // Save address for future orders
        [Display(Name = "Save Address")]
        public bool SaveAddress { get; set; } = true;

        [Required(ErrorMessage = "PleaseSelectPaymentMethod")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        // Mobile Banking Number (for bKash, Nagad, Rocket)
        [Display(Name = "Mobile Banking Number")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "MobileNumberMustBe11Digits")]
        [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "MobileNumberMustStartWithValidPrefix")]
        public string? MobileBankingNumber { get; set; }

        // Transaction ID for mobile banking
        [Display(Name = "Transaction ID")]
        public string? TransactionId { get; set; }

        public decimal DeliveryCharge { get; set; } = 100m;

        // Cart items (from session)
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();

        // CartTotal uses EffectivePrice which includes variant AdditionalPrice
        public decimal CartTotal => CartItems?.Sum(i => i.EffectivePrice * i.Quantity) ?? 0m;

        public decimal GrandTotal => CartTotal + DeliveryCharge - DiscountAmount - RewardDiscountAmount - WalletAmountToUse - PremiumDiscountAmount;

        // Coupon Discount
        public decimal DiscountAmount { get; set; } = 0m;

        // Rewards and Wallet System
        [Display(Name = "Select Discount Voucher")]
        public int? SelectedDiscountRewardId { get; set; }

        [Display(Name = "Select Free Shipping Voucher")]
        public int? SelectedFreeShippingRewardId { get; set; }

        [Display(Name = "Use Wallet Balance")]
        public bool UseWalletBalance { get; set; } = false;

        [Display(Name = "Wallet Amount to Use")]
        public decimal WalletAmountToUse { get; set; } = 0m;

        // Available rewards and wallet info (populated in GET)
        public List<UserRedeemedReward> AvailableFreeShippingVouchers { get; set; } = new List<UserRedeemedReward>();
        public List<UserRedeemedReward> AvailableDiscountVouchers { get; set; } = new List<UserRedeemedReward>();
        public decimal AvailableWalletBalance { get; set; } = 0m;
        public bool IsPremiumMember { get; set; } = false;
        public bool HasActivePremiumDiscount { get; set; } = false;

        // Calculated amounts (will be set in POST)
        public decimal RewardDiscountAmount { get; set; } = 0m;
        public decimal PremiumDiscountAmount { get; set; } = 0m;
        public bool FreeShippingApplied { get; set; } = false;
    }
}
