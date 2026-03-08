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
            //return await _context.Users
            //    .IgnoreQueryFilters() 
            //    .FirstOrDefaultAsync(u => u.Id == id);
            // 1. جرب من غير Filters
            Console.WriteLine($"🔍 Getting user by ID: {id}");

            // Count total users
            var totalUsers = await _context.Users.CountAsync();
            Console.WriteLine($"📊 Total users in DB: {totalUsers}");

            // Try with no filters
            var user = await _context.Users
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user != null)
            {
                Console.WriteLine($"✅ User found! Name: {user.Name}, IsDeleted: {user.IsDeleted}");
                return user;
            }

            Console.WriteLine("❌ User not found with LINQ, trying SQL raw...");

            // Try SQL raw as last resort
            user = await _context.Users
                .FromSqlRaw("SELECT * FROM AspNetUsers WHERE Id = {0}", id)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                Console.WriteLine($"✅ User found with SQL! Name: {user.Name}");
            }
            else
            {
                Console.WriteLine("❌ User not found even with SQL raw");
            }

            return user;
        }
        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Set<User>().FirstOrDefaultAsync(u => u.Name == username);
        }

        public bool EmailExists(string email)
        {
            return _context.Set<User>().Any(u => u.Email == email);
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
            return await _context.Set<User>()
                .Where(u => u.Id != currentUserId &&
                           !u.IsDeleted &&
                           u.Name.Contains(query))
                .Take(limit)
                .ToListAsync();
        }
    }
}
