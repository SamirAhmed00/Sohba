using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByUsernameAsync(string username);

        // Removed because we are not using it
        //bool EmailExists(string email);

        Task<IEnumerable<User>> GetRandomUsersAsync(List<Guid> excludeUserIds, int count);
        Task<IEnumerable<User>> SearchUsersAsync(string query, Guid currentUserId, int limit = 10);

    }
}
