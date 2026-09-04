using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class GroupMemberDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public GroupRole Role { get; set; }
        public bool IsOwner { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
