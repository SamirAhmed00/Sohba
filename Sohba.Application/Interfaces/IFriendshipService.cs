using Sohba.Application.DTOs.UserAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IFriendshipService
    {
        // Friend Requests
        Task<Result> SendFriendRequestAsync(Guid senderId, Guid receiverId);
        Task<Result> AcceptFriendRequestAsync(Guid senderId, Guid receiverId); 
        Task<Result> RejectFriendRequestAsync(Guid senderId, Guid receiverId);
        Task<Result> CancelFriendRequestAsync(Guid senderId, Guid receiverId);

        // Friends Management
        Task<Result> UnfriendAsync(Guid userId, Guid friendId);
        Task<Result<IEnumerable<FriendDto>>> GetFriendsListAsync(Guid userId);


        Task<Result<IEnumerable<FriendDto>>> GetPendingRequestsAsync(Guid userId);
        Task<Result<IEnumerable<FriendDto>>> GetSentRequestsAsync(Guid userId);
        Task<Result<int>> GetPendingRequestsCountAsync(Guid userId);

        // Blocking
        Task<Result> BlockUserAsync(Guid userId, Guid targetId);
        Task<Result> UnblockUserAsync(Guid userId, Guid targetId);
        Task<Result<IEnumerable<FriendDto>>> GetBlockedUsersAsync(Guid userId);

        // Suggestions
        Task<Result<IEnumerable<UserResponseDto>>> GetFriendSuggestionsAsync(Guid userId, int count = 10);

    }
}
