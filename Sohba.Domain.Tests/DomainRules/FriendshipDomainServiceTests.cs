using Sohba.Domain.Domain_Rules.Logic;

namespace Sohba.Domain.Tests.DomainRules
{
    /// <summary>
    /// Tests for every friendship rule: sending, accepting, declining,
    /// cancelling, removing, blocking and unblocking.
    /// </summary>
    public class FriendshipDomainServiceTests
    {
        private readonly FriendshipDomainService _sut = new();

        private static readonly Guid Sender = Guid.NewGuid();
        private static readonly Guid Receiver = Guid.NewGuid();

        // ---- CanSendFriendRequest ----

        /// <summary>Verifies that a valid friend request between two distinct, unblocked, non-friend users is allowed.</summary>
        [Fact]
        public void CanSendFriendRequest_WhenUsersAreUnrelated_ReturnsSuccess()
        {
            var result = _sut.CanSendFriendRequest(Sender, Receiver, alreadyFriends: false, hasPendingRequest: false, isBlocked: false);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that a user cannot send a friend request to themselves.</summary>
        [Fact]
        public void CanSendFriendRequest_WhenSenderEqualsReceiver_ReturnsFailure()
        {
            var result = _sut.CanSendFriendRequest(Sender, Sender, false, false, false);

            Assert.True(result.IsFailure);
            Assert.Contains("yourself", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a blocked pair cannot exchange friend requests, even when all other conditions are fine.</summary>
        [Fact]
        public void CanSendFriendRequest_WhenUsersAreBlocked_ReturnsFailure()
        {
            var result = _sut.CanSendFriendRequest(Sender, Receiver, alreadyFriends: false, hasPendingRequest: false, isBlocked: true);

            Assert.True(result.IsFailure);
            Assert.Contains("blocking", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that blocking takes precedence over the already-friends state (guard ordering).</summary>
        [Fact]
        public void CanSendFriendRequest_WhenBlockedAndAlreadyFriends_ReportsBlockingNotFriendship()
        {
            var result = _sut.CanSendFriendRequest(Sender, Receiver, alreadyFriends: true, hasPendingRequest: false, isBlocked: true);

            Assert.True(result.IsFailure);
            Assert.Contains("blocking", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that users who are already friends cannot send another friend request.</summary>
        [Fact]
        public void CanSendFriendRequest_WhenAlreadyFriends_ReturnsFailure()
        {
            var result = _sut.CanSendFriendRequest(Sender, Receiver, alreadyFriends: true, hasPendingRequest: false, isBlocked: false);

            Assert.True(result.IsFailure);
            Assert.Contains("already friends", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that a duplicate request is rejected while a pending request already exists.</summary>
        [Fact]
        public void CanSendFriendRequest_WhenPendingRequestExists_ReturnsFailure()
        {
            var result = _sut.CanSendFriendRequest(Sender, Receiver, alreadyFriends: false, hasPendingRequest: true, isBlocked: false);

            Assert.True(result.IsFailure);
            Assert.Contains("pending", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanAcceptFriendRequest ----

        /// <summary>Verifies that an existing pending request can be accepted.</summary>
        [Fact]
        public void CanAcceptFriendRequest_WhenRequestExists_ReturnsSuccess()
        {
            var result = _sut.CanAcceptFriendRequest(requestExists: true, alreadyFriends: false);

            Assert.True(result.IsSuccess);
        }

        /// <summary>Verifies that accepting fails when no pending request exists.</summary>
        [Fact]
        public void CanAcceptFriendRequest_WhenRequestDoesNotExist_ReturnsFailure()
        {
            var result = _sut.CanAcceptFriendRequest(requestExists: false, alreadyFriends: false);

            Assert.True(result.IsFailure);
        }

        /// <summary>Verifies that acceptance is rejected when the users are already friends (duplicate state).</summary>
        [Fact]
        public void CanAcceptFriendRequest_WhenAlreadyFriends_ReturnsFailure()
        {
            var result = _sut.CanAcceptFriendRequest(requestExists: true, alreadyFriends: true);

            Assert.True(result.IsFailure);
        }

        /// <summary>Verifies that the missing-request check takes precedence over the already-friends state.</summary>
        [Fact]
        public void CanAcceptFriendRequest_WhenNoRequestAndAlreadyFriends_ReportsMissingRequest()
        {
            var result = _sut.CanAcceptFriendRequest(requestExists: false, alreadyFriends: true);

            Assert.True(result.IsFailure);
            Assert.Contains("No pending", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanDeclineFriendRequest / CanCancelFriendRequest ----

        /// <summary>Verifies that an existing pending request can be declined.</summary>
        [Fact]
        public void CanDeclineFriendRequest_WhenRequestExists_ReturnsSuccess()
        {
            Assert.True(_sut.CanDeclineFriendRequest(requestExists: true).IsSuccess);
        }

        /// <summary>Verifies that declining fails when no pending request exists.</summary>
        [Fact]
        public void CanDeclineFriendRequest_WhenRequestDoesNotExist_ReturnsFailure()
        {
            Assert.True(_sut.CanDeclineFriendRequest(requestExists: false).IsFailure);
        }

        /// <summary>Verifies that a sent request can be cancelled by its sender.</summary>
        [Fact]
        public void CanCancelFriendRequest_WhenRequestExists_ReturnsSuccess()
        {
            Assert.True(_sut.CanCancelFriendRequest(requestExists: true).IsSuccess);
        }

        /// <summary>Verifies that cancelling fails when there is no sent request to cancel.</summary>
        [Fact]
        public void CanCancelFriendRequest_WhenRequestDoesNotExist_ReturnsFailure()
        {
            var result = _sut.CanCancelFriendRequest(requestExists: false);

            Assert.True(result.IsFailure);
            Assert.Contains("cancel", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanRemoveFriend ----

        /// <summary>Verifies that an existing friendship can be removed.</summary>
        [Fact]
        public void CanRemoveFriend_WhenAlreadyFriends_ReturnsSuccess()
        {
            Assert.True(_sut.CanRemoveFriend(alreadyFriends: true).IsSuccess);
        }

        /// <summary>Verifies that removing fails when the users are not friends.</summary>
        [Fact]
        public void CanRemoveFriend_WhenNotFriends_ReturnsFailure()
        {
            Assert.True(_sut.CanRemoveFriend(alreadyFriends: false).IsFailure);
        }

        // ---- CanBlockUser ----

        /// <summary>Verifies that a user can block another user.</summary>
        [Fact]
        public void CanBlockUser_WhenDifferentUsers_ReturnsSuccess()
        {
            Assert.True(_sut.CanBlockUser(Sender, Receiver, alreadyBlocked: false).IsSuccess);
        }

        /// <summary>Verifies that a user cannot block themselves.</summary>
        [Fact]
        public void CanBlockUser_WhenBlockingSelf_ReturnsFailure()
        {
            var result = _sut.CanBlockUser(Sender, Sender, alreadyBlocked: false);

            Assert.True(result.IsFailure);
            Assert.Contains("yourself", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Verifies that blocking an already-blocked user is rejected (duplicate operation).</summary>
        [Fact]
        public void CanBlockUser_WhenAlreadyBlocked_ReturnsFailure()
        {
            var result = _sut.CanBlockUser(Sender, Receiver, alreadyBlocked: true);

            Assert.True(result.IsFailure);
            Assert.Contains("already blocked", result.Error, StringComparison.OrdinalIgnoreCase);
        }

        // ---- CanUnblockUser ----

        /// <summary>Verifies that a blocked user can be unblocked.</summary>
        [Fact]
        public void CanUnblockUser_WhenAlreadyBlocked_ReturnsSuccess()
        {
            Assert.True(_sut.CanUnblockUser(alreadyBlocked: true).IsSuccess);
        }

        /// <summary>Verifies that unblocking fails when the user is not blocked.</summary>
        [Fact]
        public void CanUnblockUser_WhenNotBlocked_ReturnsFailure()
        {
            Assert.True(_sut.CanUnblockUser(alreadyBlocked: false).IsFailure);
        }
    }
}
