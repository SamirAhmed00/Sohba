using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class PostCreateDto
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPrivate { get; set; }

        public List<string> Hashtags { get; set; } = new List<string>();
        public Guid UserId { get; set; }

        public PostSourceType SourceType { get; set; } = PostSourceType.User;
        public Guid? SourceId { get; set; }
    }
}
