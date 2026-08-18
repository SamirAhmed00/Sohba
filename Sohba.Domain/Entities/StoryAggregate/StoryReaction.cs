using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using System;

namespace Sohba.Domain.Entities.StoryAggregate
{
    public class StoryReaction
    {
        public Guid Id { get; set; }
        public ReactionType Type { get; set; }
        public DateTime CreatedAt { get; set; }

        public Guid UserId { get; set; }
        public virtual User User { get; set; }

        public Guid StoryId { get; set; }
        public virtual Story Story { get; set; }
    }
}