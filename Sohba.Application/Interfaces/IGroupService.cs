using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Domain.Common;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IGroupService
    {
        // Basic CRUD & Discovery
        Task<Result<PagedResult<GroupResponseDto>>> GetGroupsPagedAsync(
            string? search,
            int page,
            int pageSize,
            Guid? currentUserId = null);

        Task<Result<IEnumerable<GroupResponseDto>>> GetUserGroupsAsync(Guid userId);

        Task<Result<GroupResponseDto>> GetGroupByIdAsync(
            Guid groupId,
            Guid currentUserId);

        Task<Result<GroupResponseDto>> CreateGroupAsync(
            GroupCreateDto groupDto,
            Guid adminId);

        Task<Result<GroupResponseDto>> UpdateGroupAsync(
            GroupUpdateDto updateDto,
            Guid userId);

        Task<Result<bool>> DeleteGroupAsync(
            Guid groupId,
            Guid userId,
            string reason,
            bool isAdmin = false);

        Task<Result<IEnumerable<GroupResponseDto>>> GetRecommendedGroupsAsync(
            Guid userId,
            int count = 5);

        Task<Result<int>> GetGroupsCountAsync();

        // Membership & Roles
        Task<Result<bool>> JoinGroupAsync(Guid groupId, Guid userId);

        Task<Result<bool>> LeaveGroupAsync(Guid groupId, Guid userId);

        Task<Result<PagedResult<GroupMemberDto>>> GetMembersPagedAsync(
            Guid groupId,
            string? search,
            int page,
            int pageSize,
            Guid groupAdminId);

        Task<Result<IEnumerable<GroupMemberDto>>> GetGroupMembersAsync(Guid groupId);

        Task<Result<bool>> PromoteMemberAsync(
            Guid groupId,
            Guid targetUserId,
            Guid actionUserId);

        Task<Result<bool>> DemoteMemberAsync(
            Guid groupId,
            Guid targetUserId,
            Guid actionUserId);

        Task<Result<bool>> KickMemberAsync(
            Guid groupId,
            Guid targetUserId,
            Guid adminId);

        Task<GroupRole?> GetUserRoleInGroupAsync(
            Guid groupId,
            Guid userId);

        Task<Result<bool>> IsMemberAsync(
            Guid groupId,
            Guid userId);

        // Join Requests
        Task<Result<bool>> SubmitJoinRequestAsync(
            Guid userId,
            SubmitJoinRequestDto dto);

        Task<Result<PagedResult<GroupJoinRequestDto>>> GetPendingJoinRequestsAsync(
            Guid groupId,
            Guid actionUserId,
            int page,
            int pageSize);

        Task<Result<bool>> ReviewJoinRequestAsync(
            Guid actionUserId,
            ReviewJoinRequestDto dto);

        Task<GroupJoinRequestStatus?> GetUserJoinRequestStatusAsync(
            Guid groupId,
            Guid userId);

        Task<Result<int>> GetPendingJoinRequestsCountAsync(
            Guid groupId,
            Guid actionUserId);

        // Dashboard
        Task<Result<IEnumerable<DeletedGroupDto>>> GetDeletedGroupsAsync();
    }
}
