using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.StoryAggregate
{
    public class Story
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public string? MediaUrl { get; set; } 
        public string? MediaType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        public StoryPrivacy Privacy { get; set; } = StoryPrivacy.Public; 
        public virtual ICollection<StoryViewer> Viewers { get; set; } = new List<StoryViewer>(); 

        public bool IsDeleted { get; set; }
        // Navigation Properties
        public Guid UserId { get; set; }
        public virtual User User { get; set; } 
    }
}
