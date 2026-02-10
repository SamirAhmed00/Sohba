using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.GroupAndPage
{
    public class PageFollower
    {
        public Guid Id { get; set; }
        public DateTime FollowedAt { get; set; }
        // Navigation Properties
        public Guid UserId { get; set; }
        public virtual UserAggregate.User User { get; set; } 
        public Guid PageId { get; set; }
        public virtual Page Page { get; set; }
    }
}
