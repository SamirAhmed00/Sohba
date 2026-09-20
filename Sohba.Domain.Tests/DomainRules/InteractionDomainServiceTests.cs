using Sohba.Domain.Domain_Rules.Logic;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for interaction rules: comments, reactions, comment
    /// deletion/editing authorization and reply-depth limits.
    /// </summary>
    public class InteractionDomainServiceTests
    {
        private readonly InteractionDomainService _sut = new();

        private static readonly Guid User = Guid.NewGuid();
        private static readonly Guid CommentOwner = Guid.NewGuid();
        private static readonly Guid PostOwner = Guid.NewGuid();

        // ---- CanAddComment ----

        /// <summary>Verifies that a valid comment on active, unblocked content succeeds.</summary>
        [Fact]
        public void CanAddComment_WithValidTextOnActiveContent_ReturnsSuccess()
        {
            Assert.True(_sut.CanAddComment(User, "nice post!", isContentDeleted: false, isBlockedByOwner: false).IsSuccess);
        }

        /// <summary>Verifies that comments on deleted content are rejected.</summary>
        [Fact]
        public void CanAddComment_WhenContentIsDeleted_ReturnsFailure()
        {
            var result = _sut.CanAddComment(User, "hello", isContentDeleted: true, isBlockedByOwner: false);

            Assert.True(result.IsFailure);
            Assert.Contains("deleted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a blocked user cannot comment.</summary>
        [Fact]
        public void CanAddComment_WhenBlockedByOwner_ReturnsFailure()
        {
            var result = _sut.CanAddComment(User, "hello", isContentDeleted: false, isBlockedByOwner: true);

            Assert.True(result.IsFailure);
        }

        /// <summary>Verifies that an empty comment text is rejected.</summary>
        [Fact]
        public void CanAddComment_WithEmptyText_ReturnsFailure()
        {
            Assert.True(_sut.CanAddComment(User, "", isContentDeleted: false, isBlockedByOwner: false).IsFailure);
        }

        /// <summary>Verifies that a whitespace-only comment text is rejected.</summary>
        [Fact]
        public void CanAddComment_WithWhitespaceText_ReturnsFailure()
        {
            Assert.True(_sut.CanAddComment(User, "   ", isContentDeleted: false, isBlockedByOwner: false).IsFailure);
        }

        /// <summary>Verifies that a null comment text is rejected.</summary>
        [Fact]
        public void CanAddComment_WithNullText_ReturnsFailure()
        {
            Assert.True(_sut.CanAddComment(User, null, isContentDeleted: false, isBlockedByOwner: false).IsFailure);
        }

        /// <summary>Verifies that the deleted check takes precedence over text validation.</summary>
        [Fact]
        public void CanAddComment_WhenDeletedAndEmptyText_ReportsDeletedContent()
        {
            var result = _sut.CanAddComment(User, "", isContentDeleted: true, isBlockedByOwner: false);

            Assert.True(result.IsFailure);
            Assert.Contains("deleted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the blocking check takes precedence over text validation.</summary>
        [Fact]
        public void CanAddComment_WhenBlockedAndEmptyText_ReportsBlocking()
        {
            var result = _sut.CanAddComment(User, "", isContentDeleted: false, isBlockedByOwner: true);

            Assert.True(result.IsFailure);
        }

        // ---- CanAddReaction ----

        /// <summary>Verifies that reactions on active content are allowed.</summary>
        [Fact]
        public void CanAddReaction_WhenActiveAndNotBlocked_ReturnsSuccess()
        {
            Assert.True(_sut.CanAddReaction(User, isContentDeleted: false, isUserBlocked: false).IsSuccess);
        }

        /// <summary>Verifies that reactions on deleted content are rejected.</summary>
        [Fact]
        public void CanAddReaction_WhenContentIsDeleted_ReturnsFailure()
        {
            var result = _sut.CanAddReaction(User, isContentDeleted: true, isUserBlocked: false);

            Assert.True(result.IsFailure);
            Assert.Contains("deleted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a blocked user cannot react.</summary>
        [Fact]
        public void CanAddReaction_WhenUserIsBlocked_ReturnsFailure()
        {
            var result = _sut.CanAddReaction(User, isContentDeleted: false, isUserBlocked: true);

            Assert.True(result.IsFailure);
            Assert.Contains("blocked", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanDeleteComment ----

        /// <summary>Verifies that the comment owner can delete their own comment.</summary>
        [Fact]
        public void CanDeleteComment_WhenCallerIsCommentOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanDeleteComment(CommentOwner, CommentOwner, PostOwner, isAdmin: false).IsSuccess);
        }

        /// <summary>Verifies that the post owner can delete a comment on their post.</summary>
        [Fact]
        public void CanDeleteComment_WhenCallerIsPostOwner_ReturnsSuccess()
        {
            Assert.True(_sut.CanDeleteComment(PostOwner, CommentOwner, PostOwner, isAdmin: false).IsSuccess);
        }

        /// <summary>Verifies that a system admin can delete any comment.</summary>
        [Fact]
        public void CanDeleteComment_WhenCallerIsAdmin_ReturnsSuccess()
        {
            Assert.True(_sut.CanDeleteComment(User, CommentOwner, PostOwner, isAdmin: true).IsSuccess);
        }

        /// <summary>Verifies that an unrelated user cannot delete someone else's comment.</summary>
        [Fact]
        public void CanDeleteComment_WhenCallerHasNoRights_ReturnsFailure()
        {
            var result = _sut.CanDeleteComment(User, CommentOwner, PostOwner, isAdmin: false);

            Assert.True(result.IsFailure);
            Assert.Contains("permission", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanEditComment ----

        /// <summary>Verifies that the comment owner can edit within the edit window.</summary>
        [Fact]
        public void CanEditComment_WhenOwnerWithinEditWindow_ReturnsSuccess()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-14);

            var result = _sut.CanEditComment(CommentOwner, CommentOwner, createdAt, editLimitMinutes: 15);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies the edit-window boundary: one minute past the window is rejected.</summary>
        [Fact]
        public void CanEditComment_WhenEditWindowExpired_ReturnsFailure()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-16);

            var result = _sut.CanEditComment(CommentOwner, CommentOwner, createdAt, editLimitMinutes: 15);

            Assert.True(result.IsFailure);
            Assert.Contains("older than", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a non-owner cannot edit a comment, even within the window.</summary>
        [Fact]
        public void CanEditComment_WhenCallerIsNotOwner_ReturnsFailure()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-1);

            var result = _sut.CanEditComment(User, CommentOwner, createdAt, editLimitMinutes: 15);

            Assert.True(result.IsFailure);
            Assert.Contains("your own comments", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that ownership takes precedence over the edit window.</summary>
        [Fact]
        public void CanEditComment_WhenNotOwnerAndExpired_ReportsOwnership()
        {
            var createdAt = DateTime.UtcNow.AddMinutes(-30);

            var result = _sut.CanEditComment(User, CommentOwner, createdAt, editLimitMinutes: 15);

            Assert.True(result.IsFailure);
            Assert.Contains("your own comments", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a zero-minute edit window immediately locks editing.</summary>
        [Fact]
        public void CanEditComment_WithZeroMinuteLimit_ReturnsFailure()
        {
            var createdAt = DateTime.UtcNow.AddSeconds(-1);

            Assert.True(_sut.CanEditComment(CommentOwner, CommentOwner, createdAt, editLimitMinutes: 0).IsFailure);
        }

        // ---- CanReplyToComment ----

        /// <summary>Verifies that a reply to an active, unlocked comment within limits succeeds.</summary>
        [Fact]
        public void CanReplyToComment_WithinAllLimits_ReturnsSuccess()
        {
            var result = _sut.CanReplyToComment(User, isCommentDeleted: false, isThreadLocked: false, currentDepth: 2, directReplyCount: 2);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that replies to a deleted comment are rejected.</summary>
        [Fact]
        public void CanReplyToComment_WhenCommentIsDeleted_ReturnsFailure()
        {
            var result = _sut.CanReplyToComment(User, isCommentDeleted: true, isThreadLocked: false, currentDepth: 0, directReplyCount: 0);

            Assert.True(result.IsFailure);
            Assert.Contains("deleted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that replies to a locked thread are rejected.</summary>
        [Fact]
        public void CanReplyToComment_WhenThreadIsLocked_ReturnsFailure()
        {
            var result = _sut.CanReplyToComment(User, isCommentDeleted: false, isThreadLocked: true, currentDepth: 0, directReplyCount: 0);

            Assert.True(result.IsFailure);
            Assert.Contains("locked", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the direct-replies boundary: 3 existing replies still allow one more.</summary>
        [Fact]
        public void CanReplyToComment_WithThreeDirectReplies_ReturnsSuccess()
        {
            var result = _sut.CanReplyToComment(User, isCommentDeleted: false, isThreadLocked: false, currentDepth: 0, directReplyCount: 3);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies the direct-replies boundary: the 5th direct reply (4 existing) is rejected.</summary>
        [Fact]
        public void CanReplyToComment_WithFourDirectReplies_ReturnsFailure()
        {
            var result = _sut.CanReplyToComment(User, isCommentDeleted: false, isThreadLocked: false, currentDepth: 0, directReplyCount: 4);

            Assert.True(result.IsFailure);
            Assert.Contains("maximum of 4 replies", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies the depth boundary: depth 3 still allows a nested reply.</summary>
        [Fact]
        public void CanReplyToComment_AtDepthThree_ReturnsSuccess()
        {
            var result = _sut.CanReplyToComment(User, isCommentDeleted: false, isThreadLocked: false, currentDepth: 3, directReplyCount: 0);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies the depth boundary: a reply at depth 4 is rejected.</summary>
        [Fact]
        public void CanReplyToComment_AtDepthFour_ReturnsFailure()
        {
            var result = _sut.CanReplyToComment(User, isCommentDeleted: false, isThreadLocked: false, currentDepth: 4, directReplyCount: 0);

            Assert.True(result.IsFailure);
            Assert.Contains("depth", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that the deleted check takes precedence over all other reply rules.</summary>
        [Fact]
        public void CanReplyToComment_WhenDeletedAndLocked_ReportsDeletedComment()
        {
            var result = _sut.CanReplyToComment(User, isCommentDeleted: true, isThreadLocked: true, currentDepth: 4, directReplyCount: 4);

            Assert.True(result.IsFailure);
            Assert.Contains("deleted", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanUpdateReaction ----

        /// <summary>Verifies that an existing reaction can be updated.</summary>
        [Fact]
        public void CanUpdateReaction_WhenReactionExists_ReturnsSuccess()
        {
            Assert.True(_sut.CanUpdateReaction(User, reactionExists: true).IsSuccess);
        }

        /// <summary>Verifies that updating a non-existent reaction is rejected.</summary>
        [Fact]
        public void CanUpdateReaction_WhenReactionDoesNotExist_ReturnsFailure()
        {
            var result = _sut.CanUpdateReaction(User, reactionExists: false);

            Assert.True(result.IsFailure);
            Assert.Contains("No reaction", result.Error);
        }
    }
}
