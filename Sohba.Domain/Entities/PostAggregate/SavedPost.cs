using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class SavedPost
    {
        public Guid UserId { get; set; }
        public virtual UserAggregate.User User { get; set; }
        public Guid PostId { get; set; }
        public virtual Post Post { get; set; }
        public DateTime SavedAt { get; set; }
        public SavedTag Tag { get; set; } 
        public string? UserTag { get; set; } // Optional user-defined tag for additional categorization
    }
}
