using Sohba.Application.DTOs.UserAggregate;

namespace Sohba.ViewModels.Friend
{
    public class FriendRequestsViewModel
    {
        public IEnumerable<FriendDto> PendingRequests { get; set; }
        public IEnumerable<FriendDto> SentRequests { get; set; }
        public int PendingCount { get; set; }
        public int SentCount { get; set; }
    }
}
