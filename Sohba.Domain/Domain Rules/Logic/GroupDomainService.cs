using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Enums;
using System;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class GroupDomainService : IGroupDomainService
    {
        // ============================================================
        // Group Deletion
        // ============================================================

        public Result CanDeleteGroup(
            Guid userId,
            Guid ownerId,
            bool isAdmin = false)
        {
            if (!isAdmin && userId != ownerId)
            {
                return Result.Failure(
                    "Only the group owner or a system administrator can delete the group.");
            }

            return Result.Success();
        }

        // ============================================================
        // Group Update
        // ============================================================

        public Result CanUpdateGroup(
            Guid userId,
            Guid groupId,
            Guid groupAdminId)
        {
            if (userId != groupAdminId)
            {
                return Result.Failure(
                    "Only the group owner can update group details.");
            }

            return Result.Success();
        }

        // ============================================================
        // Group Invitations
        // ============================================================

        public Result CanInviteToGroup(
            Guid inviterId,
            bool isMember,
            bool groupAllowsMemberInvites)
        {
            if (!isMember)
            {
                return Result.Failure(
                    "You must be a member of the group to invite others.");
            }

            if (!groupAllowsMemberInvites)
            {
                return Result.Failure(
                    "This group does not allow members to send invitations.");
            }

            return Result.Success();
        }

        // ============================================================
        // Group Joining
        // ============================================================

        public Result CanJoinGroup(
            Guid userId,
            bool isGroupPrivate,
            bool isUserBanned)
        {
            if (isUserBanned)
            {
                return Result.Failure(
                    "You are banned from this group.");
            }

            if (isGroupPrivate)
            {
                return Result.Failure(
                    "This is a private group. You need an invitation to join.");
            }

            return Result.Success();
        }

        public Result CanJoinGroupDirectly(
            Guid userId,
            bool isGroupPrivate,
            bool isUserBanned)
        {
            if (isUserBanned)
            {
                return Result.Failure(
                    "You are banned from this group.");
            }

            if (isGroupPrivate)
            {
                return Result.Failure(
                    "This is a private group. You must submit a join request.");
            }

            return Result.Success();
        }

        public Result CanSubmitJoinRequest(
            Guid userId,
            bool isGroupPrivate,
            bool isMember,
            bool isUserBanned,
            bool hasExistingPendingRequest)
        {
            if (isMember)
            {
                return Result.Failure(
                    "You are already a member of this group.");
            }

            if (isUserBanned)
            {
                return Result.Failure(
                    "You are banned from this group.");
            }

            if (!isGroupPrivate)
            {
                return Result.Failure(
                    "Join requests are only available for private groups.");
            }

            if (hasExistingPendingRequest)
            {
                return Result.Failure(
                    "You already have a pending join request for this group.");
            }

            return Result.Success();
        }

        // ============================================================
        // Group Posts
        // ============================================================

        public Result CanPostInGroup(
            Guid userId,
            Guid groupId,
            bool isMember,
            bool isUserBanned,
            bool isGroupLocked)
        {
            if (!isMember)
            {
                return Result.Failure(
                    "You must be an active member of this group to create posts.");
            }

            if (isUserBanned)
            {
                return Result.Failure(
                    "You are banned from posting in this group.");
            }

            if (isGroupLocked)
            {
                return Result.Failure(
                    "This group is archived or locked. New posts are not allowed.");
            }

            return Result.Success();
        }

        // ============================================================
        // Promote Member
        // ============================================================

        public Result CanPromoteMember(
            Guid actionUserId,
            GroupRole? actionUserRole,
            Guid targetUserId,
            GroupRole? targetUserRole,
            Guid groupOwnerId)
        {
            if (!actionUserRole.HasValue)
            {
                return Result.Failure(
                    "You are not a member of this group.");
            }

            if (!targetUserRole.HasValue)
            {
                return Result.Failure(
                    "Target user is not a member of this group.");
            }

            if (actionUserId == targetUserId)
            {
                return Result.Failure(
                    "You cannot promote yourself.");
            }

            if (targetUserId == groupOwnerId)
            {
                return Result.Failure(
                    "The group owner cannot be promoted.");
            }

            var isOwner =
                actionUserId == groupOwnerId;

            var isAdmin =
                isOwner ||
                actionUserRole.Value == GroupRole.Admin;

            if (!isAdmin)
            {
                return Result.Failure(
                    "You do not have permission to promote members.");
            }

            if (targetUserRole.Value == GroupRole.Member)
            {
                // Member -> CoAdmin
                // Owner and Admin are allowed.
                return Result.Success();
            }

            if (targetUserRole.Value == GroupRole.CoAdmin)
            {
                // CoAdmin -> Admin
                // Owner only.
                if (!isOwner)
                {
                    return Result.Failure(
                        "Only the group owner can promote a co-administrator to full administrator.");
                }

                return Result.Success();
            }

            if (targetUserRole.Value == GroupRole.Admin)
            {
                return Result.Failure(
                    "User is already a full administrator.");
            }

            return Result.Failure(
                "Invalid promotion request.");
        }

        // ============================================================
        // Demote Member
        // ============================================================

        public Result CanDemoteMember(
            Guid actionUserId,
            GroupRole? actionUserRole,
            Guid targetUserId,
            GroupRole? targetUserRole,
            Guid groupOwnerId)
        {
            if (!actionUserRole.HasValue)
            {
                return Result.Failure(
                    "You are not a member of this group.");
            }

            if (!targetUserRole.HasValue)
            {
                return Result.Failure(
                    "Target user is not a member of this group.");
            }

            if (actionUserId == targetUserId)
            {
                return Result.Failure(
                    "You cannot demote yourself.");
            }

            if (targetUserId == groupOwnerId)
            {
                return Result.Failure(
                    "The group owner cannot be demoted.");
            }

            var isOwner =
                actionUserId == groupOwnerId;

            var isAdmin =
                isOwner ||
                actionUserRole.Value == GroupRole.Admin;

            if (!isAdmin)
            {
                return Result.Failure(
                    "You do not have permission to demote group leaders.");
            }

            if (targetUserRole.Value == GroupRole.Admin)
            {
                // Admin -> CoAdmin
                // Owner only.
                if (!isOwner)
                {
                    return Result.Failure(
                        "Only the group owner can demote administrators.");
                }

                return Result.Success();
            }

            if (targetUserRole.Value == GroupRole.CoAdmin)
            {
                // CoAdmin -> Member
                // Owner and Admin.
                return Result.Success();
            }

            if (targetUserRole.Value == GroupRole.Member)
            {
                return Result.Failure(
                    "Regular members cannot be demoted further.");
            }

            return Result.Failure(
                "Invalid demotion request.");
        }

        // ============================================================
        // Kick Member
        // ============================================================

        public Result CanKickMember(
            Guid actionUserId,
            GroupRole? actionUserRole,
            Guid targetUserId,
            GroupRole? targetUserRole,
            Guid groupOwnerId)
        {
            if (!actionUserRole.HasValue)
            {
                return Result.Failure(
                    "You are not a member of this group.");
            }

            if (!targetUserRole.HasValue)
            {
                return Result.Failure(
                    "Target user is not a member of this group.");
            }

            if (actionUserId == targetUserId)
            {
                return Result.Failure(
                    "You cannot kick yourself. Use leave group instead.");
            }

            if (targetUserId == groupOwnerId)
            {
                return Result.Failure(
                    "The group owner cannot be removed from the group.");
            }

            var isOwner =
                actionUserId == groupOwnerId;

            var isAdmin =
                isOwner ||
                actionUserRole.Value == GroupRole.Admin;

            var isCoAdmin =
                isAdmin ||
                actionUserRole.Value == GroupRole.CoAdmin;

            if (targetUserRole.Value == GroupRole.Admin)
            {
                // Only Owner can kick Admin.
                if (!isOwner)
                {
                    return Result.Failure(
                        "Only the group owner can remove administrators.");
                }
            }
            else if (targetUserRole.Value == GroupRole.CoAdmin)
            {
                // Owner/Admin can kick CoAdmin.
                if (!isAdmin)
                {
                    return Result.Failure(
                        "Only the group owner or administrators can remove co-administrators.");
                }
            }
            else if (targetUserRole.Value == GroupRole.Member)
            {
                // Owner/Admin/CoAdmin can kick Member.
                if (!isCoAdmin)
                {
                    return Result.Failure(
                        "You do not have permission to remove members from this group.");
                }
            }
            else
            {
                return Result.Failure(
                    "Invalid target membership role.");
            }

            return Result.Success();
        }

        // ============================================================
        // Leave Group
        // ============================================================

        public Result CanLeaveGroup(
            Guid userId,
            Guid groupId,
            bool isOwner,
            int eligibleReplacementsCount)
        {
            // GroupService supplies the current number of active members.
            // Therefore an owner can leave only when at least one other
            // active member exists who can receive ownership.
            if (isOwner && eligibleReplacementsCount <= 1)
            {
                return Result.Failure(
                    "You are the only member in this group. Please delete the group if you wish to close it.");
            }

            return Result.Success();
        }

        // ============================================================
        // Review Join Requests
        // ============================================================

        public Result CanReviewJoinRequest(
            Guid actionUserId,
            GroupRole? actionUserRole,
            bool isOwner)
        {
            if (!isOwner && !actionUserRole.HasValue)
            {
                return Result.Failure(
                    "You are not a member of this group.");
            }

            // Owner is always allowed.
            if (isOwner)
            {
                return Result.Success();
            }

            // Only GroupRole.Admin can review requests.
            // CoAdmin and Member are not allowed.
            if (actionUserRole.Value != GroupRole.Admin)
            {
                return Result.Failure(
                    "Only the group owner or group administrators can manage join requests.");
            }

            return Result.Success();
        }
    }
}
