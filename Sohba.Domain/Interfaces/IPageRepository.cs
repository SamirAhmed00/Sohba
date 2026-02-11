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
    }
}
