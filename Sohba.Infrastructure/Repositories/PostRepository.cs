using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.PostAggregate;
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


        public async Task AddHashtagsToPostAsync(Guid postId, IEnumerable<string> hashtagTexts)
        {
            if (!hashtagTexts.Any()) return;

            var uniqueTags = hashtagTexts.Distinct().ToList();

            foreach (var tagText in uniqueTags)
            {
                var hashtag = await _context.Set<Hashtag>()
                    .FirstOrDefaultAsync(h => h.Tag == tagText);

                if (hashtag == null)
                {
                    hashtag = new Hashtag { Tag = tagText, CreatedAt = DateTime.UtcNow };
                    _context.Set<Hashtag>().Add(hashtag);
                }

                var postHashtag = new PostHashtag
                {
                    PostId = postId,
                    Hashtag = hashtag 
                };

                _context.Set<PostHashtag>().Add(postHashtag);
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

    }
}
