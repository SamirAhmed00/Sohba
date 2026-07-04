using AutoMapper;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;

namespace Sohba.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationDomainService _domainService;
        private readonly IMapper _mapper;

        public NotificationService(
            IUnitOfWork unitOfWork,
            INotificationDomainService domainService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _domainService = domainService;
            _mapper = mapper;
        }

        public async Task<Result> CreateNotificationAsync(
            Guid receiverId,
            string message,
            NotificationType type,
            Guid? senderId = null,
            Guid? targetId = null)
        {
            // Rule: Don't send notification to yourself
            if (!_domainService.ShouldSendNotification(senderId ?? Guid.Empty, receiverId))
                return Result.Success();

            // Verify receiver exists
            var receiver = await _unitOfWork.Users.GetByIdAsync(receiverId);
            if (receiver == null)
                return Result.Failure("Receiver not found");

            // Verify sender exists if provided
            if (senderId.HasValue)
            {
                var sender = await _unitOfWork.Users.GetByIdAsync(senderId.Value);
                if (sender == null)
                    return Result.Failure("Sender not found");
            }

            // Create notification
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                ReceiverId = receiverId,
                SenderId = senderId,
                Message = message,
                Type = type,
                TargetId = targetId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Notifications.Add(notification);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result<IEnumerable<Notification>>> GetUserNotificationsAsync(
            Guid userId,
            int page = 1,
            int pageSize = 20)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return Result<IEnumerable<Notification>>.Failure("User not found");

            var notifications = await _unitOfWork.Notifications.GetAllAsync();

            var result = notifications
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Result<IEnumerable<Notification>>.Success(result);
        }

        public async Task<Result<IEnumerable<Notification>>> GetUnreadNotificationsAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return Result<IEnumerable<Notification>>.Failure("User not found");

            var notifications = await _unitOfWork.Notifications.GetUnreadNotificationsAsync(userId);
            return Result<IEnumerable<Notification>>.Success(notifications);
        }

        public async Task<Result<int>> GetUnreadCountAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return Result<int>.Failure("User not found");

            var notifications = await _unitOfWork.Notifications.GetUnreadNotificationsAsync(userId);
            return Result<int>.Success(notifications.Count());
        }

        public async Task<Result> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (notification == null)
                return Result.Failure("Notification not found");

            // Rule: Only the notification owner can update it
            var validation = _domainService.CanMarkAsRead(userId, notification.ReceiverId);
            if (!validation.IsSuccess)
                return validation;

            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> MarkAllAsReadAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return Result.Failure("User not found");

            var notifications = await _unitOfWork.Notifications.GetUnreadNotificationsAsync(userId);

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                _unitOfWork.Notifications.Update(notification);
            }

            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<Result> DeleteNotificationAsync(Guid notificationId, Guid userId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (notification == null)
                return Result.Failure("Notification not found");

            if (notification.ReceiverId != userId)
                return Result.Failure("You can only delete your own notifications");

            _unitOfWork.Notifications.Delete(notification);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> DeleteOldNotificationsAsync(int daysOld = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var allNotifications = await _unitOfWork.Notifications.GetAllAsync();

            var oldNotifications = allNotifications
                .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                .ToList();

            foreach (var notification in oldNotifications)
            {
                _unitOfWork.Notifications.Delete(notification);
            }

            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }
    }
}