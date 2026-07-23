using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Sohba.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Track connected users
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections[userId] = Context.ConnectionId;
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.TryRemove(userId, out _);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Method to send notification to a specific user
        public async Task SendNotificationToUser(string userId, object notification)
        {
            if (_userConnections.TryGetValue(userId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveNotification", notification);
            }
        }

        // Method to send notification to multiple users
        public async Task SendNotificationToUsers(string[] userIds, object notification)
        {
            var connectionIds = new List<string>();
            foreach (var userId in userIds)
            {
                if (_userConnections.TryGetValue(userId, out var connectionId))
                {
                    connectionIds.Add(connectionId);
                }
            }

            if (connectionIds.Any())
            {
                await Clients.Clients(connectionIds).SendAsync("ReceiveNotification", notification);
            }
        }

        // Method to broadcast to all connected users (admin use)
        public async Task BroadcastNotification(object notification)
        {
            await Clients.All.SendAsync("ReceiveNotification", notification);
        }

        // Method to join a group (optional)
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // Method to leave a group (optional)
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // Get connection count
        public int GetConnectionCount()
        {
            return _userConnections.Count;
        }
    }
}