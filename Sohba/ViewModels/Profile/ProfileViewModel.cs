using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.UserAggregate;

namespace Sohba.ViewModels.Profile
{
    public class ProfileViewModel
    {
        public UserResponseDto Profile { get; set; }
        public IEnumerable<FriendDto> Friends { get; set; }
        public IEnumerable<PostResponseDto> Posts { get; set; }
        public bool IsOwnProfile { get; set; }

        public string FriendshipStatus { get; set; } = "none"; // "none" | "pending" | "accepted"



        public bool CanViewFriends { get; set; } = true;
        public bool IsPrivate { get; set; }
        public bool IsBlocked { get; set; }

    }
}
