using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.StoryAggregate
{
    public class StoryViewer
    {
        public Guid Id { get; set; }
        public Guid StoryId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ViewedAt { get; set; }

        // Navigation Properties
        public virtual Story Story { get; set; }
        public virtual User User { get; set; }
    }
}
