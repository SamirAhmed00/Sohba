using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public override async Task<User> GetByIdAsync(Guid id)
        {

            // Count total users
            //var totalUsers = await _context.Users.CountAsync();         

            // Try with no filters
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);
                //.AsNoTracking()

            return user;
        }
        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Set<User>().FirstOrDefaultAsync(u => u.Name == username);
        }

        public async Task<IEnumerable<User>> GetRandomUsersAsync(List<Guid> excludeUserIds, int count)
        {
            return await _context.Set<User>()
                .Where(u => !excludeUserIds.Contains(u.Id))
                .OrderBy(u => Guid.NewGuid())
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string query, Guid currentUserId, int limit = 10)
        {
            var blockedUserIds = await _context.Friends
                .Where(f => (f.UserId == currentUserId || f.FriendUserId == currentUserId)
                            && f.Status == FriendshipStatus.Blocked)
                .Select(f => f.UserId == currentUserId ? f.FriendUserId : f.UserId)
                .ToListAsync();

            return await _context.Set<User>()
                .Where(u => u.Id != currentUserId &&
                           !u.IsDeleted &&
                           u.IsActive &&
                           !u.IsBlocked &&
                           !blockedUserIds.Contains(u.Id) &&
                           u.Name.Contains(query))
                .OrderBy(u => u.Name)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetRecentAsync(int count)
        {
            return await _context.Set<User>()
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<User?> GetByEmailIncludingDeletedAsync(string email)
        {
            return await _context.Set<User>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetUsersAdminPagedAsync(
            string? search,
            string? status,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _context.Set<User>()
                .IgnoreQueryFilters()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u => u.Name.Contains(term) || u.Email.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                switch (status.ToLowerInvariant())
                {
                    case "active":
                        query = query.Where(u => !u.IsDeleted && !u.IsBlocked && u.IsActive);
                        break;
                    case "blocked":
                        query = query.Where(u => !u.IsDeleted && u.IsBlocked);
                        break;
                    case "deactivated":
                        query = query.Where(u => !u.IsDeleted && !u.IsActive && !u.IsBlocked);
                        break;
                    case "deleted":
                        query = query.Where(u => u.IsDeleted);
                        break;
                }
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }


        public async Task<Dictionary<DateTime, int>> GetUserRegistrationsByDayAsync(int days)
        {
            var cutoff = DateTime.UtcNow.Date.AddDays(-days + 1);

            return await _context.Set<User>()
                .Where(u => u.CreatedAt >= cutoff)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count);
        }

        public async Task<int> GetNewUsersCountSinceAsync(DateTime sinceUtc)
        {
            return await _context.Set<User>()
                .CountAsync(u => u.CreatedAt >= sinceUtc);
        }

    }
}
