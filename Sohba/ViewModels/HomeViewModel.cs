using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.StoryAggregate;

namespace Sohba.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<PostResponseDto> Posts { get; set; }
        public IEnumerable<StoryResponseDto> Stories { get; set; }
    }
}
