using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IGroupService
    {
        // Basic CRUD
        Task<Result<IEnumerable<GroupResponseDto>>> GetAllGroupsAsync(Guid? currentUserId = null);
        Task<Result<IEnumerable<GroupResponseDto>>> GetUserGroupsAsync(Guid userId);

        // TODO : I Want To Refactor This Code And Remove The Overloading And Every Thing Depends on it
        Task<Result<GroupResponseDto>> GetGroupByIdAsync(Guid groupId);
        Task<Result<GroupResponseDto>> GetGroupByIdAsync(Guid groupId, Guid currentUserId);


        Task<Result<GroupResponseDto>> CreateGroupAsync(GroupCreateDto groupDto, Guid adminId);
        Task<Result<bool>> DeleteGroupAsync(Guid groupId, Guid userId, string reason);
        Task<Result<GroupResponseDto>> UpdateGroupAsync(GroupUpdateDto updateDto, Guid userId);
        Task<Result<IEnumerable<GroupResponseDto>>> GetRecommendedGroupsAsync(Guid userId, int count = 5);

        // Membership
        Task<Result<bool>> JoinGroupAsync(Guid groupId, Guid userId);
        Task<Result<bool>> LeaveGroupAsync(Guid groupId, Guid userId);
        Task<Result<IEnumerable<GroupMemberDto>>> GetGroupMembersAsync(Guid groupId);

        // Administrative Actions
        Task<Result<bool>> KickMemberAsync(Guid groupId, Guid targetUserId, Guid adminId);
        Task<Result<int>> GetGroupsCountAsync();

        Task<Result<bool>> PromoteMemberAsync(Guid groupId, Guid targetUserId, Guid actionUserId);
        Task<Result<bool>> AddMemberAsync(Guid groupId, Guid targetUserId, Guid adminId);
        Task<string> GetUserRoleInGroupAsync(Guid groupId, Guid userId);
        Task<Result<bool>> IsMemberAsync(Guid groupId, Guid userId);
    }
}
