using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IGroupDomainService
    {
        Result CanJoinGroup(Guid userId, bool isGroupPrivate, bool isUserBanned);
        Result CanPostInGroup(Guid userId, Guid groupId, bool isMember, bool isGroupLocked);
        Result CanPromoteMember(Guid actionUserId, string actionUserRole, string targetUserRole);
        Result CanKickMember(Guid actionUserId, string actionUserRole, Guid targetUserId, string targetUserRole);
        Result CanInviteToGroup(Guid inviterId, bool isMember, bool groupAllowsMemberInvites);
        Result CanDeleteGroup(Guid userId, Guid ownerId);
    }
}
