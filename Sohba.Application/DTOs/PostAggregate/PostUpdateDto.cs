using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class PostUpdateDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public List<string> ImageUrls { get; set; } = new();

        public Sohba.Domain.Enums.PostPrivacy Privacy { get; set; }
    }
}
