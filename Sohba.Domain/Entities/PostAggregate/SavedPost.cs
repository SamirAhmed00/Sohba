using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class SavedPost
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; } 
        public virtual UserAggregate.User User { get; set; }
        public Guid PostId { get; set; }
        public virtual Post Post { get; set; }
        public Guid? CollectionId { get; set; }        // null = legacy/default
        public virtual SavedCollection Collection { get; set; }
        public DateTime SavedAt { get; set; }
        public SavedTag Tag { get; set; }              // kept for backwards compatibility
        public string? UserTag { get; set; }           // kept for backwards compatibility
    }
}
