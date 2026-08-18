using System;

namespace Sohba.Application.DTOs.StoryAggregate
{
    public class StoryReactionRequestDto
    {
        public Guid StoryId { get; set; }
        public string ReactionType { get; set; }
    }
}