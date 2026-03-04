using Sohba.Domain.Entities.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IPostRepository : IGenericRepository<Post>
    {
        Task<IEnumerable<Post>> GetTimelineAsync(Guid userId);
        Task<Dictionary<Guid, (int comments, int reactions)>> GetPostsCountsAsync(List<Guid> postIds);
        bool IsPostDeleted(Guid postId);
       

        // New method to add hashtags with location
        Task AddHashtagsToPostAsync(Guid postId, IEnumerable<string> hashtags, string location);

        Task<IEnumerable<Post>> GetGroupPostsAsync(Guid groupId);
        Task<IEnumerable<Post>> GetPagePostsAsync(Guid pageId);
        Task<IEnumerable<Post>> GetUserPostsAsync(Guid userId);

        Task<IEnumerable<Post>> SearchPostsAsync(string query, int limit = 10);
    }
}
