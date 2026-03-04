using Sohba.Application.DTOs.GroupAndPageAggregate;

namespace Sohba.ViewModels.Group
{
    public class GroupDetailsViewModel
    {
        public GroupResponseDto Group { get; set; }
        public IEnumerable<GroupMemberDto> Members { get; set; }
    }
}
