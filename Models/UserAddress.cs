using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum AddressType
    {
        Home,
        Office,
        Other
    }

    public class UserAddress
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Address Label")]
        public string Label { get; set; } = string.Empty;

        [Display(Name = "Address Type")]
        public AddressType AddressType { get; set; } = AddressType.Home;

        [Required]
        [StringLength(100)]
        [Display(Name = "Recipient Name")]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "PhoneMustBe11Digits")]
        [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "PhoneMustStartWithValidPrefix")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Division")]
        public int? DivisionId { get; set; }

        [ForeignKey("DivisionId")]
        public virtual Division? Division { get; set; }

        [Display(Name = "District")]
        public int? DistrictId { get; set; }

        [ForeignKey("DistrictId")]
        public virtual District? District { get; set; }

        [Display(Name = "Upazila")]
        public int? UpazilaId { get; set; }

        [ForeignKey("UpazilaId")]
        public virtual Upazila? Upazila { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Full Address")]
        public string FullAddress { get; set; } = string.Empty;

        [StringLength(10)]
        [RegularExpression(@"^\d*$", ErrorMessage = "PostalCodeMustBeDigitsOnly")]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [Display(Name = "Is Default")]
        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
