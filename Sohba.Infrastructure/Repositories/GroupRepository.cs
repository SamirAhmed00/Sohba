using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class GroupRepository : GenericRepository<Group>, IGroupRepository
    {
        public GroupRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Group>> GetAllAsync()
        {
            return await _context.Groups
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
                .FirstOrDefaultAsync(g => g.Id == id);
        }
        public async Task<bool> IsMemberAsync(Guid userId, Guid groupId)
        {
            return _context.Set<GroupMember>().Any(m =>
                m.GroupId == groupId &&
                m.UserId == userId &&
                !m.IsBanned);
        }

        public string GetUserRoleInGroup(Guid userId, Guid groupId)
        {
            var member = _context.Set<GroupMember>()
                .FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId);

            if (member == null) return "None";
            return member.Role.ToString();
        }

        public async Task<IEnumerable<Group>> GetGroupsByUserIdAsync(Guid userId)
        {
            return await _context.Groups
                .Include(g => g.Admin)
                .Include(g => g.GroupMembers)
                .Where(g => g.GroupMembers.Any(m => m.UserId == userId))
                .ToListAsync();
        }

        public bool IsUserBannedFromGroup(Guid userId, Guid groupId)
        {
            return _context.Set<GroupMember>().Any(m =>
                m.GroupId == groupId &&
                m.UserId == userId &&
                m.IsBanned);
        }

        public void AddMember(GroupMember member)
        {
            _context.Set<GroupMember>().Add(member);
        }

        public void RemoveMember(GroupMember member)
        {
            _context.Set<GroupMember>().Remove(member);
        }
        public async Task<IEnumerable<Group>> SearchGroupsAsync(string query, int limit = 10)
        {
            return await _context.Groups
                .Include(g => g.Admin)
                .Include(g => g.GroupMembers)
                .Where(g => g.Name.Contains(query) ||
                           g.Description.Contains(query))
                .Take(limit)
                .ToListAsync();
        }


    }
}
