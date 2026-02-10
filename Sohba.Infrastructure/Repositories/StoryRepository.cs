using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities;
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
            // Return stories created in the last 24 hours
            var cutOffTime = DateTime.UtcNow.AddHours(-24);

            return await _context.Set<Story>()
                .Where(s => s.UserId == userId && s.CreatedAt >= cutOffTime && !s.IsDeleted)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }
    }
}
