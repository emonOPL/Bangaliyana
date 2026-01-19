using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Paid,           // Kept for backward compatibility
        Processing,
        Shipped,
        OutForDelivery,
        Delivered,
        Cancelled,
        Returned,
        Refunded
    }

    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Refunded,
        PartiallyRefunded
    }

    public enum RefundMethod
    {
        [Display(Name = "Wallet Credit")]
        WalletCredit,
        [Display(Name = "bKash")]
        Bkash,
        [Display(Name = "Nagad")]
        Nagad,
        [Display(Name = "Upay")]
        Upay,
        [Display(Name = "Rocket")]
        Rocket,
        [Display(Name = "Bank Transfer")]
        BankTransfer,
        [Display(Name = "Original Payment Method")]
        OriginalMethod
    }

    public enum RefundStatus
    {
        [Display(Name = "Pending")]
        Pending,
        [Display(Name = "Reviewing")]
        Reviewing,
        [Display(Name = "In Progress")]
        InProgress,
        [Display(Name = "Approved")]
        Approved,
        [Display(Name = "Rejected")]
        Rejected,
        [Display(Name = "Completed")]
        Completed
    }

    public enum DeliveryStatus
    {
        Pending,
        Processing,
        Shipped,
        InTransit,
        OutForDelivery,
        Delivered,
        Failed,
        Returned
    }

    public class Order
    {
        public int Id { get; set; }

        [StringLength(50)]
        [Display(Name = "Order Number")]
        public string? OrderNumber { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = null!;

        [Required, EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "PhoneMustBe11Digits")]
        [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "PhoneMustStartWithValidPrefix")]
        public string Phone { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = null!;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(10)]
        [RegularExpression(@"^\d*$", ErrorMessage = "PostalCodeMustBeDigitsOnly")]
        public string? PostalCode { get; set; }

        // Division/District/Upazila/Union for Bangladesh addresses
        public int? DivisionId { get; set; }

        [ForeignKey("DivisionId")]
        public virtual Division? Division { get; set; }

        public int? DistrictId { get; set; }

        [ForeignKey("DistrictId")]
        public virtual District? District { get; set; }

        public int? UpazilaId { get; set; }

        [ForeignKey("UpazilaId")]
        public virtual Upazila? Upazila { get; set; }

        [DataType(DataType.Currency)]
        public decimal SubTotal { get; set; }

        [DataType(DataType.Currency)]
        public decimal DeliveryCharge { get; set; } = 0m;

        [DataType(DataType.Currency)]
        public decimal DiscountAmount { get; set; } = 0m;

        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        [StringLength(100)]
        public string? TransactionId { get; set; }

        // Sender's mobile number for MFS payments (bKash, Nagad, Rocket, Upay)
        [StringLength(11)]
        [Display(Name = "Sender Mobile Number")]
        public string? SenderMobileNumber { get; set; }

        // Payment verification tracking
        [Display(Name = "Payment Verified")]
        public bool IsPaymentVerified { get; set; } = false;

        [Display(Name = "Payment Verified At")]
        public DateTime? PaymentVerifiedAt { get; set; }

        [StringLength(100)]
        [Display(Name = "Payment Verified By")]
        public string? PaymentVerifiedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Payment Verification Notes")]
        public string? PaymentVerificationNotes { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;

        public bool IsPaymentReceived { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "Order Notes")]
        public string? Notes { get; set; }

        [StringLength(500)]
        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        // Tracking
        [StringLength(100)]
        [Display(Name = "Tracking Number")]
        public string? TrackingNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Courier Name")]
        public string? CourierName { get; set; }

        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // Reward Points tracking
        [Display(Name = "Delivery Points Awarded")]
        public bool DeliveryPointsAwarded { get; set; } = false;

        [Display(Name = "Delivery Points Awarded At")]
        public DateTime? DeliveryPointsAwardedAt { get; set; }

        [Display(Name = "Delivery Points Amount")]
        public int DeliveryPointsAmount { get; set; } = 0;

        public DateTime? CancelledAt { get; set; }
        public DateTime? ReturnedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Cancellation Reason")]
        public string? CancellationReason { get; set; }

        [StringLength(500)]
        [Display(Name = "Return Reason")]
        public string? ReturnReason { get; set; }

        // Return/Refund Details
        [Display(Name = "Return Images")]
        public string? ReturnImages { get; set; } // JSON array of image paths

        [Display(Name = "Preferred Refund Method")]
        public RefundMethod? PreferredRefundMethod { get; set; }

        [StringLength(100)]
        [Display(Name = "Refund Account Number")]
        public string? RefundAccountNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Refund Account Holder Name")]
        public string? RefundAccountHolderName { get; set; }

        [StringLength(100)]
        [Display(Name = "Bank Name")]
        public string? RefundBankName { get; set; }

        [Display(Name = "Prefers Replacement")]
        public bool PrefersReplacement { get; set; } = false;

        // Refund Status Tracking
        [Display(Name = "Refund Status")]
        public RefundStatus RefundStatus { get; set; } = RefundStatus.Pending;

        [Display(Name = "Refund Status Changed At")]
        public DateTime? RefundStatusChangedAt { get; set; }

        [Display(Name = "Referred To Accounting")]
        public bool ReferredToAccounting { get; set; } = false;

        [Display(Name = "Referred To Accounting At")]
        public DateTime? ReferredToAccountingAt { get; set; }

        [StringLength(100)]
        [Display(Name = "Referred To")]
        public string? ReferredToAccountingName { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Approved Refund Amount")]
        public decimal? ApprovedRefundAmount { get; set; }

        [StringLength(1000)]
        [Display(Name = "Refund Admin Notes")]
        public string? RefundAdminNotes { get; set; }

        // Actual Refund Payment Details (filled when refund is processed)
        [Display(Name = "Refund Payment Method")]
        public RefundMethod? RefundPaymentMethod { get; set; }

        [StringLength(50)]
        [Display(Name = "Refund Paid From Account")]
        public string? RefundPaidFromAccount { get; set; }

        [StringLength(50)]
        [Display(Name = "Refund Receiver Number")]
        public string? RefundReceiverNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Refund Transaction ID")]
        public string? RefundTransactionId { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Refund Paid Amount")]
        public decimal? RefundPaidAmount { get; set; }

        [Display(Name = "Refund Payment Date")]
        public DateTime? RefundPaymentDate { get; set; }

        [StringLength(200)]
        [Display(Name = "Refund Payment Notes")]
        public string? RefundPaymentNotes { get; set; }

        // COD tracking - customer refused to receive
        [Display(Name = "Customer Refused Delivery")]
        public bool CustomerRefusedDelivery { get; set; } = false;

        [Display(Name = "COD Penalty Applied")]
        public bool CODPenaltyApplied { get; set; } = false;

        // Coupon
        public int? CouponId { get; set; }

        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }

        [StringLength(50)]
        public string? CouponCode { get; set; }

        // Reward Vouchers Applied
        [Display(Name = "Applied Discount Reward")]
        public int? AppliedRewardId { get; set; }

        [ForeignKey("AppliedRewardId")]
        public virtual UserRedeemedReward? AppliedReward { get; set; }

        [Display(Name = "Applied Free Shipping Reward")]
        public int? AppliedFreeShippingRewardId { get; set; }

        [ForeignKey("AppliedFreeShippingRewardId")]
        public virtual UserRedeemedReward? AppliedFreeShippingReward { get; set; }

        [Display(Name = "Reward Discount Amount")]
        [DataType(DataType.Currency)]
        public decimal RewardDiscountAmount { get; set; } = 0m;

        [Display(Name = "Free Shipping Applied")]
        public bool FreeShippingApplied { get; set; } = false;

        [Display(Name = "Wallet Amount Used")]
        [DataType(DataType.Currency)]
        public decimal WalletAmountUsed { get; set; } = 0m;

        [Display(Name = "Premium Discount Amount")]
        [DataType(DataType.Currency)]
        public decimal PremiumDiscountAmount { get; set; } = 0m;

        [Display(Name = "Is Premium Order")]
        public bool IsPremiumOrder { get; set; } = false;

        // Seller (for marketplace orders)
        public int? SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        // Foreign key for ApplicationUser (optional - for guest checkout)
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    }
}
