using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(Guid userId, int? count = null)
        {
            var query = _context.Set<Notification>()
                .Include(n => n.Sender)
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt);

            if (count.HasValue && count.Value > 0)
            {
                return await query.Take(count.Value).ToListAsync();
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetByReceiverPagedAsync(Guid userId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            return await _context.Set<Notification>()
                .Include(n => n.Sender)
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetOldReadNotificationsAsync(DateTime cutoffDate)
        {
            return await _context.Set<Notification>()
                .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                .ToListAsync();
        }

        public async Task<int> DeleteOldNotificationsWithRetentionAsync(DateTime readCutoffDate, DateTime unreadCutoffDate)
        {
            return await _context.Set<Notification>()
                .Where(n => (n.CreatedAt < readCutoffDate && n.IsRead) ||
                            (n.CreatedAt < unreadCutoffDate && !n.IsRead))
                .ExecuteDeleteAsync();
        }


        public async Task<IEnumerable<Notification>> GetByReceiverAndTargetAsync(Guid receiverId, Guid targetId)
        {
            return await _context.Set<Notification>()
                .Where(n => n.ReceiverId == receiverId && n.TargetId == targetId && !n.IsRead)
                .ToListAsync();
        }

        public async Task<int> CountUnreadAsync(Guid userId)
        {
            return await _context.Set<Notification>()
                .CountAsync(n => n.ReceiverId == userId && !n.IsRead);
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await _context.Set<Notification>()
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }
    }
}
