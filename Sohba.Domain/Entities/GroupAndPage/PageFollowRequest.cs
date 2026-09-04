using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.GroupAndPage
{
    public class PageFollowRequest
    {
        public Guid Id { get; set; }

        public Guid PageId { get; set; }
        public virtual Page Page { get; set; } = null!;

        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string Message { get; set; } = string.Empty;
        public PageFollowRequestStatus Status { get; set; } = PageFollowRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        public Guid? ReviewedByUserId { get; set; }
        public virtual User? ReviewedByUser { get; set; }
    }
}
