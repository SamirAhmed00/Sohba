using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class PageRepository : GenericRepository<Page>, IPageRepository
    {
        public PageRepository(AppDbContext context) : base(context)
        {
        }

        public void AddFollower(PageFollower follower)
        {
            _context.Set<PageFollower>().Add(follower);
        }

        public void RemoveFollower(Guid userId, Guid pageId)
        {
            var follower = _context.Set<PageFollower>()
                .FirstOrDefault(f => f.UserId == userId && f.PageId == pageId);

            if (follower != null)
            {
                _context.Set<PageFollower>().Remove(follower);
            }
        }

        public async Task<IEnumerable<Page>> GetPagesByFollowerIdAsync(Guid userId)
        {
            var followers = await _context.Set<PageFollower>()
               .Where(f => f.UserId == userId)
               .Include(f => f.Page)        
               .ThenInclude(p => p.Admin)   
               .ToListAsync();

            return followers.Select(f => f.Page);
        }
        public override async Task<IEnumerable<Page>> GetAllAsync()
        {
            return await _context.Set<Page>()
                .Include(p => p.Admin)
                .ToListAsync();
        }

        public override async Task<Page> GetByIdAsync(Guid id)
        {
            return await _context.Set<Page>()
               .Include(p => p.Admin)
               .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Page>> SearchPagesAsync(string query, int limit = 10)
        {
            return await _context.Pages
                .Include(p => p.Admin)
                .Where(p => p.Name.Contains(query) ||
                           p.Description.Contains(query))
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> IsFollowingAsync(Guid userId, Guid pageId)
        {
            return await _context.Set<PageFollower>()
                .AnyAsync(f => f.UserId == userId && f.PageId == pageId);
        }

        public async Task<int> GetFollowersCountAsync(Guid pageId)
        {
            return await _context.Set<PageFollower>()
                .CountAsync(f => f.PageId == pageId);
        }

        public async Task<IEnumerable<PageFollower>> GetFollowersAsync(Guid pageId, int page = 1, int pageSize = 20)
        {
            return await _context.Set<PageFollower>()
                .Include(f => f.User)
                .Where(f => f.PageId == pageId)
                .OrderBy(f => f.FollowedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

    }

}
