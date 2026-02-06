using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.UserAggregate
{
    public class Friend
    {
        public int UserId { get; set; }
        public int FriendUserId { get; set; }

        public FriendshipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        // Navigation Properties
        public virtual User User { get; set; } // The user who has the friend
        public virtual User FriendUser { get; set; } // The friend user
    }
}
