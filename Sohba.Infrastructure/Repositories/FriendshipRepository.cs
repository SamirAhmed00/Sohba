using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sohba.Infrastructure.Repositories
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly AppDbContext _context;

        public FriendshipRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Add(Friend friendship)
        {
            _context.Friends.Add(friendship);
        }

        public void Update(Friend friendship)
        {
            _context.Friends.Update(friendship);
        }

        public void Delete(Friend friendship)
        {
            _context.Friends.Remove(friendship);
        }


        public async Task<IEnumerable<Friend>> GetPendingRequestsAsync(Guid userId)
        {
            return await _context.Friends
                .Include(f => f.User)
                .Include(f => f.FriendUser)
                .Where(f => f.FriendUserId == userId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task<IEnumerable<Friend>> GetSentRequestsAsync(Guid userId)
        {
            // Include both User (sender) and FriendUser (recipient) so AutoMapper
            // can resolve FriendName = src.User.Name without NullReferenceException.
            return await _context.Friends
                .AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.FriendUser)
                .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task<int> GetPendingRequestsCountAsync(Guid userId)
        {
            return await _context.Friends
                .CountAsync(f => f.FriendUserId == userId && f.Status == FriendshipStatus.Pending);
        }

        public async Task<IEnumerable<Friend>> GetBlockedUsersAsync(Guid userId)
        {
            return await _context.Friends
                .Include(f => f.User)
                .Include(f => f.FriendUser)
                .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked)
                .ToListAsync();
        }

        public async Task<Friend?> GetByUsersAsync(Guid userId, Guid friendId)
        {
            var friendship = await _context.Friends
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendUserId == friendId);

            if (friendship == null)
            {
                friendship = await _context.Friends
                    .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendUserId == userId);
            }

            return friendship;
        }

        public async Task<IEnumerable<Friend>> GetListByUserAsync(Guid userId)
        {
            return await _context.Friends
                .Include(f => f.User)
                .Include(f => f.FriendUser)
                .Where(f =>
                    (f.UserId == userId || f.FriendUserId == userId) &&
                    f.Status == FriendshipStatus.Accepted)
                .ToListAsync();
        }

        public async Task<IEnumerable<Friend>> GetAllBlockedAsync()
        {
            return await _context.Friends
                .Include(f => f.FriendUser)
                .Where(f => f.Status == FriendshipStatus.Blocked)
                .ToListAsync();
        }
        public async Task<bool> AreFriendsAsync(Guid userId, Guid friendId)
        {
            return await _context.Friends
                .AnyAsync(f =>
                    ((f.UserId == userId && f.FriendUserId == friendId) ||
                     (f.UserId == friendId && f.FriendUserId == userId)) &&
                    f.Status == FriendshipStatus.Accepted);
        }

        public async Task<bool> IsUserBlockedAsync(Guid userId, Guid targetId)
        {
            return await _context.Friends
                .AnyAsync(f =>
                    f.UserId == userId &&
                    f.FriendUserId == targetId &&
                    f.Status == FriendshipStatus.Blocked);
        }


        public async Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId)
        {
            var exists = await _context.Friends
                .AnyAsync(f => f.UserId == senderId &&
                               f.FriendUserId == receiverId &&
                               f.Status == FriendshipStatus.Pending);

            if (!exists)
            {
                exists = await _context.Friends
                    .AnyAsync(f => f.UserId == receiverId &&
                                   f.FriendUserId == senderId &&
                                   f.Status == FriendshipStatus.Pending);
            }

            return exists;
        }

        public async Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId)
        {
            var friendships = await _context.Friends
                .Where(f => (f.UserId == userId || f.FriendUserId == userId)
                            && f.Status == FriendshipStatus.Accepted)
                .ToListAsync();

            return friendships.Select(f => f.UserId == userId ? f.FriendUserId : f.UserId);
        }

        public async Task<HashSet<Guid>> GetFriendIdsSetAsync(Guid userId)
        {
            var friendIds = await _context.Friends
                .Where(f => (f.UserId == userId || f.FriendUserId == userId)
                            && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.UserId == userId ? f.FriendUserId : f.UserId)
                .ToListAsync();

            return friendIds.ToHashSet();
        }

        public async Task<bool> IsBlockedEitherDirectionAsync(Guid userId, Guid otherUserId)
        {
            return await _context.Friends
                .AnyAsync(f =>
                    ((f.UserId == userId && f.FriendUserId == otherUserId) ||
                     (f.UserId == otherUserId && f.FriendUserId == userId)) &&
                    f.Status == FriendshipStatus.Blocked);
        }

        public async Task<IEnumerable<Guid>> GetBlockedByAsync(Guid userId)
        {
            return await _context.Friends
                .Where(f => f.FriendUserId == userId && f.Status == FriendshipStatus.Blocked)
                .Select(f => f.UserId)
                .ToListAsync();
        }

    }

}
