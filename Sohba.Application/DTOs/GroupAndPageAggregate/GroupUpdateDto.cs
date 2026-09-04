using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class GroupUpdateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Rules { get; set; }
        public string? ImageUrl { get; set; }
        public string? BackgroundImageUrl { get; set; }
        public bool IsPrivate { get; set; }
    }
}
