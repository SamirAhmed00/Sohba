using Sohba.Application.DTOs.UserAggregate;

namespace Sohba.ViewModels.Dashboard
{
    public class DashboardUsersViewModel
    {
        public List<UserResponseDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 20;
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; } = "all"; // all, active, blocked
    }
}
