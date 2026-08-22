using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.UserAggregate;
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
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            return user;
        }
        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Set<User>().FirstOrDefaultAsync(u => u.Name == username);
        }

            //public bool EmailExists(string email)
            //{
            //    return _context.Set<User>().Any(u => u.Email == email);
            //}
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
            return await _context.Set<User>()
                .Where(u => u.Id != currentUserId &&
                           !u.IsDeleted &&
                           u.Name.Contains(query))
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

    }
}
