using Sohba.Application.DTOs.GroupAndPageAggregate;

namespace Sohba.ViewModels.Dashboard
{
    public class DashboardDeletedPagesViewModel
    {
        public List<DeletedPageDto> DeletedPages { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SearchTerm { get; set; } = string.Empty;
    }
}
