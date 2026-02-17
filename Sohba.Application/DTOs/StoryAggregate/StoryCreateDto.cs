using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Sohba.Application.DTOs.StoryAggregate
{
    public class StoryCreateDto
    {
        public string Content { get; set; }
        public IFormFile? MediaFile { get; set; }  
        public string? MediaUrl { get; set; }  
        public string Privacy { get; set; } = "Public";
    }
}
