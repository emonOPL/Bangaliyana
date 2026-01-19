using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface IOtpService
    {
        /// <summary>
        /// Generates a new OTP code for the given identifier
        /// </summary>
        Task<string> GenerateOtpAsync(string identifier, OtpType type);

        /// <summary>
        /// Validates the OTP code
        /// </summary>
        Task<OtpValidationResult> ValidateOtpAsync(string identifier, string code, OtpType type);

        /// <summary>
        /// Sends OTP via email
        /// </summary>
        Task<bool> SendOtpViaEmailAsync(string email, string code, OtpType type);

        /// <summary>
        /// Sends OTP via SMS (placeholder for future implementation)
        /// </summary>
        Task<bool> SendOtpViaSmsAsync(string phone, string code, OtpType type);

        /// <summary>
        /// Generates and sends OTP in one call
        /// </summary>
        Task<OtpSendResult> GenerateAndSendOtpAsync(string identifier, OtpType type, bool isPhone = false);

        /// <summary>
        /// Cleans up expired OTP codes
        /// </summary>
        Task CleanupExpiredOtpsAsync();

        /// <summary>
        /// Checks if rate limit is exceeded for the identifier
        /// </summary>
        Task<bool> IsRateLimitExceededAsync(string identifier, OtpType type);

        /// <summary>
        /// Invalidates all OTP codes for an identifier
        /// </summary>
        Task InvalidateOtpsAsync(string identifier, OtpType type);
    }

    public class OtpValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public int RemainingAttempts { get; set; }

        public static OtpValidationResult Success() => new() { IsValid = true };
        public static OtpValidationResult Failed(string message, int remainingAttempts = 0) => new()
        {
            IsValid = false,
            ErrorMessage = message,
            RemainingAttempts = remainingAttempts
        };
    }

    public class OtpSendResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public static OtpSendResult Succeeded(DateTime expiresAt) => new()
        {
            Success = true,
            ExpiresAt = expiresAt
        };

        public static OtpSendResult Failed(string message) => new()
        {
            Success = false,
            ErrorMessage = message
        };
    }
}
