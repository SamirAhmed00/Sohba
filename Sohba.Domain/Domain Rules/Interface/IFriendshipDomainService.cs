using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface IFriendshipDomainService
    {
        // Request Management
        Result CanSendFriendRequest(Guid senderId, Guid receiverId);
        Result CanAcceptFriendRequest(Guid senderId, Guid receiverId);
        Result CanDeclineFriendRequest(Guid senderId, Guid receiverId);
        Result CanCancelFriendRequest(Guid userId, Guid friendId);

        // Relationship Management
        Result CanRemoveFriend(Guid userId, Guid friendId);
        Result CanBlockUser(Guid userId, Guid targetId); 
        Result CanUnblockUser(Guid userId, Guid targetId);

        // Quick Status Checks
        bool IsFriend(Guid userId, Guid otherUserId);
        bool IsBlocked(Guid userId, Guid otherUserId);
        Result HasPendingRequest(Guid userId, Guid friendId);
    }
}
