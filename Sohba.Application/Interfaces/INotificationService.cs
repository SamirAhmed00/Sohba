using Sohba.Domain.Common;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;

namespace Sohba.Application.Interfaces
{
    public interface INotificationService
    {
        // Create notification
        Task<Result> CreateNotificationAsync(
            Guid receiverId,
            string message,
            NotificationType type,
            Guid? senderId = null,
            Guid? targetId = null);

        // Get notifications
        Task<Result<IEnumerable<Notification>>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<Result<IEnumerable<Notification>>> GetUnreadNotificationsAsync(Guid userId);
        Task<Result<int>> GetUnreadCountAsync(Guid userId);

        // Update status
        Task<Result> MarkAsReadAsync(Guid notificationId, Guid userId);
        Task<Result> MarkAllAsReadAsync(Guid userId);

        // Delete
        Task<Result> DeleteNotificationAsync(Guid notificationId, Guid userId);
        Task<Result> DeleteOldNotificationsAsync(int daysOld = 30);
    }
}