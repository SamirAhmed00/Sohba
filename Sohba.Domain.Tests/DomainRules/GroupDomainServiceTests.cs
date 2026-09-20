using Sohba.Domain.Domain_Rules.Logic;
using Sohba.Domain.Enums;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for group rules: deletion, update, invitations, joining,
    /// join requests, group posting, role promotion/demotion, kicking,
    /// leaving and join-request review.
    /// </summary>
    public class GroupDomainServiceTests
    {
        private readonly GroupDomainService _sut = new();

        private static readonly Guid Actor = Guid.NewGuid();
        private static readonly Guid Target = Guid.NewGuid();
        private static readonly Guid GroupId = Guid.NewGuid();
        private static readonly Guid OwnerId = Guid.NewGuid();

        // ---- CanDeleteGroup ----

        /// <summary>Verifies that the group owner can delete the group.</summary>
        [Fact]
        public void CanDeleteGroup_WhenCallerIsOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanDeleteGroup(OwnerId, OwnerId, isAdmin: false).IsSuccess);
        }

        /// <summary>Verifies that a system administrator can delete any group.</summary>
        [Fact]
        public void CanDeleteGroup_WhenCallerIsSystemAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanDeleteGroup(Actor, OwnerId, isAdmin: true).IsSuccess);
        }

        /// <summary>Verifies that a regular member cannot delete the group.</summary>
        [Fact]
        public void CanDeleteGroup_WhenCallerIsNotOwnerOrAdmin_ReturnsFailure()
        {
            var result = _sut.CanDeleteGroup(Actor, OwnerId, isAdmin: false);

            Assert.True(result.IsFailure);
            Assert.Contains("owner or a system administrator", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanUpdateGroup ----

        /// <summary>Verifies that the group admin (owner-designated) can update group details.</summary>
        [Fact]
        public void CanUpdateGroup_WhenCallerIsGroupAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanUpdateGroup(Actor, GroupId, Actor).IsSuccess);
        }

        /// <summary>Verifies that a non-admin cannot update group details.</summary>
        [Fact]
        public void CanUpdateGroup_WhenCallerIsNotGroupAdmin_ReturnsFailure()
        {
            var result = _sut.CanUpdateGroup(Actor, GroupId, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("owner can update", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanInviteToGroup ----

        /// <summary>Verifies that a member can invite when the group allows member invites.</summary>
        [Fact]
        public void CanInviteToGroup_WhenMemberAndInvitesAllowed_ReturnsSuccess()
        {
            Assert.True(_sut.CanInviteToGroup(Actor, isMember: true, groupAllowsMemberInvites: true).IsSuccess);
        }

        /// <summary>Verifies that a non-member cannot invite others.</summary>
        [Fact]
        public void CanInviteToGroup_WhenCallerIsNotMember_ReturnsFailure()
        {
            var result = _sut.CanInviteToGroup(Actor, isMember: false, groupAllowsMemberInvites: true);

            Assert.True(result.IsFailure);
            Assert.Contains("must be a member", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that invites are rejected when the group disallows member invites.</summary>
        [Fact]
        public void CanInviteToGroup_WhenInvitesNotAllowed_ReturnsFailure()
        {
            var result = _sut.CanInviteToGroup(Actor, isMember: true, groupAllowsMemberInvites: false);

            Assert.True(result.IsFailure);
            Assert.Contains("does not allow", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanJoinGroup / CanJoinGroupDirectly ----

        /// <summary>Verifies that a user can join a public group they are not banned from.</summary>
        [Fact]
        public void CanJoinGroup_WhenGroupIsPublicAndNotBanned_ReturnsSuccess()
        {
            Assert.True(_sut.CanJoinGroup(Actor, isGroupPrivate: false, isUserBanned: false).IsSuccess);
        }

        /// <summary>Verifies that a banned user cannot join a group.</summary>
        [Fact]
        public void CanJoinGroup_WhenUserIsBanned_ReturnsFailure()
        {
            var result = _sut.CanJoinGroup(Actor, isGroupPrivate: false, isUserBanned: true);

            Assert.True(result.IsFailure);
            Assert.Contains("banned", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a private group cannot be joined without an invitation.</summary>
        [Fact]
        public void CanJoinGroup_WhenGroupIsPrivate_ReturnsFailure()
        {
            var result = _sut.CanJoinGroup(Actor, isGroupPrivate: true, isUserBanned: false);

            Assert.True(result.IsFailure);
            Assert.Contains("invitation", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a user can join a public group directly.</summary>
        [Fact]
        public void CanJoinGroupDirectly_WhenGroupIsPublicAndNotBanned_ReturnsSuccess()
        {
            Assert.True(_sut.CanJoinGroupDirectly(Actor, isGroupPrivate: false, isUserBanned: false).IsSuccess);
        }

        /// <summary>Verifies that a banned user cannot join directly.</summary>
        [Fact]
        public void CanJoinGroupDirectly_WhenUserIsBanned_ReturnsFailure()
        {
            Assert.True(_sut.CanJoinGroupDirectly(Actor, isGroupPrivate: false, isUserBanned: true).IsFailure);
        }

        /// <summary>Verifies that joining a private group directly is rejected and a request is suggested.</summary>
        [Fact]
        public void CanJoinGroupDirectly_WhenGroupIsPrivate_ReturnsFailure()
        {
            var result = _sut.CanJoinGroupDirectly(Actor, isGroupPrivate: true, isUserBanned: false);

            Assert.True(result.IsFailure);
            Assert.Contains("join request", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanSubmitJoinRequest ----

        /// <summary>Verifies that a non-member can request to join a private group.</summary>
        [Fact]
        public void CanSubmitJoinRequest_WhenPrivateGroupAndNotMember_ReturnsSuccess()
        {
            var result = _sut.CanSubmitJoinRequest(Actor, isGroupPrivate: true, isMember: false, isUserBanned: false, hasExistingPendingRequest: false);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that an existing member cannot submit a join request.</summary>
        [Fact]
        public void CanSubmitJoinRequest_WhenAlreadyMember_ReturnsFailure()
        {
            var result = _sut.CanSubmitJoinRequest(Actor, isGroupPrivate: true, isMember: true, isUserBanned: false, hasExistingPendingRequest: false);

            Assert.True(result.IsFailure);
            Assert.Contains("already a member", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a banned user cannot submit a join request.</summary>
        [Fact]
        public void CanSubmitJoinRequest_WhenUserIsBanned_ReturnsFailure()
        {
            var result = _sut.CanSubmitJoinRequest(Actor, isGroupPrivate: true, isMember: false, isUserBanned: true, hasExistingPendingRequest: false);

            Assert.True(result.IsFailure);
            Assert.Contains("banned", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that join requests are only available for private groups.</summary>
        [Fact]
        public void CanSubmitJoinRequest_WhenGroupIsPublic_ReturnsFailure()
        {
            var result = _sut.CanSubmitJoinRequest(Actor, isGroupPrivate: false, isMember: false, isUserBanned: false, hasExistingPendingRequest: false);

            Assert.True(result.IsFailure);
            Assert.Contains("only available for private", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a duplicate pending join request is rejected.</summary>
        [Fact]
        public void CanSubmitJoinRequest_WhenPendingRequestExists_ReturnsFailure()
        {
            var result = _sut.CanSubmitJoinRequest(Actor, isGroupPrivate: true, isMember: false, isUserBanned: false, hasExistingPendingRequest: true);

            Assert.True(result.IsFailure);
            Assert.Contains("pending join request", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanPostInGroup ----

        /// <summary>Verifies that an active member of an unlocked group can post.</summary>
        [Fact]
        public void CanPostInGroup_WhenMemberNotBannedAndUnlocked_ReturnsSuccess()
        {
            Assert.True(_sut.CanPostInGroup(Actor, GroupId, isMember: true, isUserBanned: false, isGroupLocked: false).IsSuccess);
        }

        /// <summary>Verifies that a non-member cannot post in the group.</summary>
        [Fact]
        public void CanPostInGroup_WhenNotMember_ReturnsFailure()
        {
            var result = _sut.CanPostInGroup(Actor, GroupId, isMember: false, isUserBanned: false, isGroupLocked: false);

            Assert.True(result.IsFailure);
            Assert.Contains("active member", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a banned member cannot post.</summary>
        [Fact]
        public void CanPostInGroup_WhenUserIsBanned_ReturnsFailure()
        {
            var result = _sut.CanPostInGroup(Actor, GroupId, isMember: true, isUserBanned: true, isGroupLocked: false);

            Assert.True(result.IsFailure);
            Assert.Contains("banned", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that posting to a locked/archived group is rejected.</summary>
        [Fact]
        public void CanPostInGroup_WhenGroupIsLocked_ReturnsFailure()
        {
            var result = _sut.CanPostInGroup(Actor, GroupId, isMember: true, isUserBanned: false, isGroupLocked: true);

            Assert.True(result.IsFailure);
            Assert.Contains("locked", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanPromoteMember ----

        /// <summary>Verifies that the owner can promote a member to co-admin.</summary>
        [Fact]
        public void CanPromoteMember_WhenOwnerPromotesMember_ReturnsSuccess()
        {
            var result = _sut.CanPromoteMember(OwnerId, GroupRole.Admin, Target, GroupRole.Member, OwnerId);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that an admin can promote a member to co-admin.</summary>
        [Fact]
        public void CanPromoteMember_WhenAdminPromotesMember_ReturnsSuccess()
        {
            var result = _sut.CanPromoteMember(Actor, GroupRole.Admin, Target, GroupRole.Member, OwnerId);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a member cannot promote other members.</summary>
        [Fact]
        public void CanPromoteMember_WhenCallerIsMember_ReturnsFailure()
        {
            var result = _sut.CanPromoteMember(Actor, GroupRole.Member, Target, GroupRole.Member, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("permission to promote", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a caller with no membership cannot promote.</summary>
        [Fact]
        public void CanPromoteMember_WhenCallerIsNotMember_ReturnsFailure()
        {
            var result = _sut.CanPromoteMember(Actor, null, Target, GroupRole.Member, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("not a member", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that promoting a non-member is rejected.</summary>
        [Fact]
        public void CanPromoteMember_WhenTargetIsNotMember_ReturnsFailure()
        {
            var result = _sut.CanPromoteMember(OwnerId, GroupRole.Admin, Target, null, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("Target user is not a member", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a user cannot promote themselves.</summary>
        [Fact]
        public void CanPromoteMember_WhenCallerIsTarget_ReturnsFailure()
        {
            var result = _sut.CanPromoteMember(Actor, GroupRole.Admin, Actor, GroupRole.Member, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("promote yourself", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the group owner cannot be promoted.</summary>
        [Fact]
        public void CanPromoteMember_WhenTargetIsOwner_ReturnsFailure()
        {
            var result = _sut.CanPromoteMember(Actor, GroupRole.Admin, OwnerId, GroupRole.Member, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("owner cannot be promoted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that only the owner can promote a co-admin to full admin.</summary>
        [Fact]
        public void CanPromoteMember_WhenAdminPromotesCoAdmin_ReturnsFailure()
        {
            var result = _sut.CanPromoteMember(Actor, GroupRole.Admin, Target, GroupRole.CoAdmin, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("Only the group owner", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the owner can promote a co-admin to full admin.</summary>
        [Fact]
        public void CanPromoteMember_WhenOwnerPromotesCoAdmin_ReturnsSuccess()
        {
            var result = _sut.CanPromoteMember(OwnerId, GroupRole.Admin, Target, GroupRole.CoAdmin, OwnerId);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a full admin cannot be promoted further.</summary>
        [Fact]
        public void CanPromoteMember_WhenTargetIsAlreadyAdmin_ReturnsFailure()
        {
            var result = _sut.CanPromoteMember(OwnerId, GroupRole.Admin, Target, GroupRole.Admin, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("already a full administrator", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanDemoteMember ----

        /// <summary>Verifies that the owner can demote an admin to co-admin.</summary>
        [Fact]
        public void CanDemoteMember_WhenOwnerDemotesAdmin_ReturnsSuccess()
        {
            var result = _sut.CanDemoteMember(OwnerId, GroupRole.Admin, Target, GroupRole.Admin, OwnerId);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that only the owner can demote an administrator.</summary>
        [Fact]
        public void CanDemoteMember_WhenAdminDemotesAdmin_ReturnsFailure()
        {
            var result = _sut.CanDemoteMember(Actor, GroupRole.Admin, Target, GroupRole.Admin, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("Only the group owner", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that an admin can demote a co-admin to member.</summary>
        [Fact]
        public void CanDemoteMember_WhenAdminDemotesCoAdmin_ReturnsSuccess()
        {
            var result = _sut.CanDemoteMember(Actor, GroupRole.Admin, Target, GroupRole.CoAdmin, OwnerId);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a regular member cannot be demoted further.</summary>
        [Fact]
        public void CanDemoteMember_WhenTargetIsMember_ReturnsFailure()
        {
            var result = _sut.CanDemoteMember(OwnerId, GroupRole.Admin, Target, GroupRole.Member, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("cannot be demoted further", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a non-admin caller cannot demote group leaders.</summary>
        [Fact]
        public void CanDemoteMember_WhenCallerIsCoAdmin_ReturnsFailure()
        {
            var result = _sut.CanDemoteMember(Actor, GroupRole.CoAdmin, Target, GroupRole.CoAdmin, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("permission to demote", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a user cannot demote themselves.</summary>
        [Fact]
        public void CanDemoteMember_WhenCallerIsTarget_ReturnsFailure()
        {
            var result = _sut.CanDemoteMember(Actor, GroupRole.Admin, Actor, GroupRole.CoAdmin, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("demote yourself", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the group owner cannot be demoted.</summary>
        [Fact]
        public void CanDemoteMember_WhenTargetIsOwner_ReturnsFailure()
        {
            var result = _sut.CanDemoteMember(Actor, GroupRole.Admin, OwnerId, GroupRole.Admin, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("owner cannot be demoted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanKickMember ----

        /// <summary>Verifies that the owner can kick an administrator.</summary>
        [Fact]
        public void CanKickMember_WhenOwnerKicksAdmin_ReturnsSuccess()
        {
            var result = _sut.CanKickMember(OwnerId, GroupRole.Admin, Target, GroupRole.Admin, OwnerId);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that only the owner can kick an administrator.</summary>
        [Fact]
        public void CanKickMember_WhenAdminKicksAdmin_ReturnsFailure()
        {
            var result = _sut.CanKickMember(Actor, GroupRole.Admin, Target, GroupRole.Admin, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("Only the group owner", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that an admin can kick a co-admin.</summary>
        [Fact]
        public void CanKickMember_WhenAdminKicksCoAdmin_ReturnsSuccess()
        {
            var result = _sut.CanKickMember(Actor, GroupRole.Admin, Target, GroupRole.CoAdmin, OwnerId);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a co-admin cannot kick another co-admin.</summary>
        [Fact]
        public void CanKickMember_WhenCoAdminKicksCoAdmin_ReturnsFailure()
        {
            var result = _sut.CanKickMember(Actor, GroupRole.CoAdmin, Target, GroupRole.CoAdmin, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("owner or administrators", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a co-admin can kick a regular member.</summary>
        [Fact]
        public void CanKickMember_WhenCoAdminKicksMember_ReturnsSuccess()
        {
            var result = _sut.CanKickMember(Actor, GroupRole.CoAdmin, Target, GroupRole.Member, OwnerId);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a regular member cannot kick anyone.</summary>
        [Fact]
        public void CanKickMember_WhenCallerIsMember_ReturnsFailure()
        {
            var result = _sut.CanKickMember(Actor, GroupRole.Member, Target, GroupRole.Member, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("do not have permission", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that kicking a non-member is rejected.</summary>
        [Fact]
        public void CanKickMember_WhenTargetIsNotMember_ReturnsFailure()
        {
            var result = _sut.CanKickMember(OwnerId, GroupRole.Admin, Target, null, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("Target user is not a member", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the group owner cannot be kicked.</summary>
        [Fact]
        public void CanKickMember_WhenTargetIsOwner_ReturnsFailure()
        {
            var result = _sut.CanKickMember(Actor, GroupRole.Admin, OwnerId, GroupRole.Member, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("owner cannot be removed", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that an undefined target role is rejected (invalid enum branch).</summary>
        [Fact]
        public void CanKickMember_WithUndefinedTargetRole_ReturnsFailure()
        {
            var result = _sut.CanKickMember(OwnerId, GroupRole.Admin, Target, (GroupRole)99, OwnerId);

            Assert.True(result.IsFailure);
            Assert.Contains("Invalid target membership role", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanLeaveGroup ----

        /// <summary>Verifies that a non-owner can leave a group regardless of member count.</summary>
        [Fact]
        public void CanLeaveGroup_WhenCallerIsNotOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanLeaveGroup(Actor, GroupId, isOwner: false, eligibleReplacementsCount: 1).IsSuccess);
        }

        /// <summary>Verifies the leave boundary: an owner with at least one other active member can leave.</summary>
        [Fact]
        public void CanLeaveGroup_WhenOwnerHasEligibleReplacements_ReturnsSuccess()
        {
            Assert.True(_sut.CanLeaveGroup(OwnerId, GroupId, isOwner: true, eligibleReplacementsCount: 2).IsSuccess);
        }

        /// <summary>Verifies the leave boundary: the sole member cannot leave and must delete the group.</summary>
        [Fact]
        public void CanLeaveGroup_WhenOwnerIsOnlyMember_ReturnsFailure()
        {
            var result = _sut.CanLeaveGroup(OwnerId, GroupId, isOwner: true, eligibleReplacementsCount: 1);

            Assert.True(result.IsFailure);
            Assert.Contains("only member", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the leave boundary: a zero-member count also blocks an owner from leaving.</summary>
        [Fact]
        public void CanLeaveGroup_WhenOwnerWithNoMembers_ReturnsFailure()
        {
            Assert.True(_sut.CanLeaveGroup(OwnerId, GroupId, isOwner: true, eligibleReplacementsCount: 0).IsFailure);
        }

        // ---- CanReviewJoinRequest ----

        /// <summary>Verifies that the group owner can review join requests.</summary>
        [Fact]
        public void CanReviewJoinRequest_WhenCallerIsOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanReviewJoinRequest(OwnerId, GroupRole.Member, isOwner: true).IsSuccess);
        }

        /// <summary>Verifies that a group admin can review join requests.</summary>
        [Fact]
        public void CanReviewJoinRequest_WhenCallerIsAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanReviewJoinRequest(Actor, GroupRole.Admin, isOwner: false).IsSuccess);
        }

        /// <summary>Verifies that a co-admin cannot review join requests.</summary>
        [Fact]
        public void CanReviewJoinRequest_WhenCallerIsCoAdmin_ReturnsFailure()
        {
            var result = _sut.CanReviewJoinRequest(Actor, GroupRole.CoAdmin, isOwner: false);

            Assert.True(result.IsFailure);
            Assert.Contains("owner or group administrators", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a regular member cannot review join requests.</summary>
        [Fact]
        public void CanReviewJoinRequest_WhenCallerIsMember_ReturnsFailure()
        {
            Assert.True(_sut.CanReviewJoinRequest(Actor, GroupRole.Member, isOwner: false).IsFailure);
        }

        /// <summary>Verifies that a non-member cannot review join requests.</summary>
        [Fact]
        public void CanReviewJoinRequest_WhenCallerIsNotMember_ReturnsFailure()
        {
            var result = _sut.CanReviewJoinRequest(Actor, null, isOwner: false);

            Assert.True(result.IsFailure);
            Assert.Contains("not a member", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }
}
