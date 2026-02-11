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
            return await _context.Set<PageFollower>()
                .Where(f => f.UserId == userId)
                .Select(f => f.Page)
                .ToListAsync();
        }
    }

}
