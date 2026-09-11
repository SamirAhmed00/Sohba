using Sohba.Application.DTOs.GroupAndPageAggregate;
using System.Collections.Generic;

namespace Sohba.ViewModels.Dashboard
{
    public class DashboardPagesViewModel
    {
        public List<PageResponseDto> Pages { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SearchTerm { get; set; } = string.Empty;
    }
}