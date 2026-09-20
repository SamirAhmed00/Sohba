using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.AdminAggregate;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;

namespace Sohba.Infrastructure.Repositories
{
    /// <summary>
    /// Read/write access to admin audit log entries for the dashboard.
    /// </summary>
    public class AdminAuditLogRepository : GenericRepository<AdminAuditLog>, IGenericAdminLogRepository
    {
        public AdminAuditLogRepository(AppDbContext context) : base(context) { }

        public async Task<(IReadOnlyList<AdminAuditLog> Items, int TotalCount)> GetLogsPagedAsync(string? actionFilter, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.AdminAuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(actionFilter) && !actionFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.Action == actionFilter);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}