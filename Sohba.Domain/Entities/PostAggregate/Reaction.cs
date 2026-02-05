using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class Reaction
    {
        public int Id { get; set; }
        public string Type { get; set; } // it Will Be an Enum 
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public int UserId { get; set; }
        public virtual User User { get; set; } 
        public int PostId { get; set; }
        public virtual Post Post { get; set; } 
    }
}
