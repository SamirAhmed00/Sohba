using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public int UserId { get; set; }
        public virtual User User { get; set; } // one to one
        public int PostId { get; set; }
        public virtual Post Post { get; set; } // one to one 
    }
}
