using Sohba.Application.DTOs.GroupAndPageAggregate;

namespace Sohba.ViewModels.Dashboard
{
    public class DashboardGroupsViewModel
    {
        public List<GroupResponseDto> Groups { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SearchTerm { get; set; } = string.Empty;
    }
}
