using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Stores WebAuthn/FIDO2 credentials for biometric authentication (fingerprint, Face ID)
    /// </summary>
    public class WebAuthnCredential
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        /// <summary>
        /// The credential ID returned by the authenticator (raw bytes)
        /// New credentials use this, old credentials will have null
        /// </summary>
        public byte[]? CredentialIdBytes { get; set; }

        /// <summary>
        /// The credential ID as Base64URL string (for backward compatibility and lookups)
        /// </summary>
        [Required]
        [StringLength(1024)]
        public string CredentialId { get; set; } = string.Empty;

        /// <summary>
        /// The public key in COSE format (raw bytes for cryptographic operations)
        /// New credentials use this, old credentials will have null
        /// </summary>
        public byte[]? PublicKeyBytes { get; set; }

        /// <summary>
        /// The public key as Base64 string (for backward compatibility)
        /// </summary>
        [Required]
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>
        /// User handle (user ID in byte format for WebAuthn)
        /// </summary>
        public byte[]? UserHandle { get; set; }

        /// <summary>
        /// The signature counter to prevent replay attacks
        /// </summary>
        public uint SignatureCounter { get; set; } = 0;

        /// <summary>
        /// The type of authenticator (platform = built-in biometric, cross-platform = security key)
        /// </summary>
        [StringLength(50)]
        public string AuthenticatorType { get; set; } = "platform";

        /// <summary>
        /// User-friendly name for the credential (e.g., "iPhone Face ID", "Windows Hello")
        /// </summary>
        [StringLength(100)]
        public string? DeviceName { get; set; }

        /// <summary>
        /// AAGUID of the authenticator (helps identify device type) as Guid
        /// </summary>
        [Column("AaGuidValue")]
        public Guid AaGuid { get; set; } = Guid.Empty;

        /// <summary>
        /// AAGUID as string (for backward compatibility)
        /// </summary>
        [StringLength(64)]
        public string? AAGUID { get; set; }

        /// <summary>
        /// Credential type (typically "public-key")
        /// </summary>
        [StringLength(32)]
        public string CredentialType { get; set; } = "public-key";

        /// <summary>
        /// Whether this credential is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last time this credential was used for authentication
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Number of times this credential has been used
        /// </summary>
        public int UseCount { get; set; } = 0;

        /// <summary>
        /// Registration date and time
        /// </summary>
        public DateTime RegDate { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Get credential descriptor for Fido2 operations
        /// </summary>
        [NotMapped]
        public Fido2NetLib.Objects.PublicKeyCredentialDescriptor Descriptor => new(CredentialIdBytes);
    }

    /// <summary>
    /// Stores WebAuthn challenges temporarily during registration/authentication
    /// </summary>
    public class WebAuthnChallenge
    {
        public int Id { get; set; }

        /// <summary>
        /// The challenge value (Base64 encoded)
        /// </summary>
        [Required]
        [StringLength(256)]
        public string Challenge { get; set; } = string.Empty;

        /// <summary>
        /// User ID (null for authentication challenges before user is identified)
        /// </summary>
        [StringLength(450)]
        public string? UserId { get; set; }

        /// <summary>
        /// Email for authentication challenges (used to identify user)
        /// </summary>
        [StringLength(256)]
        public string? Email { get; set; }

        /// <summary>
        /// Type of challenge: "registration" or "authentication"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ChallengeType { get; set; } = "registration";

        /// <summary>
        /// When this challenge expires (typically 5 minutes)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
