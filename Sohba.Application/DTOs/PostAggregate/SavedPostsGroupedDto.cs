using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.PostAggregate
{
    public class SavedPostsGroupedDto
    {
        public Guid CollectionId { get; set; }
        public string CollectionName { get; set; }
        public bool IsFavorites { get; set; }
        public List<PostResponseDto> Posts { get; set; } = new List<PostResponseDto>();
    }
}
