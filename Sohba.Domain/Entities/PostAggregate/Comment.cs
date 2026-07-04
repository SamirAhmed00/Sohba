using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class Comment
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime DateUpdated { get; set; }

        public Guid? ParentCommentId { get; set; }

        // Navigation Properties
        public Guid UserId { get; set; }
        public virtual User User { get; set; } 
        public Guid PostId { get; set; }
        public virtual Post Post { get; set; }

        public virtual Comment ParentComment { get; set; }
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }

}
