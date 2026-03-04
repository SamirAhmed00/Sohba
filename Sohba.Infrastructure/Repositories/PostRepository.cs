using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class PostRepository : GenericRepository<Post>, IPostRepository
    {
        public PostRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Post>> GetTimelineAsync(Guid userId)
        {
            return await _context.Set<Post>()
                        .Include(p => p.User)
                        .Where(p => !p.IsDeleted)
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();
        }

        public bool IsPostDeleted(Guid postId)
        {
            return _context.Set<Post>().Any(p => p.Id == postId && p.IsDeleted);
        }


        public async Task AddHashtagsToPostAsync(Guid postId, IEnumerable<string> hashtags, string location)
        {
            foreach (var tagText in hashtags)
            {
                var hashtag = await _context.Hashtags.FirstOrDefaultAsync(h => h.Tag == tagText);

                if (hashtag == null)
                {
                    hashtag = new Hashtag
                    {
                        Id = Guid.NewGuid(),
                        Tag = tagText,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Location = location,
                        Count = 1
                    };
                    _context.Hashtags.Add(hashtag);
                }
                else
                {
                    hashtag.Count++; 
                    hashtag.UpdatedAt = DateTime.UtcNow;
                }

                _context.PostHashtags.Add(new PostHashtag { PostId = postId, HashtagId = hashtag.Id });
            }
        }
        public async Task<Dictionary<Guid, (int comments, int reactions)>> GetPostsCountsAsync(List<Guid> postIds)
        {
            var commentsCounts = await _context.Comments
                .Where(c => postIds.Contains(c.PostId))
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToListAsync();

            var reactionsCounts = await _context.Reactions
                .Where(r => postIds.Contains(r.PostId))
                .GroupBy(r => r.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<Guid, (int, int)>();

            var commentsDict = commentsCounts.ToDictionary(x => x.PostId, x => x.Count);
            var reactionsDict = reactionsCounts.ToDictionary(x => x.PostId, x => x.Count);

            foreach (var id in postIds)
            {
                commentsDict.TryGetValue(id, out var comments);
                reactionsDict.TryGetValue(id, out var reactions);

                result[id] = (comments, reactions);
            }

            return result;
        }

        public async Task<IEnumerable<Post>> GetGroupPostsAsync(Guid groupId)
        {
            return await _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => p.SourceType == PostSourceType.Group && p.SourceId == groupId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPagePostsAsync(Guid pageId)
        {
            return await _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => p.SourceType == PostSourceType.Page && p.SourceId == pageId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetUserPostsAsync(Guid userId)
        {
            return await _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => p.UserId == userId && p.SourceType == PostSourceType.User && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> SearchPostsAsync(string query, int limit = 10)
        {
            return await _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => !p.IsDeleted &&
                           (p.Title.Contains(query) ||
                            p.Content.Contains(query)))
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
