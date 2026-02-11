using Sohba.Application.DTOs.GroupAndPageAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IGroupService
    {
        Task<GroupResponseDto> CreateGroupAsync(GroupCreateDto groupDto, Guid adminId);
        Task<bool> JoinGroupAsync(Guid groupId, Guid userId);
        Task<IEnumerable<GroupMemberDto>> GetGroupMembersAsync(Guid groupId);
    }
}
