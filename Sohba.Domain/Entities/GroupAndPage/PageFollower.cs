using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.GroupAndPage
{
    public class PageFollower
    {
        public int Id { get; set; }
        public DateTime FollowedAt { get; set; }
        // Navigation Properties
        public int UserId { get; set; }
        public virtual UserAggregate.User User { get; set; } 
        public int PageId { get; set; }
        public virtual Page Page { get; set; }
    }
}
