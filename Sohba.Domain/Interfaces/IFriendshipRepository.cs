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
        Task<Friend?> GetDirectAsync(Guid userId, Guid friendUserId);
        Task<IEnumerable<Friend>> GetListByUserAsync(Guid userId);

        Task<IEnumerable<Friend>> GetPendingRequestsAsync(Guid userId);
        Task<IEnumerable<Friend>> GetSentRequestsAsync(Guid userId);
        Task<int> GetPendingRequestsCountAsync(Guid userId);
        Task<IEnumerable<Friend>> GetBlockedUsersAsync(Guid userId);
        Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId);

        Task<HashSet<Guid>> GetFriendIdsSetAsync(Guid userId);
        Task<IEnumerable<Friend>> GetAllBlockedAsync();

        // Check Methods
        Task<bool> AreFriendsAsync(Guid userId, Guid friendId);
        Task<bool> IsUserBlockedAsync(Guid userId, Guid targetId);

        Task<bool> IsBlockedEitherDirectionAsync(Guid userId, Guid otherUserId);
        Task<IEnumerable<Guid>> GetBlockedByAsync(Guid userId);
        Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId);
    }


}
