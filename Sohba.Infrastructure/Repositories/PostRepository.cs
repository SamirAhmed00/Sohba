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

        public override async Task<Post> GetByIdAsync(Guid id)
        {
            // GenericRepository.GetByIdAsync uses bare FindAsync (no Include), which left
            // User/Group/Page unloaded here. PostResponseDto mapping requires User.Name
            // (AuthorName), causing GetPostByIdAsync (Modal/Details/Edit) to throw and
            // return nothing to the client — this fixes author name and image together.
            return await _context.Set<Post>()
                .Include(p => p.User)
                .Include(p => p.Group)
                .Include(p => p.Page)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

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

            var blockedUserIds = await _context.Friends
                .Where(f => (f.UserId == userId || f.FriendUserId == userId)
                            && f.Status == FriendshipStatus.Blocked)
                .Select(f => f.UserId == userId ? f.FriendUserId : f.UserId)
                .ToListAsync();

            var followedPageIds = await _context.Set<Sohba.Domain.Entities.GroupAndPage.PageFollower>()
                .Where(pf => pf.UserId == userId)
                .Select(pf => pf.PageId)
                .ToListAsync();

            var joinedGroupIds = await _context.Set<Sohba.Domain.Entities.GroupAndPage.GroupMember>()
                .Where(gm => gm.UserId == userId && !gm.IsBanned)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            var query = _context.Set<Post>()
                .Include(p => p.User)
                .Include(p => p.Page)
                .Include(p => p.Group)
                .Where(p => !p.IsDeleted && !p.IsHidden && !blockedUserIds.Contains(p.UserId)
                            && (
                                // User Posts: author's own posts or friends' posts
                                (
                                    (p.SourceType == PostSourceType.User || p.SourceId == null)
                                    && (
                                        p.UserId == userId ||
                                        (
                                            (p.Privacy == PostPrivacy.Public || p.Privacy == PostPrivacy.Friends)
                                            && friendIds.Contains(p.UserId)
                                        )
                                    )
                                )
                                ||
                                // Page Posts: published on pages the user follows
                                (
                                    p.SourceType == PostSourceType.Page
                                    && p.SourceId != null
                                    && followedPageIds.Contains(p.SourceId.Value)
                                )
                                ||
                                // Group Posts: published in groups the user has joined
                                (
                                    p.SourceType == PostSourceType.Group
                                    && p.GroupId != null
                                    && joinedGroupIds.Contains(p.GroupId.Value)
                                )
                            ))
                .OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Post>> GetUserPostsAsync(Guid userId)
        {
            return await _context.Set<Post>()
                .Include(p => p.User)
                .Where(p => p.UserId == userId && p.SourceType == PostSourceType.User && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        public async Task AddHashtagsToPostAsync(Guid postId, IEnumerable<string> hashtags, string location)
        {
            var tagList = hashtags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().TrimStart('#').ToLowerInvariant())
                .Distinct()
                .ToList();

            if (!tagList.Any()) return;

            var existingHashtags = await _context.Hashtags
                .Where(h => tagList.Contains(h.Tag.ToLower()))
                .ToDictionaryAsync(h => h.Tag.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);

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
                    existingHashtags[tagText] = hashtag;
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
                .Include(p => p.Page)
                .Where(p => p.SourceType == PostSourceType.Page && p.SourceId == pageId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }


        public async Task<int> GetNewPostsCountSinceAsync(DateTime sinceUtc)
        {
            return await _context.Set<Post>()
                .CountAsync(p => p.CreatedAt >= sinceUtc);
        }


        public async Task<IEnumerable<Post>> SearchPostsAsync(string query, Guid currentUserId, int limit = 10)
        {
            var friendIds = await _context.Friends
                 .Where(f => (f.UserId == currentUserId || f.FriendUserId == currentUserId)
                             && f.Status == FriendshipStatus.Accepted)
                 .Select(f => f.UserId == currentUserId ? f.FriendUserId : f.UserId)
                 .ToListAsync();

            var blockedUserIds = await _context.Friends
                 .Where(f => (f.UserId == currentUserId || f.FriendUserId == currentUserId)
                             && f.Status == FriendshipStatus.Blocked)
                 .Select(f => f.UserId == currentUserId ? f.FriendUserId : f.UserId)
                 .ToListAsync();

            var userGroupIds = await _context.Set<Sohba.Domain.Entities.GroupAndPage.GroupMember>()
                 .Where(gm => gm.UserId == currentUserId && !gm.IsBanned)
                 .Select(gm => gm.GroupId)
                 .ToListAsync();

            var followedPageIds = await _context.Set<Sohba.Domain.Entities.GroupAndPage.PageFollower>()
                 .Where(pf => pf.UserId == currentUserId)
                 .Select(pf => pf.PageId)
                 .ToListAsync();

            return await _context.Set<Post>()
                .Include(p => p.User)
                .Include(p => p.Group)
                .Include(p => p.Page)
                .Where(p => !p.IsDeleted && !p.IsHidden &&
                           !blockedUserIds.Contains(p.UserId) &&
                           (p.Title.Contains(query) || p.Content.Contains(query)) &&
                           // 1. Own posts
                           (p.UserId == currentUserId ||
                           // 2. Group posts: visible if user is a member OR if the group is public
                           (p.SourceType == PostSourceType.Group && p.GroupId != null &&
                               ((p.Group != null && !p.Group.IsPrivate) || userGroupIds.Contains(p.GroupId.Value))) ||
                           // 3. Page posts: visible if user follows OR is admin OR page is public
                           (p.SourceType == PostSourceType.Page && p.PageId != null &&
                               ((p.Page != null && !p.Page.IsPrivate) || (p.Page != null && p.Page.AdminId == currentUserId) || followedPageIds.Contains(p.PageId.Value))) ||
                           // 4. User posts: public posts or friends' posts
                           (p.SourceType == PostSourceType.User &&
                               (p.Privacy == PostPrivacy.Public ||
                               (p.Privacy == PostPrivacy.Friends && friendIds.Contains(p.UserId))))))
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsByHashtagAsync(string tag, Guid currentUserId = default)
        {
            var query = _context.Set<PostHashtag>()
                .Include(ph => ph.Post)
                    .ThenInclude(p => p.User)
                .Include(ph => ph.Post)
                    .ThenInclude(p => p.Group)
                .Include(ph => ph.Post)
                    .ThenInclude(p => p.Page)
                .Where(ph => ph.Hashtag.Tag == tag && !ph.Post.IsDeleted && !ph.Post.IsHidden);

            if (currentUserId != Guid.Empty)
            {
                var friendIds = await _context.Friends
                    .Where(f => (f.UserId == currentUserId || f.FriendUserId == currentUserId)
                                && f.Status == FriendshipStatus.Accepted)
                    .Select(f => f.UserId == currentUserId ? f.FriendUserId : f.UserId)
                    .ToListAsync();

                var blockedUserIds = await _context.Friends
                    .Where(f => (f.UserId == currentUserId || f.FriendUserId == currentUserId)
                                && f.Status == FriendshipStatus.Blocked)
                    .Select(f => f.UserId == currentUserId ? f.FriendUserId : f.UserId)
                    .ToListAsync();

                var userGroupIds = await _context.Set<Sohba.Domain.Entities.GroupAndPage.GroupMember>()
                    .Where(gm => gm.UserId == currentUserId && !gm.IsBanned)
                    .Select(gm => gm.GroupId)
                    .ToListAsync();

                var followedPageIds = await _context.Set<Sohba.Domain.Entities.GroupAndPage.PageFollower>()
                    .Where(pf => pf.UserId == currentUserId)
                    .Select(pf => pf.PageId)
                    .ToListAsync();

                query = query.Where(ph =>
                    !blockedUserIds.Contains(ph.Post.UserId) &&
                    (ph.Post.UserId == currentUserId ||
                     (ph.Post.SourceType == PostSourceType.Group && ph.Post.GroupId != null &&
                         ((ph.Post.Group != null && !ph.Post.Group.IsPrivate) || userGroupIds.Contains(ph.Post.GroupId.Value))) ||
                     (ph.Post.SourceType == PostSourceType.Page && ph.Post.PageId != null &&
                         ((ph.Post.Page != null && !ph.Post.Page.IsPrivate) || (ph.Post.Page != null && ph.Post.Page.AdminId == currentUserId) || followedPageIds.Contains(ph.Post.PageId.Value))) ||
                     (ph.Post.SourceType == PostSourceType.User &&
                         (ph.Post.Privacy == PostPrivacy.Public ||
                          (ph.Post.Privacy == PostPrivacy.Friends && friendIds.Contains(ph.Post.UserId))))));
            }
            else
            {
                query = query.Where(ph =>
                    ph.Post.Privacy == PostPrivacy.Public &&
                    ph.Post.SourceType == PostSourceType.User);
            }

            return await query
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

        public async Task<(IReadOnlyList<Post> Items, int TotalCount, Dictionary<Guid, (int comments, int reactions)> Counts)> GetPostsAdminPagedAsync(
            string? search,
            string? source,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _context.Set<Post>()
                .Include(p => p.User)
                .Include(p => p.Group)
                .Include(p => p.Page)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.Title.Contains(term) || p.Content.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(source) && !source.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<PostSourceType>(source, true, out var sourceType))
                {
                    query = query.Where(p => p.SourceType == sourceType);
                }
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var postIds = items.Select(p => p.Id).ToList();
            var counts = await GetPostsCountsAsync(postIds);

            return (items, totalCount, counts);
        }
    }
}
