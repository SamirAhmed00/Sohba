using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class HashtagRepository : GenericRepository<Hashtag>, IHashtagRepository
    {
        public HashtagRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Hashtag>> GetTrendingHashtagsAsync(int count = 10)
        {
            return await _context.Hashtags
                .OrderByDescending(h => h.Count)
                .ThenByDescending(h => h.UpdatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Hashtag?> GetHashtagByTagAsync(string tag)
        {
            return await _context.Hashtags
                .FirstOrDefaultAsync(h => h.Tag == tag);
        }

        public async Task IncrementHashtagCountAsync(string tag)
        {
            var hashtag = await GetHashtagByTagAsync(tag);
            if (hashtag != null)
            {
                hashtag.Count++;
                hashtag.UpdatedAt = DateTime.UtcNow;
                _context.Hashtags.Update(hashtag);
            }
        }

        public async Task<(IEnumerable<Hashtag> Items, int TotalCount)> GetTrendingHashtagsPagedAsync(int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _context.Hashtags
                .Where(h => h.Count > 0)
                .OrderByDescending(h => h.Count)
                .ThenByDescending(h => h.UpdatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }

}
