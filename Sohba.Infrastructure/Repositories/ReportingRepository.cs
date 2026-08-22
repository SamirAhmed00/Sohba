using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class ReportingRepository : GenericRepository<PostReport>, IReportingRepository
    {
        public ReportingRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<PostReport>> GetAllAsync() 
        { 
            return await _context.Set<PostReport>() 
                .Include(r => r.User) 
                .ToListAsync(); 
        } 
        public async Task<bool> HasUserReportedEntityAsync(Guid userId, Guid entityId)
        {
            return await _context.Set<PostReport>()
                .AnyAsync(r => r.UserId == userId && r.PostId == entityId);
        }
        public async Task<int> GetReportCountForEntityAsync(Guid entityId)
        {
            return await _context.Set<PostReport>()
                .CountAsync(r => r.PostId == entityId);
        }

        public async Task<int> CountPendingAsync()
        {
            return await _context.Set<PostReport>()
                .CountAsync(r => !r.IsResolved);
        }

        public async Task<IEnumerable<PostReport>> GetRecentPendingAsync(int count)
        {
            return await _context.Set<PostReport>()
                .Include(r => r.User)
                .Where(r => !r.IsResolved)
                .OrderByDescending(r => r.ReportedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
