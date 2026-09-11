using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.UserAggregate
{
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsPrivateAccount { get; set; }
        public bool IsActive { get; set; }

        public int FriendsCount { get; set; }
        public int PostsCount { get; set; }

        public UserRole Role { get; set; } = UserRole.User;
        public Guid? PromotedByAdminUserId { get; set; }
        public string? PromotedByAdminName { get; set; }
        public bool CanCurrentAdminManageRole { get; set; }
    }
}
