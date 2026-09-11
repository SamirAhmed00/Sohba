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
                var responseDto = new Sohba.Application.DTOs.UserAggregate.NotificationResponseDto
                {
                    Id = @event.Notification.Id,
                    Message = @event.Message,
                    NotificationType = @event.Type.ToString(),
                    SenderId = @event.SenderId,
                    TargetId = @event.TargetId,
                    CreatedAt = @event.Notification.CreatedAt,
                    IsRead = @event.Notification.IsRead
                };

                var notificationDto = new
                {
                    id = responseDto.Id,
                    message = responseDto.Message,
                    notificationType = responseDto.NotificationType,
                    senderId = responseDto.SenderId,
                    targetId = responseDto.TargetId,
                    targetUrl = responseDto.TargetUrl,
                    createdAt = responseDto.CreatedAt,
                    isRead = responseDto.IsRead
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