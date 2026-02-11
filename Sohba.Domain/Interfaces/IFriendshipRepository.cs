using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IFriendshipRepository
    {
        // Commands
        void Add(Friend friendship);
        void Update(Friend friendship);
        void Delete(Friend friendship);

        // Queries
        Task<Friend?> GetByUsersAsync(Guid userId, Guid friendId);
        Task<IEnumerable<Friend>> GetListByUserAsync(Guid userId);

        Task<bool> AreFriendsAsync(Guid userId, Guid friendId);
        Task<bool> IsUserBlockedAsync(Guid userId, Guid targetId);
        Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId);
    }

}
