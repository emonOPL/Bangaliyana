using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum BankAccountType
    {
        Bank,
        MobileBanking,
        Other
    }

    public class SellerBankAccount
    {
        public int Id { get; set; }

        [Required]
        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        [Display(Name = "Account Type")]
        public BankAccountType AccountType { get; set; } = BankAccountType.Bank;

        [Required]
        [StringLength(100)]
        [Display(Name = "Bank/Provider Name")]
        public string BankName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Branch Name")]
        public string? BranchName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Account Holder Name")]
        public string AccountHolderName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Routing Number")]
        public string? RoutingNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Checkbook Photo URL")]
        public string? CheckbookPhotoUrl { get; set; }

        [Display(Name = "Is Primary")]
        public bool IsPrimary { get; set; } = false;

        [Display(Name = "Is Verified")]
        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
