using Sohba.Application.Events;
using System.Threading.Tasks;

namespace Sohba.Application.Interfaces
{
    /// <summary>
    /// Handles NotificationEvent for real-time delivery (SignalR)
    /// Implementation lives in the Web layer to access SignalR HubContext
    /// </summary>
    public interface INotificationEventHandler
    {
        /// <summary>
        /// Handle the notification event (send via SignalR)
        /// </summary>
        /// <param name="event">The notification event</param>
        Task HandleAsync(NotificationEvent @event);
    }
}