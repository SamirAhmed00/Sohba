using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IPageRepository : IGenericRepository<Page>
    {
        void AddFollower(PageFollower follower);
        void RemoveFollower(Guid userId, Guid pageId);
        Task<IEnumerable<Page>> GetPagesByFollowerIdAsync(Guid userId);
        Task<IEnumerable<Page>> SearchPagesAsync(string query, int limit = 10);
        Task<bool> IsFollowingAsync(Guid userId, Guid pageId);
        Task<int> GetFollowersCountAsync(Guid pageId);
        Task<IEnumerable<PageFollower>> GetFollowersAsync(Guid pageId, int page = 1, int pageSize = 20);
    }
}
