using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class InteractionRepository : IInteractionRepository
    {
        private readonly AppDbContext _context;

        public InteractionRepository(AppDbContext context)
        {
            _context = context;
        }

        public bool HasUserReacted(Guid userId, Guid entityId)
        {
            // Implementation depends on how reactions are stored in your DB
            // Assuming a Reaction entity exists
            return _context.Set<Reaction>().Any(r => r.UserId == userId && r.PostId == entityId);
        }

        public async Task<int> GetReactionCountAsync(Guid entityId)
        {
            return await _context.Set<Reaction>().CountAsync(r => r.PostId == entityId);
        }

        public async Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(Guid postId)
        {
            return await _context.Set<Comment>()
                .Where(c => c.PostId == postId)
                .ToListAsync();
        }
    }
}
