using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(Guid userId);

        Task<IEnumerable<Notification>> GetByReceiverPagedAsync(Guid userId, int page, int pageSize);

        Task<IEnumerable<Notification>> GetOldReadNotificationsAsync(DateTime cutoffDate);
    }
}
