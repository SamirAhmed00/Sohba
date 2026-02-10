using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Logic
{
    public class NotificationDomainService : INotificationDomainService
    {
        public bool ShouldSendNotification(Guid actorId, Guid targetOwnerId)
        {
            // Rule: Do not notify users about their own actions
            return actorId != targetOwnerId;
        }

        public bool ShouldBundleNotifications(Guid targetEntityId, string notificationType, DateTime lastSentAt)
        {
            // Rule: Bundle if the last similar notification was sent recently (e.g., within 15 minutes)
            // This prevents spamming "User X liked your post", "User Y liked your post"
            double bundleWindowMinutes = 15;
            return DateTime.UtcNow < lastSentAt.AddMinutes(bundleWindowMinutes);
        }

        public Result CanMarkAsRead(Guid userId, Guid notificationOwnerId)
        {
            if (userId != notificationOwnerId)
                return Result.Failure("You can only manage your own notifications.");

            return Result.Success();
        }
    }
}
