using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        // Optional Image URL
        public string ?ImageUrl { get; set; }

        // Navigation Properties
        public int UserId { get; set; }
        public virtual User User { get; set; } 
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>(); 
        public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
        public virtual ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();

    }
}
