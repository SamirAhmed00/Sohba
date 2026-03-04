using Sohba.Application.DTOs.UserAggregate;

namespace Sohba.ViewModels.Friend
{
    public class FriendSuggestionViewModel
    {
        public UserResponseDto User { get; set; }
        public int MutualFriendsCount { get; set; }
    }
}
