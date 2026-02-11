using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class GroupMemberDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; } // Converted from Enum
        public DateTime JoinedAt { get; set; }
    }
}
