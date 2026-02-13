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

        public bool IsMember(Guid userId, Guid groupId)
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
    }
}
