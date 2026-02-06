using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.UserAggregate
{
    public class Notification
    {
        public int Id { get; set; }
        public string Message { get; set; } 
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public NotificationType Type { get; set; } 

        public int ReceiverId { get; set; }
        public virtual User Receiver { get; set; }

        // Optional Becuase Some Notifications Might Not Have a Sender (e.g., System Notifications)
        public int? SenderId { get; set; }
        public virtual User? Sender { get; set; }

        // Optional TargetId to Link to the Relevant Entity (e.g., PostId, CommentId, etc.)
        public int? TargetId { get; set; }
    }
}
