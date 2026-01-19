using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;

namespace Bangaliyana.Services
{
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<OtpService> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        private const int OTP_LENGTH = 6;
        private const int OTP_EXPIRY_MINUTES = 5;
        private const int MAX_ATTEMPTS = 3;
        private const int RATE_LIMIT_MINUTES = 1;  // Minimum time between OTP requests
        private const int MAX_OTP_PER_HOUR = 5;    // Maximum OTPs per hour per identifier

        public OtpService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<OtpService> logger,
            IHostEnvironment hostEnvironment)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<string> GenerateOtpAsync(string identifier, OtpType type)
        {
            // Invalidate any existing unused OTPs for this identifier and type
            await InvalidateOtpsAsync(identifier, type);

            // Generate secure random 6-digit code
            var code = GenerateSecureCode();

            var otpCode = new OtpCode
            {
                Identifier = identifier.ToLowerInvariant(),
                Type = type,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
                IsUsed = false,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.OtpCodes.Add(otpCode);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated OTP for {Identifier} of type {Type}", identifier, type);

            return code;
        }

        public async Task<OtpValidationResult> ValidateOtpAsync(string identifier, string code, OtpType type)
        {
            var normalizedIdentifier = identifier.ToLowerInvariant();

            var otpCode = await _context.OtpCodes
                .Where(o => o.Identifier == normalizedIdentifier
                         && o.Type == type
                         && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpCode == null)
            {
                _logger.LogWarning("No OTP found for {Identifier}", identifier);
                return OtpValidationResult.Failed("No OTP code found. Please request a new one.");
            }

            if (otpCode.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("OTP expired for {Identifier}", identifier);
                return OtpValidationResult.Failed("OTP code has expired. Please request a new one.");
            }

            if (otpCode.AttemptCount >= MAX_ATTEMPTS)
            {
                _logger.LogWarning("Max attempts exceeded for {Identifier}", identifier);
                return OtpValidationResult.Failed("Too many failed attempts. Please request a new OTP.");
            }

            if (otpCode.Code != code)
            {
                otpCode.AttemptCount++;
                await _context.SaveChangesAsync();

                var remaining = MAX_ATTEMPTS - otpCode.AttemptCount;
                _logger.LogWarning("Invalid OTP attempt for {Identifier}. Remaining: {Remaining}", identifier, remaining);
                return OtpValidationResult.Failed($"Invalid OTP code. {remaining} attempts remaining.", remaining);
            }

            // Mark as used
            otpCode.IsUsed = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("OTP validated successfully for {Identifier}", identifier);
            return OtpValidationResult.Success();
        }

        public async Task<bool> SendOtpViaEmailAsync(string email, string code, OtpType type)
        {
            try
            {
                var subject = type switch
                {
                    OtpType.Login => "Your Login OTP - Bangaliyana",
                    OtpType.Registration => "Verify Your Email - Bangaliyana",
                    OtpType.PasswordReset => "Password Reset OTP - Bangaliyana",
                    OtpType.PhoneVerification => "Verify Your Phone Number - Bangaliyana",
                    _ => "Your OTP Code - Bangaliyana"
                };

                var typeDescription = type switch
                {
                    OtpType.Login => "login",
                    OtpType.Registration => "email verification",
                    OtpType.PasswordReset => "password reset",
                    OtpType.PhoneVerification => "phone verification",
                    _ => "verification"
                };

                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #198754; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; background-color: #f8f9fa; }}
        .otp-code {{ font-size: 32px; font-weight: bold; color: #198754; text-align: center;
                    padding: 20px; background-color: white; border-radius: 8px; margin: 20px 0;
                    letter-spacing: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Bangaliyana</h1>
        </div>
        <div class='content'>
            <h2>Your {typeDescription} OTP Code</h2>
            <p>Please use the following OTP code to complete your {typeDescription}:</p>
            <div class='otp-code'>{code}</div>
            <p><strong>This code will expire in {OTP_EXPIRY_MINUTES} minutes.</strong></p>
            <p>If you didn't request this code, please ignore this email or contact support if you have concerns.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Bangaliyana. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

                await _emailService.SendEmailAsync(email, subject, body);
                _logger.LogInformation("OTP email sent to {Email} for {Type}", email, type);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendOtpViaSmsAsync(string phone, string code, OtpType type)
        {
            var typeDescription = type switch
            {
                OtpType.Login => "login",
                OtpType.Registration => "registration",
                OtpType.PasswordReset => "password reset",
                OtpType.PhoneVerification => "phone verification",
                _ => "verification"
            };

            if (_hostEnvironment.IsDevelopment())
            {
                // In development, SMS is not actually sent
                _logger.LogInformation("SMS OTP sent to {Phone} for {Type} (Development Mode)", phone, typeDescription);
                await Task.CompletedTask;
                return true;
            }

            // TODO: Implement SMS gateway integration for production
            // Integrate with an SMS provider like Twilio, MessageBird, AWS SNS, etc.
            _logger.LogWarning("SMS gateway not configured for {Phone}", phone);
            await Task.CompletedTask;
            return true;
        }

        public async Task<OtpSendResult> GenerateAndSendOtpAsync(string identifier, OtpType type, bool isPhone = false)
        {
            // Check rate limit
            if (await IsRateLimitExceededAsync(identifier, type))
            {
                return OtpSendResult.Failed($"Please wait before requesting another OTP. You can request a new OTP every {RATE_LIMIT_MINUTES} minute(s).");
            }

            // Generate OTP
            var code = await GenerateOtpAsync(identifier, type);
            var expiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES);

            // Send OTP
            bool sent;
            if (isPhone)
            {
                sent = await SendOtpViaSmsAsync(identifier, code, type);
            }
            else
            {
                sent = await SendOtpViaEmailAsync(identifier, code, type);
            }

            if (!sent)
            {
                return OtpSendResult.Failed("Failed to send OTP. Please try again.");
            }

            return OtpSendResult.Succeeded(expiresAt);
        }

        public async Task CleanupExpiredOtpsAsync()
        {
            var expiredOtps = await _context.OtpCodes
                .Where(o => o.ExpiresAt < DateTime.UtcNow || o.IsUsed)
                .Where(o => o.CreatedAt < DateTime.UtcNow.AddDays(-1))  // Keep for 1 day for audit
                .ToListAsync();

            if (expiredOtps.Any())
            {
                _context.OtpCodes.RemoveRange(expiredOtps);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired OTP codes", expiredOtps.Count);
            }
        }

        public async Task<bool> IsRateLimitExceededAsync(string identifier, OtpType type)
        {
            var normalizedIdentifier = identifier.ToLowerInvariant();

            // Check if there's a recent OTP within rate limit window
            var recentOtp = await _context.OtpCodes
                .Where(o => o.Identifier == normalizedIdentifier && o.Type == type)
                .Where(o => o.CreatedAt > DateTime.UtcNow.AddMinutes(-RATE_LIMIT_MINUTES))
                .AnyAsync();

            if (recentOtp)
            {
                return true;
            }

            // Check hourly limit
            var hourlyCount = await _context.OtpCodes
                .Where(o => o.Identifier == normalizedIdentifier && o.Type == type)
                .Where(o => o.CreatedAt > DateTime.UtcNow.AddHours(-1))
                .CountAsync();

            return hourlyCount >= MAX_OTP_PER_HOUR;
        }

        public async Task InvalidateOtpsAsync(string identifier, OtpType type)
        {
            var normalizedIdentifier = identifier.ToLowerInvariant();

            var existingOtps = await _context.OtpCodes
                .Where(o => o.Identifier == normalizedIdentifier
                         && o.Type == type
                         && !o.IsUsed)
                .ToListAsync();

            foreach (var otp in existingOtps)
            {
                otp.IsUsed = true;
            }

            await _context.SaveChangesAsync();
        }

        private static string GenerateSecureCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);

            var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return number.ToString().PadLeft(OTP_LENGTH, '0');
        }
    }
}
