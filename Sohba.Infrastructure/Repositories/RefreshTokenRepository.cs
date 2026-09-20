using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sohba.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        // Tracked on purpose: Rotate/Revoke mutate the fetched entity.
        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
            => _context.Set<RefreshToken>().FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        public Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId)
            => _context.Set<RefreshToken>()
                       .Where(rt => rt.UserId == userId
                                    && rt.RevokedAtUtc == null
                                    && rt.ExpiresAtUtc > DateTime.UtcNow)
                       .ToListAsync();

        public void Add(RefreshToken token)
            => _context.Set<RefreshToken>().Add(token);

        public void Update(RefreshToken token)
            => _context.Set<RefreshToken>().Update(token);

        public Task<int> SaveChangesAsync()
            => _context.SaveChangesAsync();
    }
}