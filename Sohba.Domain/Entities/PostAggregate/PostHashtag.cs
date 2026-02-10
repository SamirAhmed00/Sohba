using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class PostHashtag
    {
        public Guid Id { get; set; }

        // Navigation Properties
        public Guid PostId { get; set; }
        public virtual Post Post { get; set; } 
        public Guid HashtagId { get; set; }
        public virtual Hashtag Hashtag { get; set; }
    }
}
