using Sohba.Domain.Domain_Rules.Logic;
using Sohba.Domain.Enums;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for post creation, update, deletion, visibility, commenting,
    /// reactions, sharing, group posting and reporting rules.
    /// </summary>
    public class PostDomainServiceTests
    {
        private readonly PostDomainService _sut = new();

        private static readonly Guid User = Guid.NewGuid();
        private static readonly Guid Other = Guid.NewGuid();
        private static readonly Guid PostId = Guid.NewGuid();
        private static readonly Guid GroupId = Guid.NewGuid();

        // ---- CanCreatePost ----

        /// <summary>Verifies that a post with text content can be created.</summary>
        [Fact]
        public void CanCreatePost_WhenContentPresent_ReturnsSuccess()
        {
            Assert.True(_sut.CanCreatePost(User, "hello world", hasAttachments: false).IsSuccess);
        }

        /// <summary>Verifies that a post with only attachments and no text can be created.</summary>
        [Fact]
        public void CanCreatePost_WhenOnlyAttachments_ReturnsSuccess()
        {
            Assert.True(_sut.CanCreatePost(User, null, hasAttachments: true).IsSuccess);
        }

        /// <summary>Verifies that an empty post with no text and no attachments is rejected.</summary>
        [Fact]
        public void CanCreatePost_WhenEmptyContentAndNoAttachments_ReturnsFailure()
        {
            Assert.True(_sut.CanCreatePost(User, "", hasAttachments: false).IsFailure);
        }

        /// <summary>Verifies that a whitespace-only post with no attachments is rejected.</summary>
        [Fact]
        public void CanCreatePost_WhenWhitespaceContentAndNoAttachments_ReturnsFailure()
        {
            Assert.True(_sut.CanCreatePost(User, "   ", hasAttachments: false).IsFailure);
        }

        /// <summary>Verifies that a null-content post with no attachments is rejected.</summary>
        [Fact]
        public void CanCreatePost_WhenNullContentAndNoAttachments_ReturnsFailure()
        {
            Assert.True(_sut.CanCreatePost(User, null, hasAttachments: false).IsFailure);
        }

        // ---- CanUpdatePost ----

        /// <summary>Verifies that the post owner can update their own active post.</summary>
        [Fact]
        public void CanUpdatePost_WhenCurrentUserIsOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanUpdatePost(User, PostId, User, isPostDeleted: false).IsSuccess);
        }

        /// <summary>Verifies that a non-owner cannot update another user's post.</summary>
        [Fact]
        public void CanUpdatePost_WhenCurrentUserIsNotOwner_ReturnsFailure()
        {
            var result = _sut.CanUpdatePost(Other, PostId, User, isPostDeleted: false);

            Assert.True(result.IsFailure);
            Assert.Contains("not authorized", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a deleted post cannot be updated, even by its owner.</summary>
        [Fact]
        public void CanUpdatePost_WhenPostIsDeleted_ReturnsFailure()
        {
            var result = _sut.CanUpdatePost(User, PostId, User, isPostDeleted: true);

            Assert.True(result.IsFailure);
            Assert.Contains("deleted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the deleted-state check takes precedence over ownership.</summary>
        [Fact]
        public void CanUpdatePost_WhenDeletedAndUserIsNotOwner_ReportsDeletedState()
        {
            var result = _sut.CanUpdatePost(Other, PostId, User, isPostDeleted: true);

            Assert.True(result.IsFailure);
            Assert.Contains("deleted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanDeletePost ----

        /// <summary>Verifies that a platform administrator can delete any post.</summary>
        [Fact]
        public void CanDeletePost_WhenCurrentUserIsPlatformAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanDeletePost(Other, PostId, User, isPlatformAdmin: true).IsSuccess);
        }

        /// <summary>Verifies that a container (group/page) administrator can delete a member's post.</summary>
        [Fact]
        public void CanDeletePost_WhenCurrentUserIsContainerAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanDeletePost(Other, PostId, User, isPlatformAdmin: false, isContainerAdmin: true).IsSuccess);
        }

        /// <summary>Verifies that the post owner can delete their own post.</summary>
        [Fact]
        public void CanDeletePost_WhenCurrentUserIsOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanDeletePost(User, PostId, User, isPlatformAdmin: false).IsSuccess);
        }

        /// <summary>Verifies that a regular user cannot delete another user's post.</summary>
        [Fact]
        public void CanDeletePost_WhenCurrentUserIsNotOwnerOrAdmin_ReturnsFailure()
        {
            var result = _sut.CanDeletePost(Other, PostId, User, isPlatformAdmin: false, isContainerAdmin: false);

            Assert.True(result.IsFailure);
            Assert.Contains("not authorized", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanViewPost ----

        /// <summary>Verifies that the owner always sees their own post regardless of privacy.</summary>
        [Fact]
        public void CanViewPost_WhenViewerIsOwner_ReturnsSuccess_EvenForPrivatePost()
        {
            Assert.True(_sut.CanViewPost(User, User, PostPrivacy.Private, isFriend: false).IsSuccess);
        }

        /// <summary>Verifies that a public post is visible to any user.</summary>
        [Fact]
        public void CanViewPost_WhenPostIsPublic_ReturnsSuccess()
        {
            Assert.True(_sut.CanViewPost(Other, User, PostPrivacy.Public, isFriend: false).IsSuccess);
        }

        /// <summary>Verifies that a friends-only post is visible to a friend.</summary>
        [Fact]
        public void CanViewPost_WhenFriendsOnlyAndViewerIsFriend_ReturnsSuccess()
        {
            Assert.True(_sut.CanViewPost(Other, User, PostPrivacy.Friends, isFriend: true).IsSuccess);
        }

        /// <summary>Verifies that a friends-only post is hidden from a non-friend.</summary>
        [Fact]
        public void CanViewPost_WhenFriendsOnlyAndViewerIsNotFriend_ReturnsFailure()
        {
            Assert.True(_sut.CanViewPost(Other, User, PostPrivacy.Friends, isFriend: false).IsFailure);
        }

        /// <summary>Verifies that a private post is hidden even from a friend of the owner.</summary>
        [Fact]
        public void CanViewPost_WhenPrivateAndViewerIsFriend_ReturnsFailure()
        {
            Assert.True(_sut.CanViewPost(Other, User, PostPrivacy.Private, isFriend: true).IsFailure);
        }

        /// <summary>Verifies that a private post is hidden from strangers.</summary>
        [Fact]
        public void CanViewPost_WhenPrivateAndViewerIsNotFriend_ReturnsFailure()
        {
            var result = _sut.CanViewPost(Other, User, PostPrivacy.Private, isFriend: false);

            Assert.True(result.IsFailure);
            Assert.Contains("private", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanCommentOnPost ----

        /// <summary>Verifies that an active, non-blocked user can comment on a post.</summary>
        [Fact]
        public void CanCommentOnPost_WhenActiveAndNotBlocked_ReturnsSuccess()
        {
            Assert.True(_sut.CanCommentOnPost(Other, User, isDeleted: false, isBlocked: false).IsSuccess);
        }

        /// <summary>Verifies that commenting on a deleted post is rejected.</summary>
        [Fact]
        public void CanCommentOnPost_WhenPostIsDeleted_ReturnsFailure()
        {
            var result = _sut.CanCommentOnPost(Other, User, isDeleted: true, isBlocked: false);

            Assert.True(result.IsFailure);
            Assert.Contains("deleted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a user blocked by the post owner cannot comment.</summary>
        [Fact]
        public void CanCommentOnPost_WhenUserIsBlocked_ReturnsFailure()
        {
            var result = _sut.CanCommentOnPost(Other, User, isDeleted: false, isBlocked: true);

            Assert.True(result.IsFailure);
            Assert.Contains("blocked", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the deleted check takes precedence over the blocking check.</summary>
        [Fact]
        public void CanCommentOnPost_WhenDeletedAndBlocked_ReportsDeletedState()
        {
            var result = _sut.CanCommentOnPost(Other, User, isDeleted: true, isBlocked: true);

            Assert.True(result.IsFailure);
            Assert.Contains("deleted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanReactToPost ----

        /// <summary>Verifies that reactions are allowed on active posts.</summary>
        [Fact]
        public void CanReactToPost_WhenPostIsNotDeleted_ReturnsSuccess()
        {
            Assert.True(_sut.CanReactToPost(Other, User, isDeleted: false).IsSuccess);
        }

        /// <summary>Verifies that reactions are rejected on deleted posts.</summary>
        [Fact]
        public void CanReactToPost_WhenPostIsDeleted_ReturnsFailure()
        {
            Assert.True(_sut.CanReactToPost(Other, User, isDeleted: true).IsFailure);
        }

        // ---- CanSharePost ----

        /// <summary>Verifies that a non-private post can be shared.</summary>
        [Fact]
        public void CanSharePost_WhenPostIsNotPrivate_ReturnsSuccess()
        {
            Assert.True(_sut.CanSharePost(User, PostId, isPrivate: false).IsSuccess);
        }

        /// <summary>Verifies that a private post cannot be shared.</summary>
        [Fact]
        public void CanSharePost_WhenPostIsPrivate_ReturnsFailure()
        {
            var result = _sut.CanSharePost(User, PostId, isPrivate: true);

            Assert.True(result.IsFailure);
            Assert.Contains("private", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanPostInGroup ----

        /// <summary>Verifies that an active member of an unlocked group can post.</summary>
        [Fact]
        public void CanPostInGroup_WhenMemberAndNotBanned_ReturnsSuccess()
        {
            Assert.True(_sut.CanPostInGroup(User, GroupId, isMember: true, isBannedFromGroup: false).IsSuccess);
        }

        /// <summary>Verifies that a banned member cannot post in the group.</summary>
        [Fact]
        public void CanPostInGroup_WhenBanned_ReturnsFailure()
        {
            var result = _sut.CanPostInGroup(User, GroupId, isMember: true, isBannedFromGroup: true);

            Assert.True(result.IsFailure);
            Assert.Contains("banned", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a non-member cannot post in the group.</summary>
        [Fact]
        public void CanPostInGroup_WhenNotMember_ReturnsFailure()
        {
            var result = _sut.CanPostInGroup(User, GroupId, isMember: false, isBannedFromGroup: false);

            Assert.True(result.IsFailure);
            Assert.Contains("member", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the ban check takes precedence over membership.</summary>
        [Fact]
        public void CanPostInGroup_WhenBannedAndNotMember_ReportsBan()
        {
            var result = _sut.CanPostInGroup(User, GroupId, isMember: false, isBannedFromGroup: true);

            Assert.True(result.IsFailure);
            Assert.Contains("banned", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanReportPost ----

        /// <summary>Verifies that a post can be reported the first time.</summary>
        [Fact]
        public void CanReportPost_WhenNotAlreadyReported_ReturnsSuccess()
        {
            Assert.True(_sut.CanReportPost(User, PostId, alreadyReported: false).IsSuccess);
        }

        /// <summary>Verifies that a duplicate report of the same post is rejected.</summary>
        [Fact]
        public void CanReportPost_WhenAlreadyReported_ReturnsFailure()
        {
            var result = _sut.CanReportPost(User, PostId, alreadyReported: true);

            Assert.True(result.IsFailure);
            Assert.Contains("already reported", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }
}
