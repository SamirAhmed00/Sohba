using System;

namespace Sohba.Domain.Entities.UserAggregate
{
    /// <summary>
    /// A rotating refresh token for the JWT access-token surface.
    /// Only the SHA-256 hash of the raw token is ever persisted.
    /// </summary>
    public class RefreshToken
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        /// <summary>SHA-256 of the raw token, uppercase hex (64 chars). Unique.</summary>
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>Null while the token is active.</summary>
        public DateTime? RevokedAtUtc { get; set; }

        /// <summary>Hash of the token that replaced this one (rotation chain).</summary>
        public string? ReplacedByTokenHash { get; set; }

        public string? CreatedByIp { get; set; }
        public string? RevokedByIp { get; set; }

        public bool IsActive => RevokedAtUtc == null && ExpiresAtUtc > DateTime.UtcNow;
    }
}