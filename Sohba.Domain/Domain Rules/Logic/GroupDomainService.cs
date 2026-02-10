using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class GroupDomainService : IGroupDomainService
    {
        public Result CanDeleteGroup(Guid userId, Guid ownerId)
        {
            // Only the owner of the group can delete it
            if (userId != ownerId)
                return Result.Failure("Only the group owner can delete the group.");

            return Result.Success();
        }
        public Result CanInviteToGroup(Guid inviterId, bool isMember, bool groupAllowsMemberInvites)
        {
            // Must be a member to invite others
            if (!isMember)
                return Result.Failure("You must be a member of the group to invite others.");

            // Check if group settings allow general members to invite
            if (!groupAllowsMemberInvites)
                return Result.Failure("This group does not allow members to send invitations.");

            return Result.Success();
        }

        public Result CanJoinGroup(Guid userId, bool isGroupPrivate, bool isUserBanned)
        {
            // Banned users cannot join regardless of privacy
            if (isUserBanned)
                return Result.Failure("You are banned from this group.");

            // Private groups require an invitation (handled by a different flow), so direct join is rejected
            if (isGroupPrivate)
                return Result.Failure("This is a private group. You need an invitation to join.");

            return Result.Success();
        }

        public Result CanKickMember(Guid actionUserId, string actionUserRole, Guid targetUserId, string targetUserRole)
        {
            // Cannot kick yourself
            if (actionUserId == targetUserId)
                return Result.Failure("You cannot kick yourself.");

            // Standard hierarchy logic: Admin/Owner can kick Member
            // Assuming roles are strings: "Owner", "Admin", "Member"

            bool isActionerAdminOrOwner = actionUserRole == "Admin" || actionUserRole == "Owner";
            bool isTargetAdminOrOwner = targetUserRole == "Admin" || targetUserRole == "Owner";

            if (!isActionerAdminOrOwner)
                return Result.Failure("You do not have permission to kick members.");

            // Admins cannot kick other Admins or the Owner
            if (isTargetAdminOrOwner)
                return Result.Failure("You cannot kick an Admin or the Owner.");

            return Result.Success();
        }

        public Result CanPostInGroup(Guid userId, Guid groupId, bool isMember, bool isGroupLocked)
        {
            // Must be a member to post
            if (!isMember)
                return Result.Failure("You must join the group to post.");

            // Cannot post if the group is archived/locked
            if (isGroupLocked)
                return Result.Failure("This group is locked/archived. No new posts allowed.");

            return Result.Success();
        }

        public Result CanPromoteMember(Guid actionUserId, string actionUserRole, string targetUserRole)
        {
            // Only Owner or Admin can promote
            if (actionUserRole != "Owner" && actionUserRole != "Admin")
                return Result.Failure("You do not have permission to promote members.");

            // Cannot promote someone who is already an Admin or Owner
            if (targetUserRole == "Admin" || targetUserRole == "Owner")
                return Result.Failure("User is already an Admin or Owner.");

            return Result.Success();
        }
    }
}
