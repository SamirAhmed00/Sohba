using Sohba.Application.DTOs.PostAggregate;

namespace Sohba.ViewModels.Dashboard
{
    public class DashboardCommentsViewModel
    {
        public List<CommentResponseDto> Comments { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string SearchTerm { get; set; } = string.Empty;
    }
}
