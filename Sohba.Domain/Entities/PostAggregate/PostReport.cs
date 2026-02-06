using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class PostReport
    {
        public int Id { get; set; }
        public ReportReason Reason { get; set; }
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; } = false;
        // Navigation Properties

        // The post that was reported
        public int PostId { get; set; }
        public virtual Post Post { get; set; }

        // The user who reported the post
        public int UserId { get; set; }
        public virtual UserAggregate.User User { get; set; }
    }
}
