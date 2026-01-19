using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum BankChangeRequestStatus
    {
        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "Approved")]
        Approved,

        [Display(Name = "Rejected")]
        Rejected
    }

    public class SellerBankAccountChangeRequest
    {
        public int Id { get; set; }

        [Required]
        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        // Reference to the existing bank account being changed (null if adding new)
        public int? ExistingBankAccountId { get; set; }

        [ForeignKey("ExistingBankAccountId")]
        public virtual SellerBankAccount? ExistingBankAccount { get; set; }

        // ============ NEW BANK DETAILS ============
        [Display(Name = "Account Type")]
        public BankAccountType NewAccountType { get; set; } = BankAccountType.Bank;

        [Required]
        [StringLength(100)]
        [Display(Name = "Bank/Provider Name")]
        public string NewBankName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Branch Name")]
        public string? NewBranchName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Account Holder Name")]
        public string NewAccountHolderName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string NewAccountNumber { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Routing Number")]
        public string? NewRoutingNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Checkbook Photo URL")]
        public string? NewCheckbookPhotoUrl { get; set; }

        // ============ REQUEST STATUS ============
        [Display(Name = "Status")]
        public BankChangeRequestStatus Status { get; set; } = BankChangeRequestStatus.Pending;

        [StringLength(500)]
        [Display(Name = "Reason for Change")]
        public string? ChangeReason { get; set; }

        [StringLength(500)]
        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        [Display(Name = "Reviewed By")]
        public string? ReviewedByUserId { get; set; }

        [ForeignKey("ReviewedByUserId")]
        public virtual ApplicationUser? ReviewedBy { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}
