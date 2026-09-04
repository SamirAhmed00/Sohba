using Sohba.Domain.Common;
using Sohba.Domain.Enums;
using System;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IGroupDomainService
    {
        // ==================== Joining ====================

        Result CanJoinGroup(
            Guid userId,
            bool isGroupPrivate,
            bool isUserBanned);

        Result CanJoinGroupDirectly(
            Guid userId,
            bool isGroupPrivate,
            bool isUserBanned);

        Result CanSubmitJoinRequest(
            Guid userId,
            bool isGroupPrivate,
            bool isMember,
            bool isUserBanned,
            bool hasExistingPendingRequest);

        // ==================== Group Content ====================

        Result CanPostInGroup(
            Guid userId,
            Guid groupId,
            bool isMember,
            bool isUserBanned,
            bool isGroupLocked);

        // ==================== Member Management ====================

        Result CanPromoteMember(
            Guid actionUserId,
            GroupRole? actionUserRole,
            Guid targetUserId,
            GroupRole? targetUserRole,
            Guid groupOwnerId);

        Result CanDemoteMember(
            Guid actionUserId,
            GroupRole? actionUserRole,
            Guid targetUserId,
            GroupRole? targetUserRole,
            Guid groupOwnerId);

        Result CanKickMember(
            Guid actionUserId,
            GroupRole? actionUserRole,
            Guid targetUserId,
            GroupRole? targetUserRole,
            Guid groupOwnerId);

        // ==================== Invitations ====================

        Result CanInviteToGroup(
            Guid inviterId,
            bool isMember,
            bool groupAllowsMemberInvites);

        // ==================== Group Management ====================

        Result CanDeleteGroup(
            Guid userId,
            Guid ownerId,
            bool isAdmin = false);

        Result CanUpdateGroup(
            Guid userId,
            Guid groupId,
            Guid groupAdminId);

        Result CanLeaveGroup(
            Guid userId,
            Guid groupId,
            bool isOwner,
            int eligibleReplacementsCount);

        // ==================== Join Request Management ====================

        Result CanReviewJoinRequest(
            Guid actionUserId,
            GroupRole? actionUserRole,
            bool isOwner);
    }
}

