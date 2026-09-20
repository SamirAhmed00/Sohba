using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId);
        void Add(RefreshToken token);
        void Update(RefreshToken token);
        Task<int> SaveChangesAsync();
    }
}
