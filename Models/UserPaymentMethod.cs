using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class UserPaymentMethod
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(20)]
        public string ProviderType { get; set; } = string.Empty; // bKash, Nagad, Rocket, Upay

        [Required]
        [StringLength(11)]
        public string MobileNumber { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Helper properties
        [NotMapped]
        public string MaskedNumber => MobileNumber.Length >= 4
            ? $"****{MobileNumber[^4..]}"
            : MobileNumber;

        [NotMapped]
        public string ProviderName => ProviderType switch
        {
            "bKash" => "bKash",
            "Nagad" => "Nagad",
            "Rocket" => "Rocket",
            "Upay" => "Upay",
            _ => ProviderType
        };

        [NotMapped]
        public string Icon => ProviderType switch
        {
            "bKash" => "fas fa-mobile-alt",
            "Nagad" => "fas fa-mobile-alt",
            "Rocket" => "fas fa-mobile-alt",
            "Upay" => "fas fa-mobile-alt",
            _ => "fas fa-credit-card"
        };

        [NotMapped]
        public string IconColor => ProviderType switch
        {
            "bKash" => "#E2136E",
            "Nagad" => "#F6921E",
            "Rocket" => "#8C3494",
            "Upay" => "#00BCD4",
            _ => "#6B7280"
        };
    }
}
