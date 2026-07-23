using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using System;

namespace Sohba.Application.Events
{
    /// <summary>
    /// Event raised when a notification is created.
    /// Handled by NotificationEventHandler in the Web layer for SignalR delivery.
    /// This maintains Clean Architecture by keeping Application layer decoupled from Web layer.
    /// </summary>
    public class NotificationEvent
    {
        /// <summary>
        /// ID of the user receiving the notification
        /// </summary>
        public Guid ReceiverId { get; set; }

        /// <summary>
        /// Notification message text
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Type of notification (PostLike, FriendRequest, etc.)
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// ID of the user who triggered the notification (optional)
        /// </summary>
        public Guid? SenderId { get; set; }

        /// <summary>
        /// ID of the target entity (PostId, GroupId, etc.)
        /// </summary>
        public Guid? TargetId { get; set; }

        /// <summary>
        /// The full Notification entity
        /// </summary>
        public Notification? Notification { get; set; }
    }
}