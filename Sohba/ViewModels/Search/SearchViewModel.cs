using Sohba.Application.DTOs.SearchAggregate;

namespace Sohba.ViewModels.Search
{
    public class SearchViewModel
    {
        public string Query { get; set; }
        public SearchResultDto Results { get; set; } = new();
        public string ActiveTab { get; set; } = "all";
    }
}
