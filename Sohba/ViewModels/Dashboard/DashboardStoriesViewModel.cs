using Sohba.Application.DTOs.StoryAggregate;
using System.Collections.Generic;

namespace Sohba.ViewModels.Dashboard
{
    public class DashboardStoriesViewModel
    {
        public List<StoryResponseDto> Stories { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}