using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Entities.PostAggregate
{
    public class SavedCollection
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public bool IsFavorites { get; set; }
        public DateTime CreatedAt { get; set; }
        public User User { get; set; }
        public ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();
    }
}
