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


        public async Task<(IEnumerable<Post> Items, int TotalCount)> GetTimelineAsync(
            Guid userId,
            int page = 1,
            int pageSize = 10)
        {
            var friendIds = await _context.Friends
                .Where(f => (f.UserId == userId || f.FriendUserId == userId)
                            && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.UserId == userId ? f.FriendUserId : f.UserId)
                .ToListAsync();

            var visibleUserIds = new List<Guid> { userId };
            visibleUserIds.AddRange(friendIds);

            // Build the query (don't execute yet)
            var query = _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => !p.IsDeleted && !p.IsHidden
                            && (p.SourceType == PostSourceType.User || p.SourceId == null)
                            && (
                                p.UserId == userId ||
                                (p.Privacy == PostPrivacy.Public && visibleUserIds.Contains(p.UserId)) ||
                                (p.Privacy == PostPrivacy.Friends && friendIds.Contains(p.UserId))
                            ))
                .OrderByDescending(p => p.CreatedAt);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }


        


        public async Task AddHashtagsToPostAsync(Guid postId, IEnumerable<string> hashtags, string location)
        {
            var tagList = hashtags.ToList();
            if (!tagList.Any()) return;
            
            var existingHashtags = await _context.Hashtags
                .Where(h => tagList.Contains(h.Tag))
                .ToDictionaryAsync(h => h.Tag);
            
            foreach (var tagText in tagList)
            {

                if (!existingHashtags.TryGetValue(tagText, out var hashtag))
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


        
        public async Task<IEnumerable<Post>> SearchPostsAsync(string query,Guid currentUserId, int limit = 10)
        {
            var friendIds = await _context.Friends
                 .Where(f => (f.UserId == currentUserId || f.FriendUserId == currentUserId)
                             && f.Status == FriendshipStatus.Accepted)
                 .Select(f => f.UserId == currentUserId ? f.FriendUserId : f.UserId)
                 .ToListAsync();

            return await _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => !p.IsDeleted &&
                           (p.Title.Contains(query) ||
                            p.Content.Contains(query)) &&
                           // Privacy: own posts, public posts, or friends' posts
                           (p.UserId == currentUserId ||
                            p.Privacy == PostPrivacy.Public ||
                            (p.Privacy == PostPrivacy.Friends && friendIds.Contains(p.UserId))))
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
        public async Task<IEnumerable<Post>> GetPostsByHashtagAsync(string tag)
        {
            return await _context.Set<PostHashtag>()
                .Include(ph => ph.Post)
                    .ThenInclude(p => p.User)
                .Where(ph => ph.Hashtag.Tag == tag && !ph.Post.IsDeleted)
                .Select(ph => ph.Post)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }


        public async Task<IEnumerable<Post>> GetRecentAsync(int count)
        {
            return await _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
