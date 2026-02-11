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
        public Result CanSendFriendRequest(
            Guid senderId,
            Guid receiverId,
            bool alreadyFriends,
            bool hasPendingRequest,
            bool isBlocked)
        {
            if (senderId == receiverId)
                return Result.Failure("You cannot send a friend request to yourself.");

            if (isBlocked)
                return Result.Failure("Action denied due to blocking.");

            if (alreadyFriends)
                return Result.Failure("You are already friends.");

            if (hasPendingRequest)
                return Result.Failure("A pending friend request already exists.");

            return Result.Success();
        }

        public Result CanAcceptFriendRequest(
            bool requestExists,
            bool alreadyFriends)
        {
            if (!requestExists)
                return Result.Failure("No pending friend request found.");

            if (alreadyFriends)
                return Result.Failure("You are already friends.");

            return Result.Success();
        }

        public Result CanDeclineFriendRequest(
            bool requestExists)
        {
            if (!requestExists)
                return Result.Failure("No pending friend request found.");

            return Result.Success();
        }

        public Result CanCancelFriendRequest(
            bool requestExists)
        {
            if (!requestExists)
                return Result.Failure("No sent request found to cancel.");

            return Result.Success();
        }

        public Result CanRemoveFriend(
            bool alreadyFriends)
        {
            if (!alreadyFriends)
                return Result.Failure("You are not friends with this user.");

            return Result.Success();
        }

        public Result CanBlockUser(
            Guid userId,
            Guid targetId,
            bool alreadyBlocked)
        {
            if (userId == targetId)
                return Result.Failure("You cannot block yourself.");

            if (alreadyBlocked)
                return Result.Failure("User is already blocked.");

            return Result.Success();
        }

        public Result CanUnblockUser(
            bool alreadyBlocked)
        {
            if (!alreadyBlocked)
                return Result.Failure("User is not blocked.");

            return Result.Success();
        }
    }

}
