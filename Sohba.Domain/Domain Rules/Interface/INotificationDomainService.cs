using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Domain_Rules.Interface
{
    public interface INotificationDomainService
    {
        // Rule: Don't notify me if I am the one who reacted
        bool ShouldSendNotification(Guid actorId, Guid targetOwnerId);

        // Rule: Group multiple likes into one notification if they happen fast
        bool ShouldBundleNotifications(Guid targetEntityId, string notificationType, DateTime lastSentAt);

        Result CanMarkAsRead(Guid userId, Guid notificationOwnerId);
    }
}
