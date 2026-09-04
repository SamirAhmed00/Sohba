using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class GroupResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Rules { get; set; }
        public string? ImageUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public int MembersCount { get; set; }
        public bool IsCurrentUserMember { get; set; }
        public bool IsPrivate { get; set; }
        public Guid AdminId { get; set; }
        public DateTime CreatedAt { get; set; }
        public GroupJoinRequestStatus? UserJoinRequestStatus { get; set; }
    }
}
