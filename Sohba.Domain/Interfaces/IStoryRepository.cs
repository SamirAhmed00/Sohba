using Sohba.Domain.Entities.StoryAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IStoryRepository : IGenericRepository<Story>
    {
        Task<IEnumerable<Story>> GetActiveStoriesAsync(Guid userId);
        Task<IEnumerable<Story>> GetStoriesForFeedAsync(Guid currentUserId);
        Task AddViewerAsync(Guid storyId, Guid userId);        
        Task<bool> HasUserViewedStoryAsync(Guid storyId, Guid userId);
        Task<int> GetViewersCountAsync(Guid storyId);
        Task DeleteExpiredStoriesAsync();
        Task<IEnumerable<Story>> GetUserStoriesAsync(Guid userId, Guid currentUserId);
        Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId);

        Task<IEnumerable<StoryViewer>> GetViewersAsync(Guid storyId);
        Task<StoryReaction?> GetReactionAsync(Guid storyId, Guid userId);
        Task<int> GetReactionCountAsync(Guid storyId);
        Task<Dictionary<Guid, int>> GetReactionCountsForStoriesAsync(IEnumerable<Guid> storyIds);
        Task<Dictionary<Guid, StoryReaction>> GetUserReactionsForStoriesAsync(IEnumerable<Guid> storyIds, Guid userId);

        void AddReaction(StoryReaction reaction);
        void RemoveReaction(StoryReaction reaction);
        Task<(IReadOnlyList<Story> Items, int TotalCount)> GetStoriesAdminPagedAsync(int page, int pageSize);


    }
}
