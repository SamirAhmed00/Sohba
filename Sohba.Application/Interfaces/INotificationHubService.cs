using System;
using System.Threading.Tasks;

namespace Sohba.Application.Interfaces
{
    public interface INotificationHubService
    {
        Task SendNotificationToUserAsync(string userId, object notification);
        Task SendNotificationToUsersAsync(string[] userIds, object notification);
        Task BroadcastNotificationAsync(object notification);
    }
}