using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class FriendshipRepository : GenericRepository<Friend>, IFriendshipRepository
    {
        public FriendshipRepository(AppDbContext context) : base(context) { }

        public bool AreFriends(Guid userId, Guid friendId)
        {
            // Check if a friendship exists in either direction with 'Accepted' status
            return _context.Set<Friend>().Any(f =>
                ((f.UserId == userId && f.FriendUserId == friendId) ||
                 (f.UserId == friendId && f.FriendUserId == userId)) &&
                f.Status == FriendshipStatus.Accepted);
        }

        public bool IsUserBlocked(Guid userId, Guid targetId)
        {
            // Check if userId has blocked targetId
            // Assuming there is a separate Block entity or a Status in Friendship
            // Here assuming a 'Block' list or status
            return _context.Set<Friend>().Any(f =>
                f.UserId == userId &&
                f.FriendUserId == targetId &&
                f.Status == FriendshipStatus.Blocked);
        }

        public string GetFriendshipStatus(Guid userId, Guid otherId)
        {
            var friendship = _context.Set<Friend>().FirstOrDefault(f =>
                (f.UserId == userId && f.FriendUserId == otherId) ||
                (f.FriendUserId == otherId && f.UserId == userId));

            if (friendship == null) return "None";

            return friendship.Status.ToString(); // Returns "Accepted", "Pending", "Blocked"
        }

        public bool HasPendingRequest(Guid senderId, Guid receiverId)
        {
            // Check specifically if sender sent a request to receiver that is still pending
            return _context.Set<Friend>().Any(f =>
                f.UserId == senderId &&
                f.FriendUserId == receiverId &&
                f.Status == FriendshipStatus.Pending);
        }
    }
}
