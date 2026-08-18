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

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(Guid userId)
        {
            return await _context.Set<Notification>()
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetByReceiverPagedAsync(Guid userId, int page, int pageSize)
        {
            return await _context.Set<Notification>()
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetOldReadNotificationsAsync(DateTime cutoffDate)
        {
            return await _context.Set<Notification>()
                .Where(n => n.CreatedAt<cutoffDate && n.IsRead)
                .ToListAsync();
        }


        public async Task<IEnumerable<Notification>> GetByReceiverAndTargetAsync(Guid receiverId, Guid targetId)
        {
            return await _context.Set<Notification>()
                .Where(n => n.ReceiverId == receiverId && n.TargetId == targetId && !n.IsRead)
                .ToListAsync();
        }
    }
}
