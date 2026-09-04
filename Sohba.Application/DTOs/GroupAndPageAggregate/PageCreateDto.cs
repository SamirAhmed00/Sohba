using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Sohba.Application.DTOs.GroupAndPageAggregate
{
    public class PageCreateDto
    {
        [StringLength(100, MinimumLength = 3)]
        public string Name { get; set; }

        [StringLength(2000)]
        public string Description { get; set; }

        public string? BackgroundImageUrl { get; set; }

        public string? ImageUrl { get; set; }
        public string? Rules { get; set; }
        public bool IsPrivate { get; set; }
        public Guid AdminId { get; set; }
    }
}
