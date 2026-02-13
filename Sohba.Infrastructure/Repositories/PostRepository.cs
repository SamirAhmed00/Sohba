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
    }
}
