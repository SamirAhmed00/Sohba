using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

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
                .Include(f => f.FriendUser)
                .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked)
                .ToListAsync();
        }

        //public async Task<Friend?> GetByUsersAsync(Guid userId, Guid friendId)
        //{
        //    return await _context.Friends
        //        .FirstOrDefaultAsync(f =>
        //            f.UserId == userId &&
        //            f.FriendUserId == friendId);
        //}

        public async Task<Friend?> GetByUsersAsync(Guid userId, Guid friendId)
        {
            Console.WriteLine($"🔍 GetByUsersAsync - userId: {userId}, friendId: {friendId}");

            var friendship = await _context.Friends
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendUserId == friendId);

            Console.WriteLine($"📊 Friendship found: {friendship != null}");

            if (friendship == null)
            {
                var reversed = await _context.Friends
                    .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendUserId == userId);
                Console.WriteLine($"📊 Reversed found: {reversed != null}");
            }

            return friendship;
        }

        public async Task<IEnumerable<Friend>> GetListByUserAsync(Guid userId)
        {
            return await _context.Friends
                .Where(f =>
                    (f.UserId == userId || f.FriendUserId == userId) &&
                    f.Status == FriendshipStatus.Accepted)
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

        //public async Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId)
        //{
        //    return await _context.Friends
        //        .AnyAsync(f =>
        //            f.UserId == senderId &&
        //            f.FriendUserId == receiverId &&
        //            f.Status == FriendshipStatus.Pending);
        //}

        public async Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId)
        {
            Console.WriteLine($"🔍 HasPendingRequest - senderId: {senderId}, receiverId: {receiverId}");

            var exists = await _context.Friends
                .AnyAsync(f => f.UserId == senderId &&
                               f.FriendUserId == receiverId &&
                               f.Status == FriendshipStatus.Pending);

            Console.WriteLine($"📊 Pending request exists: {exists}");

            // لو مش موجود، شوف لو مقلوبين
            if (!exists)
            {
                var reversedExists = await _context.Friends
                    .AnyAsync(f => f.UserId == receiverId &&
                                   f.FriendUserId == senderId &&
                                   f.Status == FriendshipStatus.Pending);
                Console.WriteLine($"📊 Reversed check: {reversedExists}");
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
    }

}
