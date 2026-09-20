using Sohba.Domain.Domain_Rules.Logic;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for page rules: creation validation, following, editing,
    /// posting as page, deletion, kicking, promotion/demotion and
    /// ownership transfer (PageRole hierarchy Member→CoAdmin→Admin→PageOwner).
    /// </summary>
    public class PageDomainServiceTests
    {
        private readonly PageDomainService _sut = new();

        private static readonly Guid Actor = Guid.NewGuid();
        private static readonly Guid Target = Guid.NewGuid();

        private static Page PageOwnedBy(Guid adminId) => new() { Id = Guid.NewGuid(), AdminId = adminId };

        // ---- CanCreatePage ----

        /// <summary>Verifies that a valid page name is accepted.</summary>
        [Fact]
        public void CanCreatePage_WithValidName_ReturnsSuccess()
        {
            Assert.True(_sut.CanCreatePage("My Page").IsSuccess);
        }

        /// <summary>Verifies that a null page name is rejected.</summary>
        [Fact]
        public void CanCreatePage_WithNullName_ReturnsFailure()
        {
            var result = _sut.CanCreatePage(null);

            Assert.True(result.IsFailure);
            Assert.Contains("empty", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a whitespace-only page name is rejected.</summary>
        [Fact]
        public void CanCreatePage_WithWhitespaceName_ReturnsFailure()
        {
            Assert.True(_sut.CanCreatePage("   ").IsFailure);
        }

        /// <summary>Verifies the name boundary: 2 characters is too short.</summary>
        [Fact]
        public void CanCreatePage_WithTwoCharacters_ReturnsFailure()
        {
            var result = _sut.CanCreatePage("ab");

            Assert.True(result.IsFailure);
            Assert.Contains("too short", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the name boundary: exactly 3 characters is accepted.</summary>
        [Fact]
        public void CanCreatePage_WithThreeCharacters_ReturnsSuccess()
        {
            Assert.True(_sut.CanCreatePage("abc").IsSuccess);
        }

        /// <summary>Verifies the name boundary: exactly 100 characters is accepted.</summary>
        [Fact]
        public void CanCreatePage_With100Characters_ReturnsSuccess()
        {
            Assert.True(_sut.CanCreatePage(new string('a', 100)).IsSuccess);
        }

        /// <summary>Verifies the name boundary: 101 characters is too long.</summary>
        [Fact]
        public void CanCreatePage_With101Characters_ReturnsFailure()
        {
            var result = _sut.CanCreatePage(new string('a', 101));

            Assert.True(result.IsFailure);
            Assert.Contains("too long", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanFollowPage ----

        /// <summary>Verifies that a user can follow an existing page they do not follow yet.</summary>
        [Fact]
        public void CanFollowPage_WhenNotAlreadyFollowing_ReturnsSuccess()
        {
            var page = PageOwnedBy(Guid.NewGuid());

            Assert.True(_sut.CanFollowPage(Actor, page, alreadyFollowing: false).IsSuccess);
        }

        /// <summary>Verifies that following a non-existent page is rejected.</summary>
        [Fact]
        public void CanFollowPage_WhenPageIsNull_ReturnsFailure()
        {
            var result = _sut.CanFollowPage(Actor, null, alreadyFollowing: false);

            Assert.True(result.IsFailure);
            Assert.Contains("does not exist", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the page owner cannot follow their own page.</summary>
        [Fact]
        public void CanFollowPage_WhenCallerIsPageOwner_ReturnsFailure()
        {
            var page = PageOwnedBy(Actor);

            var result = _sut.CanFollowPage(Actor, page, alreadyFollowing: false);

            Assert.True(result.IsFailure);
            Assert.Contains("page owner", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that duplicate follows are rejected.</summary>
        [Fact]
        public void CanFollowPage_WhenAlreadyFollowing_ReturnsFailure()
        {
            var page = PageOwnedBy(Guid.NewGuid());

            var result = _sut.CanFollowPage(Actor, page, alreadyFollowing: true);

            Assert.True(result.IsFailure);
            Assert.Contains("already following", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanUnfollowPage ----

        /// <summary>Verifies that an existing follower can unfollow a page.</summary>
        [Fact]
        public void CanUnfollowPage_WhenAlreadyFollowing_ReturnsSuccess()
        {
            Assert.True(_sut.CanUnfollowPage(alreadyFollowing: true).IsSuccess);
        }

        /// <summary>Verifies that unfollowing a page not followed is rejected.</summary>
        [Fact]
        public void CanUnfollowPage_WhenNotFollowing_ReturnsFailure()
        {
            Assert.True(_sut.CanUnfollowPage(alreadyFollowing: false).IsFailure);
        }

        // ---- CanEditPage ----

        /// <summary>Verifies that the page owner can edit page settings.</summary>
        [Fact]
        public void CanEditPage_WhenCallerIsPageOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanEditPage(PageRole.PageOwner).IsSuccess);
        }

        /// <summary>Verifies that an admin can edit page settings.</summary>
        [Fact]
        public void CanEditPage_WhenCallerIsAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanEditPage(PageRole.Admin).IsSuccess);
        }

        /// <summary>Verifies that a co-admin cannot edit page settings.</summary>
        [Fact]
        public void CanEditPage_WhenCallerIsCoAdmin_ReturnsFailure()
        {
            var result = _sut.CanEditPage(PageRole.CoAdmin);

            Assert.True(result.IsFailure);
            Assert.Contains("Admins or the Page Owner", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a member cannot edit page settings.</summary>
        [Fact]
        public void CanEditPage_WhenCallerIsMember_ReturnsFailure()
        {
            Assert.True(_sut.CanEditPage(PageRole.Member).IsFailure);
        }

        /// <summary>Verifies that a caller with no role cannot edit page settings.</summary>
        [Fact]
        public void CanEditPage_WhenCallerHasNoRole_ReturnsFailure()
        {
            Assert.True(_sut.CanEditPage(null).IsFailure);
        }

        // ---- CanPostAsPage ----

        /// <summary>Verifies that a co-admin can post as the page.</summary>
        [Fact]
        public void CanPostAsPage_WhenCallerIsCoAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanPostAsPage(PageRole.CoAdmin).IsSuccess);
        }

        /// <summary>Verifies that an admin can post as the page.</summary>
        [Fact]
        public void CanPostAsPage_WhenCallerIsAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanPostAsPage(PageRole.Admin).IsSuccess);
        }

        /// <summary>Verifies that the page owner can post as the page.</summary>
        [Fact]
        public void CanPostAsPage_WhenCallerIsPageOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanPostAsPage(PageRole.PageOwner).IsSuccess);
        }

        /// <summary>Verifies that a member cannot post as the page.</summary>
        [Fact]
        public void CanPostAsPage_WhenCallerIsMember_ReturnsFailure()
        {
            var result = _sut.CanPostAsPage(PageRole.Member);

            Assert.True(result.IsFailure);
            Assert.Contains("do not have permission", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a caller with no role cannot post as the page.</summary>
        [Fact]
        public void CanPostAsPage_WhenCallerHasNoRole_ReturnsFailure()
        {
            Assert.True(_sut.CanPostAsPage(null).IsFailure);
        }

        // ---- CanDeletePage ----

        /// <summary>Verifies that only the page owner can delete the page.</summary>
        [Fact]
        public void CanDeletePage_WhenCallerIsPageOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanDeletePage(Actor, PageRole.PageOwner).IsSuccess);
        }

        /// <summary>Verifies that an admin cannot delete the page.</summary>
        [Fact]
        public void CanDeletePage_WhenCallerIsAdmin_ReturnsFailure()
        {
            var result = _sut.CanDeletePage(Actor, PageRole.Admin);

            Assert.True(result.IsFailure);
            Assert.Contains("Only the Page Owner", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a member cannot delete the page.</summary>
        [Fact]
        public void CanDeletePage_WhenCallerIsMember_ReturnsFailure()
        {
            Assert.True(_sut.CanDeletePage(Actor, PageRole.Member).IsFailure);
        }

        /// <summary>Verifies that a caller with no role cannot delete the page.</summary>
        [Fact]
        public void CanDeletePage_WhenCallerHasNoRole_ReturnsFailure()
        {
            Assert.True(_sut.CanDeletePage(Actor, null).IsFailure);
        }

        // ---- CanKickPageMember ----

        /// <summary>Verifies that the page owner can kick an admin.</summary>
        [Fact]
        public void CanKickPageMember_WhenOwnerKicksAdmin_ReturnsSuccess()
        {
            var result = _sut.CanKickPageMember(Actor, PageRole.PageOwner, Target, PageRole.Admin);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that an admin can kick a co-admin.</summary>
        [Fact]
        public void CanKickPageMember_WhenAdminKicksCoAdmin_ReturnsSuccess()
        {
            var result = _sut.CanKickPageMember(Actor, PageRole.Admin, Target, PageRole.CoAdmin);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that an admin cannot kick another admin (equal role).</summary>
        [Fact]
        public void CanKickPageMember_WhenRolesAreEqual_ReturnsFailure()
        {
            var result = _sut.CanKickPageMember(Actor, PageRole.Admin, Target, PageRole.Admin);

            Assert.True(result.IsFailure);
            Assert.Contains("same or higher", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that an admin cannot kick the page owner (higher role).</summary>
        [Fact]
        public void CanKickPageMember_WhenTargetHasHigherRole_ReturnsFailure()
        {
            var result = _sut.CanKickPageMember(Actor, PageRole.Admin, Target, PageRole.PageOwner);

            Assert.True(result.IsFailure);
        }

        /// <summary>Verifies that a member cannot kick anyone.</summary>
        [Fact]
        public void CanKickPageMember_WhenCallerIsMember_ReturnsFailure()
        {
            var result = _sut.CanKickPageMember(Actor, PageRole.Member, Target, PageRole.Member);

            Assert.True(result.IsFailure);
            Assert.Contains("do not have permission", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a caller with no role cannot kick members.</summary>
        [Fact]
        public void CanKickPageMember_WhenCallerHasNoRole_ReturnsFailure()
        {
            var result = _sut.CanKickPageMember(Actor, null, Target, PageRole.Member);

            Assert.True(result.IsFailure);
        }

        /// <summary>Verifies that a user cannot kick themselves.</summary>
        [Fact]
        public void CanKickPageMember_WhenCallerIsTarget_ReturnsFailure()
        {
            var result = _sut.CanKickPageMember(Actor, PageRole.Admin, Actor, PageRole.Member);

            Assert.True(result.IsFailure);
            Assert.Contains("remove yourself", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that kicking a non-member is rejected.</summary>
        [Fact]
        public void CanKickPageMember_WhenTargetHasNoRole_ReturnsFailure()
        {
            var result = _sut.CanKickPageMember(Actor, PageRole.Admin, Target, null);

            Assert.True(result.IsFailure);
            Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanPromotePageMember ----

        /// <summary>Verifies that the owner can promote a member to admin.</summary>
        [Fact]
        public void CanPromotePageMember_WhenOwnerPromotesMemberToAdmin_ReturnsSuccess()
        {
            var result = _sut.CanPromotePageMember(Actor, Target, PageRole.PageOwner, PageRole.Member, PageRole.Admin);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that an admin can promote a member to co-admin.</summary>
        [Fact]
        public void CanPromotePageMember_WhenAdminPromotesMemberToCoAdmin_ReturnsSuccess()
        {
            var result = _sut.CanPromotePageMember(Actor, Target, PageRole.Admin, PageRole.Member, PageRole.CoAdmin);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a non-owner admin cannot promote a member to admin.</summary>
        [Fact]
        public void CanPromotePageMember_WhenAdminPromotesToAdmin_ReturnsFailure()
        {
            var result = _sut.CanPromotePageMember(Actor, Target, PageRole.Admin, PageRole.Member, PageRole.Admin);

            Assert.True(result.IsFailure);
            Assert.Contains("Only the Page Owner", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a member cannot promote others.</summary>
        [Fact]
        public void CanPromotePageMember_WhenCallerIsMember_ReturnsFailure()
        {
            var result = _sut.CanPromotePageMember(Actor, Target, PageRole.Member, PageRole.Member, PageRole.CoAdmin);

            Assert.True(result.IsFailure);
        }

        /// <summary>Verifies that promoting a member who already holds an equal or higher role is rejected.</summary>
        [Fact]
        public void CanPromotePageMember_WhenTargetAlreadyHasRole_ReturnsFailure()
        {
            var result = _sut.CanPromotePageMember(Actor, Target, PageRole.PageOwner, PageRole.CoAdmin, PageRole.CoAdmin);

            Assert.True(result.IsFailure);
            Assert.Contains("already holds", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a user cannot change their own page role.</summary>
        [Fact]
        public void CanPromotePageMember_WhenCallerIsTarget_ReturnsFailure()
        {
            var result = _sut.CanPromotePageMember(Actor, Actor, PageRole.Admin, PageRole.Member, PageRole.CoAdmin);

            Assert.True(result.IsFailure);
            Assert.Contains("your own role", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanDemotePageMember ----

        /// <summary>Verifies that the owner can demote an admin to co-admin.</summary>
        [Fact]
        public void CanDemotePageMember_WhenOwnerDemotesAdmin_ReturnsSuccess()
        {
            var result = _sut.CanDemotePageMember(Actor, Target, PageRole.PageOwner, PageRole.Admin, PageRole.CoAdmin);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that an admin can demote a co-admin to member.</summary>
        [Fact]
        public void CanDemotePageMember_WhenAdminDemotesCoAdmin_ReturnsSuccess()
        {
            var result = _sut.CanDemotePageMember(Actor, Target, PageRole.Admin, PageRole.CoAdmin, PageRole.Member);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a non-owner admin cannot demote another admin.</summary>
        [Fact]
        public void CanDemotePageMember_WhenAdminDemotesAdmin_ReturnsFailure()
        {
            var result = _sut.CanDemotePageMember(Actor, Target, PageRole.Admin, PageRole.Admin, PageRole.CoAdmin);

            Assert.True(result.IsFailure);
            Assert.Contains("same or higher", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the page owner can never be demoted.</summary>
        [Fact]
        public void CanDemotePageMember_WhenTargetIsPageOwner_ReturnsFailure()
        {
            var result = _sut.CanDemotePageMember(Actor, Target, PageRole.PageOwner, PageRole.PageOwner, PageRole.Admin);

            Assert.True(result.IsFailure);
            Assert.Contains("cannot be demoted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that demoting a member below their current role is rejected.</summary>
        [Fact]
        public void CanDemotePageMember_WhenTargetAlreadyAtOrBelowNewRole_ReturnsFailure()
        {
            var result = _sut.CanDemotePageMember(Actor, Target, PageRole.PageOwner, PageRole.Member, PageRole.Member);

            Assert.True(result.IsFailure);
            Assert.Contains("already holds", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that demoting a member with no role is rejected.</summary>
        [Fact]
        public void CanDemotePageMember_WhenTargetHasNoRole_ReturnsFailure()
        {
            var result = _sut.CanDemotePageMember(Actor, Target, PageRole.PageOwner, null, PageRole.Member);

            Assert.True(result.IsFailure);
        }

        /// <summary>Verifies that a user cannot change their own role.</summary>
        [Fact]
        public void CanDemotePageMember_WhenCallerIsTarget_ReturnsFailure()
        {
            var result = _sut.CanDemotePageMember(Actor, Actor, PageRole.Admin, PageRole.CoAdmin, PageRole.Member);

            Assert.True(result.IsFailure);
        }

        /// <summary>Verifies that a member cannot demote anyone.</summary>
        [Fact]
        public void CanDemotePageMember_WhenCallerIsMember_ReturnsFailure()
        {
            var result = _sut.CanDemotePageMember(Actor, Target, PageRole.Member, PageRole.CoAdmin, PageRole.Member);

            Assert.True(result.IsFailure);
            Assert.Contains("do not have permission", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanTransferOwnership ----

        /// <summary>Verifies that the owner can transfer ownership to an admin.</summary>
        [Fact]
        public void CanTransferOwnership_WhenOwnerTransfersToAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanTransferOwnership(Actor, PageRole.PageOwner, PageRole.Admin).IsSuccess);
        }

        /// <summary>Verifies that ownership can only be transferred to an admin.</summary>
        [Fact]
        public void CanTransferOwnership_WhenTargetIsCoAdmin_ReturnsFailure()
        {
            var result = _sut.CanTransferOwnership(Actor, PageRole.PageOwner, PageRole.CoAdmin);

            Assert.True(result.IsFailure);
            Assert.Contains("only be transferred to an Admin", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a target with no role cannot receive ownership.</summary>
        [Fact]
        public void CanTransferOwnership_WhenTargetHasNoRole_ReturnsFailure()
        {
            Assert.True(_sut.CanTransferOwnership(Actor, PageRole.PageOwner, null).IsFailure);
        }

        /// <summary>Verifies that a non-owner cannot transfer ownership.</summary>
        [Fact]
        public void CanTransferOwnership_WhenCallerIsNotOwner_ReturnsFailure()
        {
            var result = _sut.CanTransferOwnership(Actor, PageRole.Admin, PageRole.Admin);

            Assert.True(result.IsFailure);
            Assert.Contains("Only the Page Owner", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a caller with no role cannot transfer ownership.</summary>
        [Fact]
        public void CanTransferOwnership_WhenCallerHasNoRole_ReturnsFailure()
        {
            Assert.True(_sut.CanTransferOwnership(Actor, null, PageRole.Admin).IsFailure);
        }
    }
}
