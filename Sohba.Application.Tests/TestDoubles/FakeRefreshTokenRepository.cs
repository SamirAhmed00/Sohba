using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Interfaces;

namespace Sohba.Application.Tests.TestDoubles
{
    /// <summary>
    /// Minimal in-memory refresh-token repository. A token is "active"
    /// when it is not revoked and not expired relative to the injected
    /// fake clock, mirroring the RefreshToken.IsActive semantics.
    /// </summary>
    public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly TimeProvider _timeProvider;
        private readonly List<RefreshToken> _tokens = new();

        public FakeRefreshTokenRepository(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        /// <summary>Number of times SaveChangesAsync was invoked (observable side effect).</summary>
        public int SaveCount { get; private set; }

        /// <summary>All tracked tokens, including ones added during the test.</summary>
        public IReadOnlyList<RefreshToken> Tokens => _tokens;

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            return Task.FromResult(_tokens.FirstOrDefault(t => t.TokenHash == tokenHash));
        }

        public Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var active = _tokens
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
                .ToList();

            return Task.FromResult(active);
        }

        public void Add(RefreshToken token) => _tokens.Add(token);

        public void Update(RefreshToken token)
        {
            // Entities are tracked by reference; nothing to do beyond marking a save.
        }

        public Task<int> SaveChangesAsync()
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
