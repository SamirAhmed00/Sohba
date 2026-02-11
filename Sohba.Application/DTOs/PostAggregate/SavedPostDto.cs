using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class SavedPostDto
    {
        public Guid PostId { get; set; }
        public string PostTitle { get; set; }
        public string Tag { get; set; } // Enum: e.g., "Work", "Personal"
        public string? UserTag { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
