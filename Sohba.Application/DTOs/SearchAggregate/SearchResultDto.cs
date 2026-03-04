using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.DTOs.SearchAggregate
{
    public class SearchResultDto
    {
        public List<PostSearchResultDto> Posts { get; set; } = new();
        public List<UserSearchResultDto> Users { get; set; } = new();
        public List<GroupSearchResultDto> Groups { get; set; } = new();
        public List<PageSearchResultDto> Pages { get; set; } = new();
        public int TotalCount => Posts.Count + Users.Count + Groups.Count + Pages.Count;
    }
}
