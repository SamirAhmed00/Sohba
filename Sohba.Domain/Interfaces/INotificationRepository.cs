using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(Guid userId, int? count = null);

        Task<int> CountUnreadAsync(Guid userId);

        Task<IEnumerable<Notification>> GetByReceiverPagedAsync(Guid userId, int page, int pageSize);

        Task<IEnumerable<Notification>> GetOldReadNotificationsAsync(DateTime cutoffDate);

        Task<int> DeleteOldNotificationsWithRetentionAsync(DateTime readCutoffDate, DateTime unreadCutoffDate);

        Task<IEnumerable<Notification>> GetByReceiverAndTargetAsync(Guid receiverId, Guid targetId);

        Task MarkAllAsReadAsync(Guid userId);
    }
}
