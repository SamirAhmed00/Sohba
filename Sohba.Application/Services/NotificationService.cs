using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Events;
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
        private readonly INotificationEventHandler _eventHandler;

        protected readonly ILogger<NotificationService> _logger;
        public NotificationService(
            IUnitOfWork unitOfWork,
            INotificationDomainService domainService,
            IMapper mapper,
            INotificationEventHandler eventHandler,
            ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _domainService = domainService;
            _mapper = mapper;
            _eventHandler = eventHandler;
            _logger = logger;
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
            {
                _logger.LogWarning("Notification not sent: receiver {ReceiverId} not found", receiverId);
                return Result.Failure("Receiver not found");
            }
            // Verify sender exists if provided
            if (senderId.HasValue)
            {
                var sender = await _unitOfWork.Users.GetByIdAsync(senderId.Value);
                if (sender == null)
                {
                    _logger.LogWarning("Notification not sent: sender {SenderId} not found", senderId.Value);
                    return Result.Failure("Sender not found");
                }
            }

            // Check user preferences before sending
            if (!await ShouldSendBasedOnPreferences(receiverId, type))
            {
                _logger.LogInformation("Notification suppressed by user preferences: receiver {ReceiverId}, type {Type}", receiverId, type);
                return Result.Success();
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
            _logger.LogInformation("Notification created: {NotificationId}, receiver {ReceiverId}, type {Type}", notification.Id, receiverId, type);

            // Send real-time notification via SignalR
            try
            {
                var notificationEvent = new NotificationEvent
                {
                    ReceiverId = receiverId,
                    Message = message,
                    Type = type,
                    SenderId = senderId,
                    TargetId = targetId,
                    Notification = notification
                };

                await _eventHandler.HandleAsync(notificationEvent);
                _logger.LogInformation("Notification {NotificationId} dispatched via SignalR to user {ReceiverId}", notification.Id, receiverId);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the operation
                _logger.LogError(ex, "NotificationService event handler failed for receiver {ReceiverId}, type {Type}", receiverId, type);
                _logger.LogError(ex, "SignalR dispatch failed for notification {NotificationId} to user {ReceiverId}", notification.Id, receiverId);
            }

            return Result.Success();
        }



        // Check user preferences
        private async Task<bool> ShouldSendBasedOnPreferences(Guid userId, NotificationType type)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return false;

            // If user has disabled notifications, don't send
            if (!user.EmailNotifications && !user.PushNotifications)
                return false;

            // Check notification type-specific preferences
            var notificationType = type switch
            {
                NotificationType.PostLike or NotificationType.PostComment => "social",
                NotificationType.FriendRequest or NotificationType.GroupInvitation => "social",
                NotificationType.SystemAlert => "system",
                _ => "system"
            };

            // If it's a social notification and user has disabled email notifications
            // but push is enabled, we still send via SignalR
            if (notificationType == "social" && !user.PushNotifications)
                return false;

            return true;
        }

        public async Task<Result<IEnumerable<Notification>>> GetUserNotificationsAsync(
            Guid userId,
            int page = 1,
            int pageSize = 20)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return Result<IEnumerable<Notification>>.Failure("User not found");

            var result = await _unitOfWork.Notifications.GetByReceiverPagedAsync(userId, page, pageSize);

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

            var oldNotifications = await _unitOfWork.Notifications.GetOldReadNotificationsAsync(cutoffDate);

            foreach (var notification in oldNotifications)
            {
                _unitOfWork.Notifications.Delete(notification);
            }

            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }
    }
}