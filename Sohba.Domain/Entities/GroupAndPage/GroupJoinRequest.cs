using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.GroupAndPage
{
    public class GroupJoinRequest
    {
        public Guid Id { get; set; }

        public Guid GroupId { get; set; }
        public virtual Group Group { get; set; } = null!;

        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string? Reason { get; set; }
        public GroupJoinRequestStatus Status { get; set; } = GroupJoinRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        public Guid? ReviewedByUserId { get; set; }
        public virtual User? ReviewedByUser { get; set; }
    }

}
