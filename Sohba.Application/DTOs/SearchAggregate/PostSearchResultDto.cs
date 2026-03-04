using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.SearchAggregate
{
    public class PostSearchResultDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public string AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Url => $"/Posts/Details/{Id}";
    }
}
