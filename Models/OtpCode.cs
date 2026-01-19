using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    public class OtpCode
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Identifier { get; set; } = string.Empty;  // Email or Phone

        public OtpType Type { get; set; }  // Login, Registration, PasswordReset

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;  // 6-digit code

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; }

        public int AttemptCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum OtpType
    {
        Login = 0,
        Registration = 1,
        PasswordReset = 2,
        PhoneVerification = 3
    }
}
