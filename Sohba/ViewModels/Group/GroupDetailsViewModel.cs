using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Domain.Enums;

namespace Sohba.ViewModels.Group
{
    public class GroupDetailsViewModel
    {
        public GroupResponseDto Group { get; set; } = null!;

        public GroupJoinRequestStatus? UserJoinRequestStatus { get; set; }

        public bool CanManageRequests { get; set; }

        public int PendingRequestsCount { get; set; }
    }
}
