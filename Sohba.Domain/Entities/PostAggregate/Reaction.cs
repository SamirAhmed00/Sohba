using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class Reaction
    {
        public Guid Id { get; set; }
        public ReactionType Type { get; set; } 
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public Guid UserId { get; set; }
        public virtual User User { get; set; } 
        public Guid PostId { get; set; }
        public virtual Post Post { get; set; } 
    }
}
