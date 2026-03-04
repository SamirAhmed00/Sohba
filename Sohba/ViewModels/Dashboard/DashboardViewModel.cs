using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.UserAggregate;

namespace Sohba.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        // Statistics
        public int TotalUsers { get; set; }
        public int TotalPosts { get; set; }
        public int TotalGroups { get; set; }
        public int TotalPages { get; set; }
        public int PendingReports { get; set; }
        public int NewUsersToday { get; set; }
        public int NewPostsToday { get; set; }

        // Charts Data
        public List<int> UsersLast7Days { get; set; } = new();
        public List<string> Last7DaysLabels { get; set; } = new();

        // Recent Activities
        public List<UserResponseDto> RecentUsers { get; set; } = new();
        public List<PostResponseDto> RecentPosts { get; set; } = new();
        public List<PostReportResponseDto> RecentReports { get; set; } = new();
    }
}
