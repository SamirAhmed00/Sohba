using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.GroupAndPage
{
    public class GroupMember
    {
        public int Id { get; set; }
        public DateTime JoinedAt { get; set; }
        public GroupRole Role { get; set; } = GroupRole.Member; // Defaults to Member
        // Navigation Properties
        public int UserId { get; set; }
        public virtual UserAggregate.User User { get; set; } 
        public int GroupId { get; set; }
        public virtual Group Group { get; set; } 
    }
}
