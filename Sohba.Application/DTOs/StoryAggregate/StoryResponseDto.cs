using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.StoryAggregate
{
    public class StoryResponseDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
