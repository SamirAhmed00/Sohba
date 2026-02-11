using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class PostResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public string AuthorName { get; set; }
        public int CommentsCount { get; set; }
        public int ReactionsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
