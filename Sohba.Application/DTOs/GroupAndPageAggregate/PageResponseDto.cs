using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class PageResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public string AdminName { get; set; }
        public Guid AdminId { get; set; }  
        public bool IsFollowing { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
