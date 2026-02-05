using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class PostHashtag
    {
        public int Id { get; set; }

        // Navigation Properties
        public int PostId { get; set; }
        public virtual Post Post { get; set; } 
        public int HashtagId { get; set; }
        public virtual Hashtag Hashtag { get; set; }
    }
}
