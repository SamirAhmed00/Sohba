using Sohba.Application.DTOs.UserAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface ISocialService
    {
        Task<Result> SendFriendRequestAsync(Guid senderId, Guid receiverId);
        Task<Result> RespondToRequestAsync(Guid userId, Guid requesterId, bool accept);
        Task<Result<IEnumerable<FriendDto>>>GetFriendsListAsync(Guid userId);
        Task<Result<IEnumerable<NotificationResponseDto>>> GetMyNotificationsAsync(Guid userId);
    }
}
