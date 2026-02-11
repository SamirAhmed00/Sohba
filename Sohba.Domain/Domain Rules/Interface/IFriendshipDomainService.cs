using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IFriendshipDomainService
    {
        // Request Management
        Result CanSendFriendRequest(
            Guid senderId,
            Guid receiverId,
            bool alreadyFriends,
            bool hasPendingRequest,
            bool isBlocked);

        Result CanAcceptFriendRequest(
            bool requestExists,
            bool alreadyFriends);

        Result CanDeclineFriendRequest(
            bool requestExists);

        Result CanCancelFriendRequest(
            bool requestExists);

        // Relationship Management
        Result CanRemoveFriend(
            bool alreadyFriends);

        Result CanBlockUser(
            Guid userId,
            Guid targetId,
            bool alreadyBlocked);

        Result CanUnblockUser(
            bool alreadyBlocked);
    }
}
