using Sohba.Domain.Entities.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IPostRepository : IGenericRepository<Post>
    {
        Task<IEnumerable<Post>> GetTimelineAsync(Guid userId);
        bool IsPostDeleted(Guid postId);
        Task AddHashtagsToPostAsync(Guid postId, IEnumerable<string> hashtags);
    }
}
