using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.GroupAndPage
{
    public class GroupMember
    {
        public Guid Id { get; set; }
        public DateTime JoinedAt { get; set; }
        public GroupRole Role { get; set; } = GroupRole.Member; // Defaults to Member
        public bool IsBanned { get; set; } = false;

        // Navigation Properties
        public Guid UserId { get; set; }
        public virtual UserAggregate.User User { get; set; } 
        public Guid GroupId { get; set; }
        public virtual Group Group { get; set; } 
    }
}
