using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.StoryAggregate
{
    public class StoryResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }
        public string UserName { get; set; }
        public string? UserProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int ViewersCount { get; set; }
        public bool HasUserViewed { get; set; }
        public string Privacy { get; set; }
    }

}
