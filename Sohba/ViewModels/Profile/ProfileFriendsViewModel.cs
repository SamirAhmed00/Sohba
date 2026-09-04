using Sohba.Application.DTOs.UserAggregate;
using System.Collections.Generic;

namespace Sohba.ViewModels.Profile
{
    public class ProfileFriendsViewModel
    {
        public UserResponseDto Profile { get; set; } = null!;
        public IEnumerable<FriendDto> Friends { get; set; } = new List<FriendDto>();
        public bool IsOwnProfile { get; set; }
        public bool CanViewFriends { get; set; }
    }
}
