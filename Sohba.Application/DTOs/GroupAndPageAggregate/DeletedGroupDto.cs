using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class DeletedGroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string DeletionReason { get; set; } = string.Empty;
        public Guid AdminId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public Guid? DeletedByUserId { get; set; }
        public string DeletedByName { get; set; } = string.Empty;
    }

}
