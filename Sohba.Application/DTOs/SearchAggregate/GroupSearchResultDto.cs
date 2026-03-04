using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.SearchAggregate
{
    public class GroupSearchResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public int MembersCount { get; set; }
        public string Url => $"/Groups/Details/{Id}";
    }
}
