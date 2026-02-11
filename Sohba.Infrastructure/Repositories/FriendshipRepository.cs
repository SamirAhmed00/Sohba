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


        public async Task<Friend?> GetByUsersAsync(Guid userId, Guid friendId)
        {
            return await _context.Friends
                .FirstOrDefaultAsync(f =>
                    f.UserId == userId &&
                    f.FriendUserId == friendId);
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

        public async Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId)
        {
            return await _context.Friends
                .AnyAsync(f =>
                    f.UserId == senderId &&
                    f.FriendUserId == receiverId &&
                    f.Status == FriendshipStatus.Pending);
        }
    }

}
