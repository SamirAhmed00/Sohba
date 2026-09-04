using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;
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

        // Returns the role name (e.g. "Admin", "Member", "PageOwner", "CoAdmin") or "None" if the user is not a follower.
        string GetUserRoleInPage(Guid userId, Guid pageId);

        // Returns the role as the enum (null when the user is not a follower).
        Task<PageRole?> GetUserRoleInPageAsync(Guid userId, Guid pageId);

        Task<int> GetAdminCountAsync(Guid pageId);
        Task<int> GetRoleCountAsync(Guid pageId, PageRole role);
        Task<PageFollower?> GetFollowerAsync(Guid userId, Guid pageId);
        Task<PageFollower?> GetEarliestAdminAsync(Guid pageId);

        // Case-insensitive name uniqueness check used during Page creation.
        Task<bool> ExistsByNameAsync(string name);

        Task<IEnumerable<Page>> GetPagesToDiscoverAsync(Guid userId, int count = 5);

        void AddFollowRequest(PageFollowRequest request);
        Task<PageFollowRequest?> GetFollowRequestByIdAsync(Guid requestId);
        Task<PageFollowRequest?> GetPendingFollowRequestAsync(Guid pageId, Guid userId);
        Task<IEnumerable<PageFollowRequest>> GetPendingFollowRequestsAsync(Guid pageId);
        Task<IEnumerable<PageFollowRequest>> GetPendingFollowRequestsForUserPagesAsync(Guid adminUserId);
        Task<bool> HasPendingRequestAsync(Guid pageId, Guid userId);
    }
}
