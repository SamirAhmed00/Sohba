using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.GroupAndPage
{
    public class Page
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        // Navigation Properties
        public Guid AdminId { get; set; }
        public virtual UserAggregate.User Admin { get; set; } // Admin is a User
    }
}
