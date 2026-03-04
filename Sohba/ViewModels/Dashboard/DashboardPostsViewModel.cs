using Sohba.Application.DTOs.PostAggregate;

namespace Sohba.ViewModels.Dashboard
{
    public class DashboardPostsViewModel
    {
        public List<PostResponseDto> Posts { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 20;
        public string SearchTerm { get; set; }
        public string SourceFilter { get; set; } = "all"; // all, user, group, page
    }
}
