using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class SellerPaymentSettings
    {
        public int Id { get; set; }

        [Display(Name = "Minimum Payout Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumPayoutAmount { get; set; } = 500m;

        [Display(Name = "Payment Day of Month")]
        [Range(1, 28)]
        public int PaymentDayOfMonth { get; set; } = 1;

        [Display(Name = "Payment Hour (24h format, Bangladesh Time)")]
        [Range(0, 23)]
        public int PaymentHour { get; set; } = 0; // 12:00 AM

        [Display(Name = "Auto-process Payments")]
        public bool AutoProcessPayments { get; set; } = true;

        [Display(Name = "Send Email Notifications")]
        public bool SendEmailNotifications { get; set; } = true;

        [Display(Name = "Require Bank Account Verification")]
        public bool RequireBankAccountVerification { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
