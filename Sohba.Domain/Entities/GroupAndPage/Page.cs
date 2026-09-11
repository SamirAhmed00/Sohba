using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.GroupAndPage
{
    public class Page
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }
        public string? Rules { get; set; }
        public bool IsPrivate { get; set; } = false;
        public DateTime CreatedAt { get; set; }

        // Soft Delete & Deletion Tracking
        public bool IsDeleted { get; set; } = false;
        public string? DeletionReason { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedByUserId { get; set; }

        // Navigation Properties
        public Guid AdminId { get; set; }
        public virtual UserAggregate.User Admin { get; set; } = null!;
        public virtual ICollection<PageFollower> Followers { get; set; } = new List<PageFollower>();
        public virtual ICollection<PageFollowRequest> FollowRequests { get; set; } = new List<PageFollowRequest>();
    }
}
