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

        public async Task<int> GetReportCountAsync(Guid entityId)
        {
            // Count how many times a specific entity (e.g., Post) has been reported
            return await _context.Set<PostReport>()
                .CountAsync(r => r.PostId == entityId);
        }
    }
}
