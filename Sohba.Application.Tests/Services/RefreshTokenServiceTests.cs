using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sohba.Application.Services;
using Sohba.Application.Settings;
using Sohba.Application.Tests.TestDoubles;
using Sohba.Domain.Entities.UserAggregate;

namespace Sohba.Application.Tests.Services
{
    /// <summary>
    /// Complete lifecycle tests for the refresh-token service: issuance,
    /// hashing, rotation, expiration, reuse detection, revocation and
    /// bulk revocation, all driven by a deterministic fake clock.
    /// </summary>
    public class RefreshTokenServiceTests
    {
        private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private readonly FixedTimeProvider _clock = new(new DateTimeOffset(FixedNow));
        private readonly FakeRefreshTokenRepository _repository;
        private readonly JwtSettings _settings;
        private readonly RefreshTokenService _sut;

        public RefreshTokenServiceTests()
        {
            _settings = new JwtSettings
            {
                Key = new string('x', 64),
                Issuer = "test-issuer",
                Audience = "test-audience",
                AccessTokenLifetimeMinutes = 60,
                RefreshTokenLifetimeDays = 14
            };
            _repository = new FakeRefreshTokenRepository(_clock);
            _sut = new RefreshTokenService(_repository, Options.Create(_settings), _clock, NullLogger<RefreshTokenService>.Instance);
        }

        private static string ExpectedHash(string raw) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        private RefreshToken SeedActiveToken(Guid userId, DateTime? expiresAt = null)
        {
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = ExpectedHash("seeded-raw-" + Guid.NewGuid()),
                CreatedAtUtc = FixedNow.AddDays(-1),
                ExpiresAtUtc = expiresAt ?? FixedNow.AddDays(14)
            };
            _repository.Add(token);
            return token;
        }

        // ---- IssueAsync ----

        /// <summary>Verifies that issuing a token returns a raw token and stores only its SHA-256 hash, never the raw value.</summary>
        [Fact]
        public async Task IssueAsync_StoresHashedTokenNotRaw_ReturnsRawToken()
        {
            var userId = Guid.NewGuid();

            var result = await _sut.IssueAsync(userId, "10.0.0.1");

            Assert.True(result.IsSuccess);
            Assert.False(string.IsNullOrEmpty(result.Value));

            var stored = Assert.Single(_repository.Tokens);
            Assert.Equal(userId, stored.UserId);
            Assert.NotEqual(result.Value, stored.TokenHash);
            Assert.Equal(ExpectedHash(result.Value), stored.TokenHash);
            Assert.Matches("^[0-9A-F]{64}$", stored.TokenHash);
        }

        /// <summary>Verifies that a newly issued token is created now and expires exactly after the configured lifetime.</summary>
        [Fact]
        public async Task IssueAsync_UsesConfiguredLifetimeForExpiry()
        {
            await _sut.IssueAsync(Guid.NewGuid(), null);

            var stored = Assert.Single(_repository.Tokens);
            Assert.Equal(FixedNow, stored.CreatedAtUtc);
            Assert.Equal(FixedNow.AddDays(14), stored.ExpiresAtUtc);
            Assert.Null(stored.CreatedByIp);
        }

        /// <summary>Verifies that a custom refresh lifetime setting flows into the token expiry.</summary>
        [Fact]
        public async Task IssueAsync_WithCustomLifetime_AppliesConfiguredDays()
        {
            _settings.RefreshTokenLifetimeDays = 7;

            await _sut.IssueAsync(Guid.NewGuid(), null);

            Assert.Equal(FixedNow.AddDays(7), _repository.Tokens.Single().ExpiresAtUtc);
        }

        /// <summary>Verifies that the issuing IP is stored on the token.</summary>
        [Fact]
        public async Task IssueAsync_StoresCreatedByIp()
        {
            await _sut.IssueAsync(Guid.NewGuid(), "192.168.1.1");

            Assert.Equal("192.168.1.1", _repository.Tokens.Single().CreatedByIp);
        }

        /// <summary>Verifies that an IP longer than 64 characters is truncated on issue.</summary>
        [Fact]
        public async Task IssueAsync_TruncatesLongIpAddress()
        {
            await _sut.IssueAsync(Guid.NewGuid(), new string('a', 100));

            Assert.Equal(64, _repository.Tokens.Single().CreatedByIp!.Length);
        }

        /// <summary>Verifies that a missing IP is stored as null rather than an empty string.</summary>
        [Fact]
        public async Task IssueAsync_WithEmptyIp_StoresNull()
        {
            await _sut.IssueAsync(Guid.NewGuid(), "");

            Assert.Null(_repository.Tokens.Single().CreatedByIp);
        }

        // ---- RotateAsync: invalid inputs ----

        /// <summary>Verifies that rotation with a missing token fails with a "missing" error.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RotateAsync_WhenTokenIsMissing_ReturnsFailure(string? rawToken)
        {
            var result = await _sut.RotateAsync(rawToken, "10.0.0.1");

            Assert.True(result.IsFailure);
            Assert.Contains("missing", result.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, _repository.SaveCount);
        }

        /// <summary>Verifies that rotation with an unknown token fails without touching state.</summary>
        [Fact]
        public async Task RotateAsync_WhenTokenIsUnknown_ReturnsFailure()
        {
            var result = await _sut.RotateAsync("totally-unknown-token", "10.0.0.1");

            Assert.True(result.IsFailure);
            Assert.Contains("Invalid refresh token", result.Error);
            Assert.Empty(_repository.Tokens);
            Assert.Equal(0, _repository.SaveCount);
        }

        // ---- RotateAsync: success path ----

        /// <summary>Verifies that rotating a valid token revokes it, issues a different replacement, and preserves the user binding.</summary>
        [Fact]
        public async Task RotateAsync_WhenTokenIsValid_RotatesToNewTokenForSameUser()
        {
            var userId = Guid.NewGuid();
            var issued = await _sut.IssueAsync(userId, null);

            var result = await _sut.RotateAsync(issued.Value, "10.0.0.9");

            Assert.True(result.IsSuccess);
            Assert.Equal(userId, result.Value.UserId);
            Assert.NotEqual(issued.Value, result.Value.NewRawToken);

            var oldToken = _repository.Tokens.Single(t => t.TokenHash == ExpectedHash(issued.Value));
            Assert.Equal(FixedNow, oldToken.RevokedAtUtc);
            Assert.Equal("10.0.0.9", oldToken.RevokedByIp);
            Assert.Equal(ExpectedHash(result.Value.NewRawToken), oldToken.ReplacedByTokenHash);

            var replacement = _repository.Tokens.Single(t => t.TokenHash == ExpectedHash(result.Value.NewRawToken));
            Assert.Equal(userId, replacement.UserId);
            Assert.Null(replacement.RevokedAtUtc);
            Assert.Equal(FixedNow, replacement.CreatedAtUtc);
            Assert.Equal(FixedNow.AddDays(14), replacement.ExpiresAtUtc);
        }

        /// <summary>Verifies that the rotation chain links the old token's ReplacedByTokenHash to the new stored hash.</summary>
        [Fact]
        public async Task RotateAsync_ReplacementHashMatchesStoredNewToken()
        {
            var issued = await _sut.IssueAsync(Guid.NewGuid(), null);

            var result = await _sut.RotateAsync(issued.Value, null);

            var replacementHash = ExpectedHash(result.Value.NewRawToken);
            var oldToken = _repository.Tokens.Single(t => t.TokenHash == ExpectedHash(issued.Value));
            Assert.NotNull(oldToken.ReplacedByTokenHash);
            Assert.Equal(replacementHash, oldToken.ReplacedByTokenHash);
            Assert.Contains(_repository.Tokens, t => t.TokenHash == replacementHash);
        }

        /// <summary>Verifies that a long request IP is truncated during rotation.</summary>
        [Fact]
        public async Task RotateAsync_TruncatesLongRequestIp()
        {
            var issued = await _sut.IssueAsync(Guid.NewGuid(), null);

            await _sut.RotateAsync(issued.Value, new string('b', 80));

            var oldToken = _repository.Tokens.Single(t => t.TokenHash == ExpectedHash(issued.Value));
            Assert.Equal(64, oldToken.RevokedByIp!.Length);
        }

        // ---- RotateAsync: expiration ----

        /// <summary>Verifies the expiry boundary: rotation fails exactly at the expiration instant.</summary>
        [Fact]
        public async Task RotateAsync_WhenTokenExpiresExactlyNow_ReturnsFailure()
        {
            var issued = await _sut.IssueAsync(Guid.NewGuid(), null);
            _clock.Advance(TimeSpan.FromDays(14));

            var result = await _sut.RotateAsync(issued.Value, null);

            Assert.True(result.IsFailure);
            Assert.Contains("expired", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the expiry boundary: rotation succeeds one tick before expiration.</summary>
        [Fact]
        public async Task RotateAsync_OneTickBeforeExpiration_Succeeds()
        {
            var issued = await _sut.IssueAsync(Guid.NewGuid(), null);
            _clock.Advance(TimeSpan.FromDays(14).Subtract(TimeSpan.FromTicks(1)));

            var result = await _sut.RotateAsync(issued.Value, null);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that rotating an expired token fails and leaves the stored token untouched.</summary>
        [Fact]
        public async Task RotateAsync_WhenTokenIsExpired_DoesNotMutateStoredState()
        {
            var issued = await _sut.IssueAsync(Guid.NewGuid(), null);
            _clock.Advance(TimeSpan.FromDays(30));
            var savesBefore = _repository.SaveCount;

            await _sut.RotateAsync(issued.Value, null);

            var stored = _repository.Tokens.Single();
            Assert.Null(stored.RevokedAtUtc);
            Assert.Null(stored.ReplacedByTokenHash);
            Assert.Equal(savesBefore, _repository.SaveCount);
        }

        // ---- RotateAsync: reuse detection ----

        /// <summary>Verifies that replaying a revoked token triggers token-family revocation and fails.</summary>
        [Fact]
        public async Task RotateAsync_WhenRevokedTokenIsReplayed_RevokesAllActiveTokensAndFails()
        {
            var userId = Guid.NewGuid();
            var raw1 = await _sut.IssueAsync(userId, null);
            await _sut.RotateAsync(raw1.Value, "10.0.0.1");
            var raw2 = await _sut.IssueAsync(userId, null);

            _clock.Advance(TimeSpan.FromMinutes(5));

            // Replay of the already-rotated (revoked) first token.
            var result = await _sut.RotateAsync(raw1.Value, "10.0.0.2");

            Assert.True(result.IsFailure);
            Assert.Contains("reuse detected", result.Error, StringComparison.OrdinalIgnoreCase);

            // All active tokens of the user, including the replacement and raw2, are now revoked.
            var revoked = _repository.Tokens
                .Where(t => t.TokenHash != ExpectedHash(raw1.Value))
                .ToList();
            Assert.All(revoked, t => Assert.Equal(FixedNow.AddMinutes(5), t.RevokedAtUtc));
            Assert.All(revoked, t => Assert.Equal("10.0.0.2", t.RevokedByIp));
        }

        /// <summary>Verifies that replaying a revoked token when no active tokens remain fails without throwing.</summary>
        [Fact]
        public async Task RotateAsync_WhenRevokedTokenReplayedWithNoActiveTokens_FailsGracefully()
        {
            var raw = await _sut.IssueAsync(Guid.NewGuid(), null);
            await _sut.RotateAsync(raw.Value, null);

            var result = await _sut.RotateAsync(raw.Value, null);

            Assert.True(result.IsFailure);
            Assert.Contains("reuse detected", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that reuse detection only revokes the affected user's tokens, not other users' tokens.</summary>
        [Fact]
        public async Task RotateAsync_ReuseDetection_DoesNotTouchOtherUsersTokens()
        {
            var otherRaw = await _sut.IssueAsync(Guid.NewGuid(), null);

            var raw = await _sut.IssueAsync(Guid.NewGuid(), null);
            await _sut.RotateAsync(raw.Value, null);

            await _sut.RotateAsync(raw.Value, null);

            var otherToken = _repository.Tokens.Single(t => t.TokenHash == ExpectedHash(otherRaw.Value));
            Assert.Null(otherToken.RevokedAtUtc);
        }

        // ---- RevokeAsync ----

        /// <summary>Verifies that revoking a missing token fails.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RevokeAsync_WhenTokenIsMissing_ReturnsFailure(string? rawToken)
        {
            var result = await _sut.RevokeAsync(rawToken, "10.0.0.1");

            Assert.True(result.IsFailure);
            Assert.Equal(0, _repository.SaveCount);
        }

        /// <summary>Verifies that revoking an unknown token is idempotently successful with no state change.</summary>
        [Fact]
        public async Task RevokeAsync_WhenTokenIsUnknown_ReturnsSuccessWithoutChanges()
        {
            var result = await _sut.RevokeAsync("unknown-token", "10.0.0.1");

            Assert.True(result.IsSuccess);
            Assert.Empty(_repository.Tokens);
            Assert.Equal(0, _repository.SaveCount);
        }

        /// <summary>Verifies that revoking an active token stamps revocation time, IP and persists.</summary>
        [Fact]
        public async Task RevokeAsync_WhenTokenIsActive_RevokesAtCurrentTime()
        {
            var raw = await _sut.IssueAsync(Guid.NewGuid(), null);
            var savesBefore = _repository.SaveCount;

            var result = await _sut.RevokeAsync(raw.Value, "10.0.0.3");

            Assert.True(result.IsSuccess);
            var stored = _repository.Tokens.Single();
            Assert.Equal(FixedNow, stored.RevokedAtUtc);
            Assert.Equal("10.0.0.3", stored.RevokedByIp);
            Assert.Equal(savesBefore + 1, _repository.SaveCount);
        }

        /// <summary>Verifies that revoking an already-revoked token is idempotent and preserves the original revocation metadata.</summary>
        [Fact]
        public async Task RevokeAsync_WhenAlreadyRevoked_IsIdempotent()
        {
            var raw = await _sut.IssueAsync(Guid.NewGuid(), null);
            await _sut.RevokeAsync(raw.Value, "10.0.0.3");

            _clock.Advance(TimeSpan.FromHours(2));
            var result = await _sut.RevokeAsync(raw.Value, "10.9.9.9");

            Assert.True(result.IsSuccess);
            var stored = _repository.Tokens.Single();
            Assert.Equal(FixedNow, stored.RevokedAtUtc);
            Assert.Equal("10.0.0.3", stored.RevokedByIp);
        }

        // ---- RevokeAllForUserAsync ----

        /// <summary>Verifies that revoking all tokens for a user revokes every active token at the current time.</summary>
        [Fact]
        public async Task RevokeAllForUserAsync_WithActiveTokens_RevokesAllOfThem()
        {
            var userId = Guid.NewGuid();
            await _sut.IssueAsync(userId, null);
            await _sut.IssueAsync(userId, null);

            var result = await _sut.RevokeAllForUserAsync(userId, "10.0.0.4");

            Assert.True(result.IsSuccess);
            Assert.All(_repository.Tokens, t =>
            {
                Assert.Equal(FixedNow, t.RevokedAtUtc);
                Assert.Equal("10.0.0.4", t.RevokedByIp);
            });
        }

        /// <summary>Verifies that bulk revocation succeeds for a user with no tokens and still persists the no-op save.</summary>
        [Fact]
        public async Task RevokeAllForUserAsync_WithNoTokens_ReturnsSuccess()
        {
            var savesBefore = _repository.SaveCount;

            var result = await _sut.RevokeAllForUserAsync(Guid.NewGuid(), null);

            Assert.True(result.IsSuccess);
            Assert.Equal(savesBefore + 1, _repository.SaveCount);
        }

        /// <summary>Verifies that bulk revocation only affects tokens of the requested user.</summary>
        [Fact]
        public async Task RevokeAllForUserAsync_DoesNotTouchOtherUsersTokens()
        {
            var userId = Guid.NewGuid();
            await _sut.IssueAsync(userId, null);
            await _sut.IssueAsync(Guid.NewGuid(), null);

            await _sut.RevokeAllForUserAsync(userId, null);

            Assert.Single(_repository.Tokens, t => t.RevokedAtUtc != null);
        }

        /// <summary>Verifies that already-revoked tokens are skipped by bulk revocation (their revocation time is preserved).</summary>
        [Fact]
        public async Task RevokeAllForUserAsync_SkipsAlreadyRevokedTokens()
        {
            var userId = Guid.NewGuid();
            var raw1 = await _sut.IssueAsync(userId, null);
            await _sut.RevokeAsync(raw1.Value, "10.0.0.5");
            var firstRevocation = _repository.Tokens.Single().RevokedAtUtc;

            _clock.Advance(TimeSpan.FromHours(1));
            await _sut.RevokeAllForUserAsync(userId, "10.0.0.6");

            Assert.Equal(firstRevocation, _repository.Tokens.Single().RevokedAtUtc);
        }
    }
}
