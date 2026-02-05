using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.GroupAndPage
{
    public class Page
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        // Navigation Properties
        public int AdminId { get; set; }
        public virtual UserAggregate.User Admin { get; set; } // Admin is a User
    }
}
