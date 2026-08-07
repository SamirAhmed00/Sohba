using Sohba.Application.DTOs.PostAggregate;

namespace Sohba.ViewModels.Post
{
    public class SavedPostsViewModel
    {
        public IEnumerable<SavedPostsGroupedDto> Groups { get; set; } = new List<SavedPostsGroupedDto>();
    }
}
