using System;

namespace Sohba.Application.DTOs.StoryAggregate
{
    public class StoryViewerDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime ViewedAt { get; set; }
    }
}