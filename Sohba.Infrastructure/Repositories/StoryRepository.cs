using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.StoryAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class StoryRepository : GenericRepository<Story>, IStoryRepository
    {
        public StoryRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Story>> GetActiveStoriesAsync(Guid userId)
        {
            var now = DateTime.UtcNow;

            return await _context.Stories
                .Include(s => s.User)
                .Where(s => s.UserId == userId &&
                           s.ExpiresAt > now &&
                           !s.IsDeleted)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Story>> GetStoriesForFeedAsync(Guid currentUserId)
        {
            var now = DateTime.UtcNow;

            var friendIds = await _context.Friends
                .Where(f => (f.UserId == currentUserId || f.FriendUserId == currentUserId)
                            && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.UserId == currentUserId ? f.FriendUserId : f.UserId)
                .ToListAsync();

            return await _context.Stories
                .Include(s => s.User)
                .Include(s => s.Viewers)
                .Where(s => s.ExpiresAt > now &&
                           !s.IsDeleted &&
                           (
                               s.UserId == currentUserId ||
                               (s.Privacy == StoryPrivacy.Public && friendIds.Contains(s.UserId)) ||
                               (s.Privacy == StoryPrivacy.FriendsOnly && friendIds.Contains(s.UserId))
                           ))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task AddViewerAsync(Guid storyId, Guid userId)
        {
            var viewer = new StoryViewer
            {
                StoryId = storyId,
                UserId = userId,
                ViewedAt = DateTime.UtcNow
            };

            await _context.Set<StoryViewer>().AddAsync(viewer);
        }

        public async Task<bool> HasUserViewedStoryAsync(Guid storyId, Guid userId)
        {
            return await _context.Set<StoryViewer>()
                .AnyAsync(v => v.StoryId == storyId && v.UserId == userId);
        }

        public async Task<int> GetViewersCountAsync(Guid storyId)
        {
            return await _context.Set<StoryViewer>()
                .CountAsync(v => v.StoryId == storyId);
        }

        public async Task DeleteExpiredStoriesAsync()
        {
            var now = DateTime.UtcNow;
            var expiredStories = await _context.Stories
                .Where(s => s.ExpiresAt <= now && !s.IsDeleted)
                .ToListAsync();

            foreach (var story in expiredStories)
            {
                story.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Story>> GetUserStoriesAsync(Guid userId, Guid currentUserId)
        {
            var now = DateTime.UtcNow;

            var isFriend = await _context.Friends
                 .AnyAsync(f =>
                     ((f.UserId == currentUserId && f.FriendUserId == userId)
                      || (f.UserId == userId && f.FriendUserId == currentUserId))
                     && f.Status == FriendshipStatus.Accepted);

            var owner = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var isOwnerAccountPrivate = owner?.IsPrivateAccount ?? false;

            if (userId != currentUserId && isOwnerAccountPrivate && !isFriend)
                return Enumerable.Empty<Story>();

            return await _context.Stories
                .Include(s => s.User)
                .Include(s => s.Viewers)
                .Where(s => s.UserId == userId &&
                           s.ExpiresAt > now &&
                           !s.IsDeleted &&
                           (s.UserId == currentUserId ||
                            s.Privacy == StoryPrivacy.Public ||
                            (s.Privacy == StoryPrivacy.FriendsOnly && isFriend)))
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }


        public async Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId)
        {
            var friendships = await _context.Friends
                .Where(f => (f.UserId == userId || f.FriendUserId == userId)
                            && f.Status == FriendshipStatus.Accepted)
                .ToListAsync();

            var friendIds = friendships.Select(f => f.UserId == userId ? f.FriendUserId : f.UserId).ToList();
            return friendIds;
        }


        public async Task<IEnumerable<StoryViewer>> GetViewersAsync(Guid storyId)
        {
            return await _context.Set<StoryViewer>()
                .Include(v => v.User)
                .Where(v => v.StoryId == storyId)
                .OrderByDescending(v => v.ViewedAt)
                .ToListAsync();
        }

        public async Task<StoryReaction?> GetReactionAsync(Guid storyId, Guid userId)
        {
            return await _context.Set<StoryReaction>()
                .FirstOrDefaultAsync(r => r.StoryId == storyId && r.UserId == userId);
        }

        public async Task<int> GetReactionCountAsync(Guid storyId)
        {
            return await _context.Set<StoryReaction>().CountAsync(r => r.StoryId == storyId);
        }

        public async Task<Dictionary<Guid, int>> GetReactionCountsForStoriesAsync(IEnumerable<Guid> storyIds)
        {
            var idList = storyIds.ToList();
            if (!idList.Any()) return new Dictionary<Guid, int>();

            return await _context.Set<StoryReaction>()
                .Where(r => idList.Contains(r.StoryId))
                .GroupBy(r => r.StoryId)
                .Select(g => new { StoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StoryId, x => x.Count);
        }

        public async Task<Dictionary<Guid, StoryReaction>> GetUserReactionsForStoriesAsync(IEnumerable<Guid> storyIds, Guid userId)
        {
            var idList = storyIds.ToList();
            if (!idList.Any()) return new Dictionary<Guid, StoryReaction>();

            return await _context.Set<StoryReaction>()
                .Where(r => idList.Contains(r.StoryId) && r.UserId == userId)
                .ToDictionaryAsync(r => r.StoryId, r => r);
        }

        public void AddReaction(StoryReaction reaction)
        {
            _context.Set<StoryReaction>().Add(reaction);
        }
        public void RemoveReaction(StoryReaction reaction)
        {
            _context.Set<StoryReaction>().Remove(reaction);
        }

        public async Task<(IReadOnlyList<Story> Items, int TotalCount)> GetStoriesAdminPagedAsync(int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var now = DateTime.UtcNow;
            var query = _context.Stories
                .Include(s => s.User)
                .Where(s => !s.IsDeleted && s.ExpiresAt > now)
                .OrderByDescending(s => s.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

    }
}
