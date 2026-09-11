using Sohba.Domain.Entities.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IPostRepository : IGenericRepository<Post>
    {
        Task<(IEnumerable<Post> Items, int TotalCount)> GetTimelineAsync(
           Guid userId,
           int page = 1,
           int pageSize = 10);

        Task<Dictionary<Guid, (int comments, int reactions)>> GetPostsCountsAsync(List<Guid> postIds);
        Task<IEnumerable<Post>> GetPostsByHashtagAsync(string tag, Guid currentUserId = default);


        // New method to add hashtags with location
        Task AddHashtagsToPostAsync(Guid postId, IEnumerable<string> hashtags, string location);

        Task<IEnumerable<Post>> GetGroupPostsAsync(Guid groupId);
        Task<IEnumerable<Post>> GetPagePostsAsync(Guid pageId);
        Task<IEnumerable<Post>> GetUserPostsAsync(Guid userId);

        Task<IEnumerable<Post>> SearchPostsAsync(string query, Guid currentUserId, int limit = 10);

        Task<IEnumerable<Post>> GetRecentAsync(int count);

        Task<(IReadOnlyList<Post> Items, int TotalCount, Dictionary<Guid, (int comments, int reactions)> Counts)> GetPostsAdminPagedAsync(
            string? search,
            string? source,
            int page,
            int pageSize);

        Task<int> GetNewPostsCountSinceAsync(DateTime sinceUtc);


    }
}
