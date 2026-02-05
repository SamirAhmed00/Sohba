using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class Hashtag
    {
        public int Id { get; set; }
        public string Tag { get; set; }
        public string Location { get; set; } 

        // Navigation Properties
        public virtual ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
    }
}
