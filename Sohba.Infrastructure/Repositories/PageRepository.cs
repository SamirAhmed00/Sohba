using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public string GetUserRoleInPage(Guid userId, Guid pageId)
        {
            var follower = _context.Set<PageFollower>()
                .AsNoTracking()
                .FirstOrDefault(f => f.PageId == pageId && f.UserId == userId);
            return follower == null ? "None" : follower.Role.ToString();
        }

        public async Task<PageRole?> GetUserRoleInPageAsync(Guid userId, Guid pageId)
        {
            var role = await _context.Set<PageFollower>()
                .AsNoTracking()
                .Where(f => f.PageId == pageId && f.UserId == userId)
                .Select(f => (PageRole?)f.Role)
                .FirstOrDefaultAsync();
            return role;
        }

        public async Task<int> GetAdminCountAsync(Guid pageId)
        {
            // Counts both Admin (3) and PageOwner (4) — i.e., users with role >= Admin.
            return await _context.Set<PageFollower>()
                .CountAsync(f => f.PageId == pageId && f.Role >= PageRole.Admin);
        }

        public async Task<int> GetRoleCountAsync(Guid pageId, PageRole role)
        {
            return await _context.Set<PageFollower>()
                .CountAsync(f => f.PageId == pageId && f.Role == role);
        }

        public async Task<PageFollower?> GetFollowerAsync(Guid userId, Guid pageId)
        {
            return await _context.Set<PageFollower>()
                .FirstOrDefaultAsync(f => f.UserId == userId && f.PageId == pageId);
        }

        public async Task<PageFollower?> GetEarliestAdminAsync(Guid pageId)
        {
            return await _context.Set<PageFollower>()
                .Where(f => f.PageId == pageId && f.Role == PageRole.Admin)
                .OrderBy(f => f.FollowedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var trimmed = name.Trim();
            return await _context.Set<Page>()
                .AnyAsync(p => p.Name == trimmed);
        }

        public async Task<IEnumerable<Page>> GetPagesToDiscoverAsync(Guid userId, int count = 5)
        {
            var followedPageIds = _context.Set<PageFollower>()
                .Where(f => f.UserId == userId)
                .Select(f => f.PageId);

            return await _context.Pages
                .AsNoTracking()
                .Include(p => p.Admin)
                .Where(p => !followedPageIds.Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public void AddFollowRequest(PageFollowRequest request)
        {
            _context.Set<PageFollowRequest>().Add(request);
        }

        public async Task<PageFollowRequest?> GetFollowRequestByIdAsync(Guid requestId)
        {
            return await _context.Set<PageFollowRequest>()
                .Include(r => r.Page)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == requestId);
        }

        public async Task<PageFollowRequest?> GetPendingFollowRequestAsync(Guid pageId, Guid userId)
        {
            return await _context.Set<PageFollowRequest>()
                .FirstOrDefaultAsync(r => r.PageId == pageId && r.UserId == userId && r.Status == PageFollowRequestStatus.Pending);
        }

        public async Task<IEnumerable<PageFollowRequest>> GetPendingFollowRequestsAsync(Guid pageId)
        {
            return await _context.Set<PageFollowRequest>()
                .Include(r => r.User)
                .Include(r => r.Page)
                .Where(r => r.PageId == pageId && r.Status == PageFollowRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PageFollowRequest>> GetPendingFollowRequestsForUserPagesAsync(Guid adminUserId)
        {
            var adminPageIds = await _context.Set<PageFollower>()
                .Where(pf => pf.UserId == adminUserId && pf.Role >= PageRole.Admin)
                .Select(pf => pf.PageId)
                .ToListAsync();

            return await _context.Set<PageFollowRequest>()
                .Include(r => r.User)
                .Include(r => r.Page)
                .Where(r => adminPageIds.Contains(r.PageId) && r.Status == PageFollowRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasPendingRequestAsync(Guid pageId, Guid userId)
        {
            return await _context.Set<PageFollowRequest>()
                .AnyAsync(r => r.PageId == pageId && r.UserId == userId && r.Status == PageFollowRequestStatus.Pending);
        }
    }
}
