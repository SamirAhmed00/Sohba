using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IFriendshipRepository : IGenericRepository<Friend>
    {
        bool AreFriends(Guid userId, Guid friendId);
        bool IsUserBlocked(Guid userId, Guid targetId);
        string GetFriendshipStatus(Guid userId, Guid otherId);
        bool HasPendingRequest(Guid senderId, Guid receiverId);
    }
}
