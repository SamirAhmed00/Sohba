using Microsoft.AspNetCore.SignalR;
using Sohba.Application.Events;
using Sohba.Application.Interfaces;
using Sohba.Controllers;
using Sohba.Hubs;
using System.Threading.Tasks;

namespace Sohba.Handlers
{
    /// <summary>
    /// Handles NotificationEvent by sending real-time notifications via SignalR
    /// Lives in Web layer because it needs access to IHubContext<NotificationHub>
    /// </summary>
    public class NotificationEventHandler : INotificationEventHandler
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        protected readonly ILogger<NotificationEventHandler> _logger;
        public NotificationEventHandler(IHubContext<NotificationHub> hubContext, ILogger<NotificationEventHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleAsync(NotificationEvent @event)
        {
            if (@event == null || @event.Notification == null)
                return;

            try
            {
                // Build the notification DTO for the client
                var notificationDto = new
                {
                    id = @event.Notification.Id,
                    message = @event.Message,
                    notificationType = @event.Type.ToString(),
                    senderId = @event.SenderId,
                    targetId = @event.TargetId,
                    createdAt = @event.Notification.CreatedAt,
                    isRead = @event.Notification.IsRead,
                    // Add sender name if available (we'll enrich it on the client side)
                    // Or we can fetch it here from a service
                };

                // Send to the specific user
                await _hubContext.Clients.User(@event.ReceiverId.ToString())
                    .SendAsync("ReceiveNotification", notificationDto);

            }
            catch (System.Exception ex)
            {
                // Log error but don't fail the operation
                _logger.LogError(ex, "SignalR failed to send notification to user {ReceiverId}", @event.ReceiverId);
            }
        }
    }
}