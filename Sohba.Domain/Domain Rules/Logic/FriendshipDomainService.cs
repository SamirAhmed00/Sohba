using System;
using System.Collections.Generic;
using System.Text;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Interfaces;

namespace Sohba.Domain.Domain_Rules.Logic
{

    public class FriendshipDomainService : IFriendshipDomainService
    {
        private readonly IUnitOfWork _uow;

        public FriendshipDomainService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public Result CanAcceptFriendRequest(Guid senderId, Guid receiverId)
        {
            // Ensure there is an actual pending request sent to the current user
            var hasRequest = _uow.Friendships.HasPendingRequest(senderId, receiverId);
            if (!hasRequest)
                return Result.Failure("No pending friend request found to accept.");

            return Result.Success();
        }

        public Result CanBlockUser(Guid userId, Guid targetId)
        {
            if (userId == targetId)
                return Result.Failure("You cannot block yourself.");

            return Result.Success();
        }

        public Result CanUnblockUser(Guid userId, Guid targetId)
        {
            // Unblocking attempt is generally allowed; logic is handled at the repo level
            return Result.Success();
        }

        public Result CanCancelFriendRequest(Guid userId, Guid friendId)
        {
            // Only the initiator can cancel the request
            var hasRequest = _uow.Friendships.HasPendingRequest(userId, friendId);
            if (!hasRequest)
                return Result.Failure("No sent request found to cancel.");

            return Result.Success();
        }

        public Result CanDeclineFriendRequest(Guid senderId, Guid receiverId)
        {
            var hasRequest = _uow.Friendships.HasPendingRequest(senderId, receiverId);
            if (!hasRequest)
                return Result.Failure("No pending friend request found to decline.");

            return Result.Success();
        }

        public Result CanRemoveFriend(Guid userId, Guid friendId)
        {
            if (!IsFriend(userId, friendId))
                return Result.Failure("You are not friends with this user.");

            return Result.Success();
        }

        public Result CanSendFriendRequest(Guid senderId, Guid receiverId)
        {
            // Rule 1: Cannot send a request to self
            if (senderId == receiverId)
                return Result.Failure("You cannot send a friend request to yourself.");

            // Rule 2: Check for block constraints between users
            if (IsBlocked(senderId, receiverId))
                return Result.Failure("Action denied due to blocking.");

            // Rule 3: Ensure no existing relationship or pending request
            var status = _uow.Friendships.GetFriendshipStatus(senderId, receiverId);
            if (status != "None")
                return Result.Failure("A relationship or pending request already exists.");

            return Result.Success();
        }

        public Result HasPendingRequest(Guid userId, Guid friendId)
        {
            var exists = _uow.Friendships.HasPendingRequest(userId, friendId);
            return exists ? Result.Success() : Result.Failure("No pending request found.");
        }

        public bool IsBlocked(Guid userId, Guid otherUserId)
        {
            // Accessing the blocking logic through the UOW
            return _uow.Friendships.IsUserBlocked(userId, otherUserId);
        }

        public bool IsFriend(Guid userId, Guid otherUserId)
        {
            return _uow.Friendships.AreFriends(userId, otherUserId);
        }
        
    }
}
