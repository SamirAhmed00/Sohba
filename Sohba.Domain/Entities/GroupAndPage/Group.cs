using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.GroupAndPage
{
    public class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Rules { get; set; }
        public string? ImageUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Soft Delete & Moderation Tracking
        public bool IsDeleted { get; set; } = false;
        public string? DeletionReason { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedByUserId { get; set; }

        // Navigation Properties
        public Guid AdminId { get; set; } // Canonical Group Owner
        public virtual UserAggregate.User Admin { get; set; } = null!;
        public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
        public virtual ICollection<GroupJoinRequest> JoinRequests { get; set; } = new List<GroupJoinRequest>();
    }
}
