using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{

    public class SavedCollectionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public bool IsFavorites { get; set; }
        public int PostCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
