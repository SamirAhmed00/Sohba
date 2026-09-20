using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sohba.Application.Interfaces;
using Sohba.Application.Settings;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Sohba.Application.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private const int RawTokenByteCount = 64;

        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly JwtSettings _jwtSettings;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(
            IRefreshTokenRepository refreshTokens,
            IOptions<JwtSettings> jwtSettings,
            TimeProvider timeProvider,
            ILogger<RefreshTokenService> logger)
        {
            _refreshTokens = refreshTokens;
            _jwtSettings = jwtSettings.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<Result<string>> IssueAsync(Guid userId, string? createdByIp)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var raw = GenerateRawToken();

            _refreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = Hash(raw),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
                CreatedByIp = Truncate(createdByIp)
            });

            await _refreshTokens.SaveChangesAsync();
            return Result<string>.Success(raw);
        }

        public async Task<Result<RefreshResult>> RotateAsync(string? rawToken, string? requestIp)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return Result<RefreshResult>.Failure("Refresh token is missing.");

            var stored = await _refreshTokens.GetByTokenHashAsync(Hash(rawToken));
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (stored == null)
                return Result<RefreshResult>.Failure("Invalid refresh token.");

            if (stored.RevokedAtUtc != null)
            {
                // REUSE DETECTION: a revoked token is being replayed.
                // Safest simple family strategy: revoke every active token of the user.
                var active = await _refreshTokens.GetActiveByUserIdAsync(stored.UserId);
                foreach (var token in active)
                {
                    token.RevokedAtUtc = now;
                    token.RevokedByIp = Truncate(requestIp);
                    _refreshTokens.Update(token);
                }
                await _refreshTokens.SaveChangesAsync();

                _logger.LogWarning(
                    "Refresh token reuse detected for user {UserId}; revoked {Count} active token(s).",
                    stored.UserId, active.Count);

                return Result<RefreshResult>.Failure(
                    "Refresh token reuse detected. All sessions were revoked; please sign in again.");
            }

            if (stored.ExpiresAtUtc <= now)
                return Result<RefreshResult>.Failure("Refresh token has expired. Please sign in again.");

            var newRaw = GenerateRawToken();
            var newHash = Hash(newRaw);

            stored.RevokedAtUtc = now;
            stored.RevokedByIp = Truncate(requestIp);
            stored.ReplacedByTokenHash = newHash;
            _refreshTokens.Update(stored);

            _refreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = stored.UserId,
                TokenHash = newHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
                CreatedByIp = Truncate(requestIp)
            });

            await _refreshTokens.SaveChangesAsync();
            return Result<RefreshResult>.Success(new RefreshResult(stored.UserId, newRaw));
        }

        public async Task<Result> RevokeAsync(string? rawToken, string? revokedByIp)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return Result.Failure("Refresh token is missing.");

            var stored = await _refreshTokens.GetByTokenHashAsync(Hash(rawToken));
            if (stored == null || stored.RevokedAtUtc != null)
                return Result.Success(); // idempotent

            stored.RevokedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            stored.RevokedByIp = Truncate(revokedByIp);
            _refreshTokens.Update(stored);
            await _refreshTokens.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> RevokeAllForUserAsync(Guid userId, string? revokedByIp)
        {
            var active = await _refreshTokens.GetActiveByUserIdAsync(userId);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            foreach (var token in active)
            {
                token.RevokedAtUtc = now;
                token.RevokedByIp = Truncate(revokedByIp);
                _refreshTokens.Update(token);
            }
            await _refreshTokens.SaveChangesAsync();
            return Result.Success();
        }

        private static string GenerateRawToken()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(RawTokenByteCount));

        private static string Hash(string rawToken)
            => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));

        private static string? Truncate(string? value)
            => string.IsNullOrEmpty(value) ? null : value[..Math.Min(value.Length, 64)];
    }
}