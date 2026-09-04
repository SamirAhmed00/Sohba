using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sohba.Infrastructure.Repositories
{
    public class GroupRepository : GenericRepository<Group>, IGroupRepository
    {
        public GroupRepository(AppDbContext context)
            : base(context)
        {
        }

        // ============================================================
        // Groups
        // ============================================================

        public override async Task<IEnumerable<Group>> GetAllAsync()
        {
            // AsNoTracking is required here:
            // shared/read-heavy calls should not create tracked Group
            // instances that may later conflict with tracked entities.
            return await _context.Groups
                .AsNoTrackingWithIdentityResolution()
                .Include(g => g.Admin)
                .Include(g => g.GroupMembers)
                .ToListAsync();
        }

        public override async Task<Group> GetByIdAsync(Guid id)
        {
            return await _context.Groups
                .Include(g => g.Admin)
                .Include(g => g.GroupMembers)
                .ThenInclude(m => m.User)
                .AsNoTrackingWithIdentityResolution()
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<IEnumerable<Group>> GetGroupsByUserIdAsync(
            Guid userId)
        {
            return await _context.Groups
                .AsNoTracking()
                .Include(g => g.Admin)
                .Include(g => g.GroupMembers)
                .Where(g =>
                    g.GroupMembers.Any(m =>
                        m.UserId == userId &&
                        !m.IsBanned))
                .ToListAsync();
        }

        public async Task<IEnumerable<Group>> SearchGroupsAsync(
            string query,
            int limit = 10)
        {
            return await _context.Groups
                .AsNoTracking()
                .Include(g => g.Admin)
                .Include(g => g.GroupMembers)
                .Where(g =>
                    g.Name.Contains(query) ||
                    g.Description.Contains(query))
                .Take(limit)
                .ToListAsync();
        }

        public async Task<(IReadOnlyList<Group> Items, int TotalCount)>
            GetGroupsPagedAsync(
                string? search,
                int page,
                int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _context.Groups
                .AsNoTrackingWithIdentityResolution()
                .Include(g => g.Admin)
                .Include(g => g.GroupMembers)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();

                query = query.Where(g =>
                    g.Name.Contains(q) ||
                    g.Description.Contains(q));
            }

            var totalCount =
                await query.CountAsync();

            var items =
                await query
                    .OrderByDescending(g => g.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Group>> GetRecommendedGroupsAsync(
            Guid userId,
            int count = 5)
        {
            var userGroups =
                await _context.GroupMembers
                    .Where(gm =>
                        gm.UserId == userId &&
                        !gm.IsBanned)
                    .Select(gm => gm.GroupId)
                    .ToListAsync();

            return await _context.Groups
                .AsNoTracking()
                .Include(g => g.Admin)
                .Include(g => g.GroupMembers)
                .Where(g =>
                    !userGroups.Contains(g.Id))
                .OrderByDescending(g =>
                    g.GroupMembers.Count)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Group?> GetTrackedGroupByIdAsync(
            Guid id)
        {
            return await _context.Groups
                .FirstOrDefaultAsync(g =>
                    g.Id == id);
        }

        // ============================================================
        // Membership
        // ============================================================

        public async Task<bool> IsMemberAsync(
            Guid userId,
            Guid groupId)
        {
            if (userId == Guid.Empty ||
                groupId == Guid.Empty)
            {
                return false;
            }

            return await _context.Set<GroupMember>()
                .AsNoTracking()
                .AnyAsync(m =>
                    m.GroupId == groupId &&
                    m.UserId == userId &&
                    !m.IsBanned);
        }

        public bool IsUserBannedFromGroup(
            Guid userId,
            Guid groupId)
        {
            if (userId == Guid.Empty ||
                groupId == Guid.Empty)
            {
                return false;
            }

            return _context.Set<GroupMember>()
                .Any(m =>
                    m.GroupId == groupId &&
                    m.UserId == userId &&
                    m.IsBanned);
        }

        public void AddMember(
            GroupMember member)
        {
            _context.Set<GroupMember>()
                .Add(member);
        }

        public async Task<GroupMember?>
            GetMemberByUserAndGroupAsync(
                Guid groupId,
                Guid userId)
        {
            // Intentionally tracked:
            // service may modify/remove this entity before CompleteAsync().
            return await _context.Set<GroupMember>()
                .FirstOrDefaultAsync(m =>
                    m.GroupId == groupId &&
                    m.UserId == userId);
        }

        public void RemoveMember(
            GroupMember member)
        {
            if (member == null)
            {
                return;
            }

            // Prevent duplicate navigation entities from being attached
            // when this member is removed from a shared DbContext.
            member.User = null;
            member.Group = null;

            _context.Set<GroupMember>()
                .Remove(member);
        }

        public GroupRole? GetUserRoleInGroup(
            Guid userId,
            Guid groupId)
        {
            if (userId == Guid.Empty ||
                groupId == Guid.Empty)
            {
                return null;
            }

            var member =
                _context.Set<GroupMember>()
                    .AsNoTracking()
                    .FirstOrDefault(m =>
                        m.GroupId == groupId &&
                        m.UserId == userId &&
                        !m.IsBanned);

            return member?.Role;
        }

        public async Task<IEnumerable<GroupMember>>
            GetGroupMembersAsync(
                Guid groupId)
        {
            return await _context.Set<GroupMember>()
                .Include(gm => gm.User)
                .Where(gm =>
                    gm.GroupId == groupId &&
                    !gm.IsBanned &&
                    !gm.User.IsDeleted)
                .OrderByDescending(gm => gm.Role)
                .ThenBy(gm => gm.JoinedAt)
                .ToListAsync();
        }

        public async Task<
            (IReadOnlyList<GroupMember> Items, int TotalCount)>
            GetMembersPagedAsync(
                Guid groupId,
                string? search,
                int page,
                int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query =
                _context.Set<GroupMember>()
                    .Include(gm => gm.User)
                    .Where(gm =>
                        gm.GroupId == groupId &&
                        !gm.IsBanned &&
                        !gm.User.IsDeleted)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();

                query = query.Where(gm =>
                    gm.User.Name.Contains(q));
            }

            var totalCount =
                await query.CountAsync();

            var items =
                await query
                    .OrderByDescending(gm => gm.Role)
                    .ThenBy(gm => gm.JoinedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

            return (items, totalCount);
        }

        // ============================================================
        // Ownership Transfer
        // ============================================================

        public async Task<GroupMember?>
            GetEarliestEligibleMemberForOwnershipTransferAsync(
                Guid groupId,
                Guid excludeUserId)
        {
            // Priority:
            // 1. Admin
            // 2. CoAdmin
            // 3. Member
            //
            // Within each role tier:
            // earliest JoinedAt first.
            //
            // Exclude:
            // - current owner
            // - banned members
            // - deleted users
            return await _context.Set<GroupMember>()
                .Include(gm => gm.User)
                .Where(gm =>
                    gm.GroupId == groupId &&
                    gm.UserId != excludeUserId &&
                    !gm.IsBanned &&
                    !gm.User.IsDeleted)
                .OrderByDescending(gm => gm.Role)
                .ThenBy(gm => gm.JoinedAt)
                .FirstOrDefaultAsync();
        }

        // ============================================================
        // Deleted Groups
        // ============================================================

        public async Task<IEnumerable<Group>>
            GetDeletedGroupsAsync()
        {
            return await _context.Groups
                .IgnoreQueryFilters()
                .Where(g => g.IsDeleted)
                .Include(g => g.Admin)
                .OrderByDescending(g => g.DeletedAt)
                .ToListAsync();
        }

        // ============================================================
        // Group Admins
        // ============================================================

        public async Task<IEnumerable<GroupMember>>
            GetGroupAdminsAsync(
                Guid groupId)
        {
            return await _context.Set<GroupMember>()
                .Include(gm => gm.User)
                .Where(gm =>
                    gm.GroupId == groupId &&
                    gm.Role == GroupRole.Admin &&
                    !gm.IsBanned &&
                    !gm.User.IsDeleted)
                .ToListAsync();
        }

        // ============================================================
        // Join Requests
        // ============================================================

        public void AddJoinRequest(
            GroupJoinRequest request)
        {
            _context.GroupJoinRequests
                .Add(request);
        }

        public async Task<GroupJoinRequest?>
            GetJoinRequestByIdAsync(
                Guid requestId)
        {
            return await _context.GroupJoinRequests
                .Include(r => r.Group)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.Id == requestId);
        }

        public async Task<GroupJoinRequest?>
            GetPendingJoinRequestAsync(
                Guid groupId,
                Guid userId)
        {
            return await _context.GroupJoinRequests
                .FirstOrDefaultAsync(r =>
                    r.GroupId == groupId &&
                    r.UserId == userId &&
                    r.Status ==
                        GroupJoinRequestStatus.Pending);
        }

        public async Task<
            (IEnumerable<GroupJoinRequest> Items, int TotalCount)>
            GetPendingJoinRequestsPagedAsync(
                Guid groupId,
                int page,
                int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query =
                _context.GroupJoinRequests
                    .Include(r => r.User)
                    .Where(r =>
                        r.GroupId == groupId &&
                        r.Status ==
                            GroupJoinRequestStatus.Pending)
                    .OrderByDescending(r =>
                        r.CreatedAt);

            var totalCount =
                await query.CountAsync();

            var items =
                await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

            return (items, totalCount);
        }

        public async Task<int>
            GetPendingJoinRequestsCountAsync(
                Guid groupId)
        {
            return await _context.GroupJoinRequests
                .CountAsync(r =>
                    r.GroupId == groupId &&
                    r.Status ==
                        GroupJoinRequestStatus.Pending);
        }
    }
}
