using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IGroupService
    {
        Task<Result<GroupResponseDto>> CreateGroupAsync(GroupCreateDto groupDto, Guid adminId);
        Task<Result<bool>> JoinGroupAsync(Guid groupId, Guid userId);
        Task<Result<IEnumerable<GroupMemberDto>>> GetGroupMembersAsync(Guid groupId);
    }
}
