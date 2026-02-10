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
            // Logic to get posts for the user's timeline (e.g., from friends or public)
            return await _context.Set<Post>()
                .Where(p => !p.IsDeleted) // Basic check based on IPostRepository requirement
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public bool IsPostDeleted(Guid postId)
        {
            return _context.Set<Post>().Any(p => p.Id == postId && p.IsDeleted);
        }
    }
}
