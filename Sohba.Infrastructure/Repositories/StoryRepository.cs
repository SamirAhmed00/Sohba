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
            var cutoffTime = DateTime.UtcNow.AddHours(-24);

            return await _context.Stories
                .Include(s => s.User)
                .Where(s => s.UserId == userId &&
                           s.CreatedAt >= cutoffTime &&
                           !s.IsDeleted)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Story>> GetStoriesForFeedAsync(Guid currentUserId)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);

            // جلب أصدقاء المستخدم (مؤقتاً بنجيب كل الـ public stories)
            // TODO: بعد ما Friendship يشتغل، هنضيف شرط الأصدقاء
            return await _context.Stories
                .Include(s => s.User)
                .Include(s => s.Viewers)
                .Where(s => s.CreatedAt >= cutoffTime &&
                           !s.IsDeleted &&
                           (s.Privacy == StoryPrivacy.Public || s.UserId == currentUserId))
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
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            var expiredStories = await _context.Stories
                .Where(s => s.CreatedAt < cutoffTime && !s.IsDeleted)
                .ToListAsync();

            foreach (var story in expiredStories)
            {
                story.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
