using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class Post
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPrivate { get; set; }

        // i think it will be nullable ?
        public DateTime? UpdatedAt { get; set; }

        // Optional Image URL
        public string ?ImageUrl { get; set; }

        // Navigation Properties
        public Guid UserId { get; set; }
        public virtual User User { get; set; } 
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>(); 
        public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
        public virtual ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
        public virtual ICollection<PostReport> Reports { get; set; } = new List<PostReport>();
        public virtual ICollection<SavedPost> SavedByUsers { get; set; } = new List<SavedPost>();
    }
}
