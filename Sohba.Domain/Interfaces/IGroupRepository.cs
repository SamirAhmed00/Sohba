using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IGroupRepository : IGenericRepository<Group>
    {
        Task<bool> IsMemberAsync(Guid userId, Guid groupId);
        string GetUserRoleInGroup(Guid userId, Guid groupId);
        bool IsUserBannedFromGroup(Guid userId, Guid groupId);

        void AddMember(GroupMember member);
        void RemoveMember(GroupMember member);
    }
}
