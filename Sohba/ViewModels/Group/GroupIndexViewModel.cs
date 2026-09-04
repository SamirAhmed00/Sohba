using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.GroupAndPageAggregate;

namespace Sohba.ViewModels.Group
{
    public class GroupIndexViewModel
    {
        public PagedResult<GroupResponseDto> Groups { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
    }
}
