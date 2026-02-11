using Sohba.Application.DTOs.UserAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IFriendshipService
    {
        Task<Result> SendFriendRequestAsync(Guid senderId, Guid receiverId);
        Task<Result> AcceptFriendRequestAsync(Guid senderId, Guid receiverId); 
        Task<Result> RejectFriendRequestAsync(Guid senderId, Guid receiverId);
        Task<Result> UnfriendAsync(Guid userId, Guid friendId);
        Task<Result<IEnumerable<FriendDto>>> GetFriendsListAsync(Guid userId);
    }
}
