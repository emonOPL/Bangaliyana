using Bangaliyana.Data;
using Bangaliyana.Models;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Bangaliyana.Controllers
{
    /// <summary>
    /// API Controller for WebAuthn/FIDO2 biometric authentication (fingerprint, Face ID)
    /// Uses Fido2NetLib for proper cryptographic verification
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BiometricAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IFido2 _fido2;
        private readonly ILogger<BiometricAuthController> _logger;

        public BiometricAuthController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IFido2 fido2,
            ILogger<BiometricAuthController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _fido2 = fido2;
            _logger = logger;
        }

        #region Registration Endpoints

        /// <summary>
        /// Get registration options for WebAuthn credential creation
        /// </summary>
        [HttpPost("register/options")]
        [Authorize]
        public async Task<IActionResult> GetRegistrationOptions([FromBody] RegistrationOptionsRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }

                // Clean up expired challenges
                await CleanupExpiredChallenges();

                // Get existing credentials to exclude (prevent re-registration of same authenticator)
                // Handle both new credentials (with CredentialIdBytes) and legacy credentials (only CredentialId string)
                var existingCredentialsList = await _context.WebAuthnCredentials
                    .Where(c => c.UserId == user.Id && c.IsActive)
                    .ToListAsync();

                var existingCredentials = existingCredentialsList
                    .Where(c => c.CredentialIdBytes != null || !string.IsNullOrEmpty(c.CredentialId))
                    .Select(c => new PublicKeyCredentialDescriptor(
                        c.CredentialIdBytes ?? Base64UrlDecode(c.CredentialId)))
                    .ToList();

                // Create Fido2User
                var fido2User = new Fido2User
                {
                    Id = Encoding.UTF8.GetBytes(user.Id),
                    Name = user.Email ?? user.UserName ?? "user",
                    DisplayName = user.FullName ?? user.UserName ?? user.Email ?? "User"
                };

                // Configure authenticator selection - allow any authenticator type
                var authenticatorSelection = new AuthenticatorSelection
                {
                    UserVerification = UserVerificationRequirement.Required,
                    RequireResidentKey = false,
                    AuthenticatorAttachment = null // Allow any authenticator type (platform or cross-platform)
                };

                // Create registration options using Fido2NetLib
                var options = _fido2.RequestNewCredential(
                    fido2User,
                    existingCredentials,
                    authenticatorSelection,
                    AttestationConveyancePreference.None);

                // Store the challenge in database for verification
                var challenge = new WebAuthnChallenge
                {
                    Challenge = Base64UrlEncode(options.Challenge),
                    UserId = user.Id,
                    ChallengeType = "registration",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    CreatedAt = DateTime.UtcNow
                };
                _context.WebAuthnChallenges.Add(challenge);
                await _context.SaveChangesAsync();

                // Store options in session for verification (Fido2NetLib requirement)
                HttpContext.Session.SetString($"fido2.attestationOptions.{user.Id}", options.ToJson());

                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registration options");
                return BadRequest(new { error = "Failed to get registration options: " + ex.Message });
            }
        }

        /// <summary>
        /// Complete WebAuthn credential registration with proper attestation verification
        /// </summary>
        [HttpPost("register/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteRegistration([FromBody] AuthenticatorAttestationRawResponse attestationResponse)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }

                // Get stored options from session
                var optionsJson = HttpContext.Session.GetString($"fido2.attestationOptions.{user.Id}");
                if (string.IsNullOrEmpty(optionsJson))
                {
                    return BadRequest(new { error = "Registration session expired. Please try again." });
                }

                var options = CredentialCreateOptions.FromJson(optionsJson);

                // Callback to check if credential ID is unique
                IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, cancellationToken) =>
                {
                    var exists = await _context.WebAuthnCredentials
                        .AnyAsync(c => c.CredentialIdBytes == args.CredentialId, cancellationToken);
                    return !exists;
                };

                // Verify the attestation response using Fido2NetLib
                var result = await _fido2.MakeNewCredentialAsync(
                    attestationResponse,
                    options,
                    callback);

                if (result.Result == null)
                {
                    return BadRequest(new { error = "Registration verification failed" });
                }

                // Store the credential
                var credential = new WebAuthnCredential
                {
                    UserId = user.Id,
                    CredentialIdBytes = result.Result.CredentialId,
                    CredentialId = Base64UrlEncode(result.Result.CredentialId),
                    PublicKeyBytes = result.Result.PublicKey,
                    PublicKey = Convert.ToBase64String(result.Result.PublicKey),
                    UserHandle = Encoding.UTF8.GetBytes(user.Id),
                    SignatureCounter = result.Result.Counter,
                    AaGuid = result.Result.Aaguid,
                    AAGUID = result.Result.Aaguid.ToString(),
                    CredentialType = result.Result.CredType.ToString(),
                    AuthenticatorType = GetAuthenticatorType(attestationResponse),
                    DeviceName = GetDeviceNameFromUserAgent(),
                    IsActive = true,
                    RegDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.WebAuthnCredentials.Add(credential);

                // Clean up the session and challenge
                HttpContext.Session.Remove($"fido2.attestationOptions.{user.Id}");
                var challenges = await _context.WebAuthnChallenges
                    .Where(c => c.UserId == user.Id && c.ChallengeType == "registration")
                    .ToListAsync();
                _context.WebAuthnChallenges.RemoveRange(challenges);

                await _context.SaveChangesAsync();

                _logger.LogInformation("WebAuthn credential registered for user {UserId}, CredentialId: {CredentialId}",
                    user.Id, credential.CredentialId);

                return Ok(new
                {
                    success = true,
                    message = "Biometric credential registered successfully",
                    credentialId = credential.Id
                });
            }
            catch (Fido2VerificationException ex)
            {
                _logger.LogWarning(ex, "Fido2 verification failed during registration");
                return BadRequest(new { error = "Registration verification failed: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing WebAuthn registration");
                return BadRequest(new { error = "Registration failed: " + ex.Message });
            }
        }

        #endregion

        #region Authentication Endpoints

        /// <summary>
        /// Get authentication options for WebAuthn login
        /// </summary>
        [HttpPost("login/options")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAuthenticationOptions([FromBody] AuthenticationOptionsRequest request)
        {
            try
            {
                // Find the user by email
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    // Return generic error to prevent user enumeration
                    return BadRequest(new { error = "No biometric credentials found" });
                }

                // Get user's active credentials
                // Handle both new credentials (with CredentialIdBytes) and legacy credentials (only CredentialId string)
                var credentialsList = await _context.WebAuthnCredentials
                    .Where(c => c.UserId == user.Id && c.IsActive)
                    .ToListAsync();

                if (!credentialsList.Any())
                {
                    return BadRequest(new { error = "No biometric credentials found" });
                }

                var credentials = credentialsList
                    .Where(c => c.CredentialIdBytes != null || !string.IsNullOrEmpty(c.CredentialId))
                    .Select(c => new PublicKeyCredentialDescriptor(
                        c.CredentialIdBytes ?? Base64UrlDecode(c.CredentialId)))
                    .ToList();

                // Clean up expired challenges
                await CleanupExpiredChallenges();

                // Create assertion options using Fido2NetLib
                var options = _fido2.GetAssertionOptions(
                    credentials,
                    UserVerificationRequirement.Required);

                // Store challenge for this specific user/email
                var challenge = new WebAuthnChallenge
                {
                    Challenge = Base64UrlEncode(options.Challenge),
                    UserId = user.Id,
                    Email = request.Email,
                    ChallengeType = "authentication",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    CreatedAt = DateTime.UtcNow
                };
                _context.WebAuthnChallenges.Add(challenge);
                await _context.SaveChangesAsync();

                // Store options in session
                HttpContext.Session.SetString($"fido2.assertionOptions.{request.Email}", options.ToJson());

                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authentication options");
                return BadRequest(new { error = "Failed to get authentication options: " + ex.Message });
            }
        }

        /// <summary>
        /// Complete WebAuthn authentication with proper signature verification
        /// </summary>
        [HttpPost("login/complete")]
        [AllowAnonymous]
        public async Task<IActionResult> CompleteAuthentication([FromBody] AuthenticationCompleteRequest request)
        {
            try
            {
                // Find the credential by ID
                // First try to find by byte array, then fallback to string for legacy credentials
                var credentialIdBytes = Base64UrlDecode(request.Id);
                var credentialIdString = request.Id;

                var credential = await _context.WebAuthnCredentials
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CredentialIdBytes == credentialIdBytes && c.IsActive);

                // Fallback: try to find by string CredentialId for legacy credentials
                if (credential == null)
                {
                    credential = await _context.WebAuthnCredentials
                        .Include(c => c.User)
                        .FirstOrDefaultAsync(c => c.CredentialId == credentialIdString && c.IsActive);
                }

                if (credential == null || credential.User == null)
                {
                    return BadRequest(new { error = "Credential not found" });
                }

                // Get stored options from session using the user's email
                var optionsJson = HttpContext.Session.GetString($"fido2.assertionOptions.{credential.User.Email}");
                if (string.IsNullOrEmpty(optionsJson))
                {
                    return BadRequest(new { error = "Authentication session expired. Please try again." });
                }

                var options = AssertionOptions.FromJson(optionsJson);

                // Verify the challenge is for this user (prevents challenge reuse attack)
                var storedChallenge = await _context.WebAuthnChallenges
                    .Where(c => c.UserId == credential.UserId &&
                               c.ChallengeType == "authentication" &&
                               c.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                if (storedChallenge == null)
                {
                    return BadRequest(new { error = "Challenge expired or not found" });
                }

                // Create the authenticator assertion response
                var assertionResponse = new AuthenticatorAssertionRawResponse
                {
                    Id = credentialIdBytes,
                    RawId = credentialIdBytes,
                    Type = PublicKeyCredentialType.PublicKey,
                    Response = new AuthenticatorAssertionRawResponse.AssertionResponse
                    {
                        ClientDataJson = Base64UrlDecode(request.Response.ClientDataJSON),
                        AuthenticatorData = Base64UrlDecode(request.Response.AuthenticatorData),
                        Signature = Base64UrlDecode(request.Response.Signature),
                        UserHandle = string.IsNullOrEmpty(request.Response.UserHandle)
                            ? null
                            : Base64UrlDecode(request.Response.UserHandle)
                    }
                };

                // Check if this is a legacy credential without proper COSE public key
                if (credential.PublicKeyBytes == null || credential.PublicKeyBytes.Length == 0)
                {
                    _logger.LogWarning("Legacy credential detected for user {UserId}. Credential needs re-registration.", credential.UserId);
                    return BadRequest(new {
                        error = "This biometric credential needs to be re-registered for enhanced security. Please delete this credential and register again.",
                        needsReRegistration = true
                    });
                }

                // Callback to get stored credential counter
                IsUserHandleOwnerOfCredentialIdAsync callback = async (args, cancellationToken) =>
                {
                    var cred = await _context.WebAuthnCredentials
                        .FirstOrDefaultAsync(c => c.UserHandle == args.UserHandle, cancellationToken);
                    return cred != null;
                };

                // Verify the assertion using Fido2NetLib (this does signature verification!)
                var result = await _fido2.MakeAssertionAsync(
                    assertionResponse,
                    options,
                    credential.PublicKeyBytes,
                    credential.SignatureCounter,
                    callback);

                if (result.Status != "ok")
                {
                    _logger.LogWarning("WebAuthn assertion failed: {Error}", result.ErrorMessage);
                    return BadRequest(new { error = "Authentication failed: " + result.ErrorMessage });
                }

                // Update credential with new signature counter (prevents replay attacks)
                credential.SignatureCounter = result.Counter;
                credential.LastUsedAt = DateTime.UtcNow;
                credential.UseCount++;
                credential.UpdatedAt = DateTime.UtcNow;

                // Clean up session and challenge
                HttpContext.Session.Remove($"fido2.assertionOptions.{credential.User.Email}");
                _context.WebAuthnChallenges.Remove(storedChallenge);

                await _context.SaveChangesAsync();

                // Sign in the user
                await _signInManager.SignInAsync(credential.User, isPersistent: request.RememberMe);

                _logger.LogInformation("User {UserId} logged in via WebAuthn, Counter: {Counter}",
                    credential.UserId, result.Counter);

                return Ok(new
                {
                    success = true,
                    message = "Authentication successful",
                    returnUrl = request.ReturnUrl ?? "/"
                });
            }
            catch (Fido2VerificationException ex)
            {
                _logger.LogWarning(ex, "Fido2 verification failed during authentication");
                return BadRequest(new { error = "Authentication verification failed: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing WebAuthn authentication");
                return BadRequest(new { error = "Authentication failed: " + ex.Message });
            }
        }

        #endregion

        #region Credential Management

        /// <summary>
        /// Get list of user's registered biometric credentials
        /// </summary>
        [HttpGet("credentials")]
        [Authorize]
        public async Task<IActionResult> GetCredentials()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var credentials = await _context.WebAuthnCredentials
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    id = c.Id,
                    deviceName = c.DeviceName ?? "Unknown Device",
                    authenticatorType = c.AuthenticatorType,
                    isActive = c.IsActive,
                    lastUsedAt = c.LastUsedAt,
                    useCount = c.UseCount,
                    createdAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(credentials);
        }

        /// <summary>
        /// Delete a biometric credential
        /// </summary>
        [HttpDelete("credentials/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCredential(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var credential = await _context.WebAuthnCredentials
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (credential == null)
            {
                return NotFound(new { error = "Credential not found" });
            }

            _context.WebAuthnCredentials.Remove(credential);
            await _context.SaveChangesAsync();

            _logger.LogInformation("WebAuthn credential {CredentialId} deleted for user {UserId}", id, user.Id);

            return Ok(new { success = true, message = "Credential deleted successfully" });
        }

        /// <summary>
        /// Update credential name
        /// </summary>
        [HttpPut("credentials/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCredential(int id, [FromBody] UpdateCredentialRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var credential = await _context.WebAuthnCredentials
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (credential == null)
            {
                return NotFound(new { error = "Credential not found" });
            }

            credential.DeviceName = request.DeviceName;
            credential.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Credential updated successfully" });
        }

        /// <summary>
        /// Check if user has biometric credentials registered
        /// </summary>
        [HttpGet("has-credentials")]
        [AllowAnonymous]
        public async Task<IActionResult> HasCredentials([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "Email is required" });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Return false to prevent user enumeration
                return Ok(new { hasCredentials = false });
            }

            var hasCredentials = await _context.WebAuthnCredentials
                .AnyAsync(c => c.UserId == user.Id && c.IsActive);

            return Ok(new { hasCredentials });
        }

        /// <summary>
        /// Check if platform authenticator is available (for UI)
        /// </summary>
        [HttpGet("platform-available")]
        [AllowAnonymous]
        public IActionResult IsPlatformAuthenticatorAvailable()
        {
            // This is checked client-side, but we return true to indicate server support
            return Ok(new { serverSupport = true });
        }

        #endregion

        #region Helper Methods

        private async Task CleanupExpiredChallenges()
        {
            var expired = await _context.WebAuthnChallenges
                .Where(c => c.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expired.Any())
            {
                _context.WebAuthnChallenges.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }
        }

        private string GetAuthenticatorType(AuthenticatorAttestationRawResponse response)
        {
            // Try to determine authenticator type from the response
            // Platform authenticators are typically built-in (fingerprint, Face ID)
            // Cross-platform are typically security keys
            return "platform"; // Default to platform for most biometric use cases
        }

        private string GetDeviceNameFromUserAgent()
        {
            var userAgent = Request.Headers.UserAgent.ToString().ToLower();

            if (userAgent.Contains("iphone"))
                return "iPhone Face ID / Touch ID";
            if (userAgent.Contains("ipad"))
                return "iPad Face ID / Touch ID";
            if (userAgent.Contains("mac"))
                return "Mac Touch ID";
            if (userAgent.Contains("android"))
                return "Android Biometric";
            if (userAgent.Contains("windows"))
                return "Windows Hello";

            return "Biometric Device";
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string data)
        {
            var padded = data.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }

        #endregion
    }

    #region Request/Response Models

    public class RegistrationOptionsRequest
    {
        public string? DeviceName { get; set; }
    }

    public class AuthenticationOptionsRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class AuthenticationCompleteRequest
    {
        public string Id { get; set; } = string.Empty;
        public string RawId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public AssertionResponseData Response { get; set; } = new();
        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class AssertionResponseData
    {
        public string ClientDataJSON { get; set; } = string.Empty;
        public string AuthenticatorData { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string? UserHandle { get; set; }
    }

    public class UpdateCredentialRequest
    {
        public string DeviceName { get; set; } = string.Empty;
    }

    #endregion
}
