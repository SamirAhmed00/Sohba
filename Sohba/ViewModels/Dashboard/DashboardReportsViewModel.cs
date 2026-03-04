using Sohba.Application.DTOs.PostAggregate;

namespace Sohba.ViewModels.Dashboard
{
    public class DashboardReportsViewModel
    {
        public List<PostReportResponseDto> Reports { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 20;
        public string StatusFilter { get; set; } = "pending"; // all, pending, resolved
    }
}
