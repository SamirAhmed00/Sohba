using AutoMapper;
using Microsoft.Extensions.Logging;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sohba.Application.Services
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IGroupDomainService _groupDomainService;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly ILogger<GroupService> _logger;

        public GroupService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IGroupDomainService groupDomainService,
            INotificationService notificationService,
            IUserService userService,
            ILogger<GroupService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _groupDomainService = groupDomainService;
            _notificationService = notificationService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GroupResponseDto>>> GetGroupsPagedAsync(
            string? search,
            int page,
            int pageSize,
            Guid? currentUserId = null)
        {
            var (groups, totalCount) =
                await _unitOfWork.Groups.GetGroupsPagedAsync(search, page, pageSize);

            var items = groups.Select(g =>
            {
                var dto = _mapper.Map<GroupResponseDto>(g);

                dto.AdminName = g.Admin?.Name ?? "Group Owner";

                dto.MembersCount =
                    g.GroupMembers?.Count(m => !m.IsBanned) ?? 0;

                dto.IsCurrentUserMember =
                    currentUserId.HasValue &&
                    g.GroupMembers != null &&
                    g.GroupMembers.Any(m =>
                        m.UserId == currentUserId.Value &&
                        !m.IsBanned);

                return dto;
            }).ToList();

            var pagedResult = new PagedResult<GroupResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(
                    (double)totalCount / pageSize)
            };

            return Result<PagedResult<GroupResponseDto>>.Success(pagedResult);
        }

        public async Task<Result<GroupResponseDto>> GetGroupByIdAsync(
            Guid groupId,
            Guid currentUserId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);

            if (group == null || group.IsDeleted)
                return Result<GroupResponseDto>.Failure("Group not found.");

            var response = _mapper.Map<GroupResponseDto>(group);

            response.AdminName = group.Admin?.Name ?? "Group Owner";

            response.MembersCount =
                group.GroupMembers?.Count(m => !m.IsBanned) ?? 0;

            response.IsCurrentUserMember =
                currentUserId != Guid.Empty &&
                group.GroupMembers != null &&
                group.GroupMembers.Any(m =>
                    m.UserId == currentUserId &&
                    !m.IsBanned);

            if (currentUserId != Guid.Empty &&
                !response.IsCurrentUserMember &&
                group.IsPrivate)
            {
                response.UserJoinRequestStatus =
                    await GetUserJoinRequestStatusAsync(
                        groupId,
                        currentUserId);
            }

            return Result<GroupResponseDto>.Success(response);
        }

        public async Task<Result<GroupResponseDto>> CreateGroupAsync(
            GroupCreateDto groupDto,
            Guid adminId)
        {
            var group = _mapper.Map<Group>(groupDto);

            group.AdminId = adminId;
            group.CreatedAt = DateTime.UtcNow;

            var adminMember = new GroupMember
            {
                UserId = adminId,
                Role = GroupRole.Admin,
                JoinedAt = DateTime.UtcNow,
                IsBanned = false
            };

            group.GroupMembers =
                new List<GroupMember> { adminMember };

            _unitOfWork.Groups.Add(group);

            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<GroupResponseDto>(group);

            response.AdminName =
                (await _unitOfWork.Users.GetByIdAsync(adminId))?.Name
                ?? "Group Owner";

            response.MembersCount = 1;
            response.IsCurrentUserMember = true;

            return Result<GroupResponseDto>.Success(response);
        }

        public async Task<Result<GroupResponseDto>> UpdateGroupAsync(
            GroupUpdateDto updateDto,
            Guid userId)
        {
            var trackedGroup =
                await _unitOfWork.Groups.GetTrackedGroupByIdAsync(
                    updateDto.Id);

            if (trackedGroup == null || trackedGroup.IsDeleted)
                return Result<GroupResponseDto>.Failure("Group not found.");

            var validation =
                _groupDomainService.CanUpdateGroup(
                    userId,
                    trackedGroup.Id,
                    trackedGroup.AdminId);

            if (!validation.IsSuccess)
                return Result<GroupResponseDto>.Failure(
                    validation.Error);

            trackedGroup.Name = updateDto.Name;
            trackedGroup.Description = updateDto.Description;
            trackedGroup.Rules = updateDto.Rules;
            trackedGroup.ImageUrl = updateDto.ImageUrl;
            trackedGroup.BackgroundImageUrl = updateDto.BackgroundImageUrl;
            trackedGroup.IsPrivate = updateDto.IsPrivate;

            await _unitOfWork.CompleteAsync();

            var response =
                _mapper.Map<GroupResponseDto>(trackedGroup);

            response.AdminName =
                trackedGroup.Admin?.Name ?? "Group Owner";

            response.MembersCount = 0;
            response.IsCurrentUserMember = true;

            return Result<GroupResponseDto>.Success(response);
        }

        public async Task<Result<bool>> DeleteGroupAsync(
            Guid groupId,
            Guid userId,
            string reason,
            bool isAdmin = false)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result<bool>.Failure(
                    "A deletion reason is required.");

            var group =
                await _unitOfWork.Groups.GetByIdAsync(groupId);

            if (group == null || group.IsDeleted)
                return Result<bool>.Failure("Group not found.");

            var validation =
                _groupDomainService.CanDeleteGroup(
                    userId,
                    group.AdminId,
                    isAdmin);

            if (!validation.IsSuccess)
                return Result<bool>.Failure(
                    validation.Error);

            _logger.LogInformation(
                "Group {GroupId} ('{Name}') soft-deleted by user {UserId} (Admin: {IsAdmin}). Reason: {Reason}",
                groupId,
                group.Name,
                userId,
                isAdmin,
                reason);

            // 1. Soft-delete all posts belonging to this group.
            var groupPosts =
                await _unitOfWork.Posts.GetGroupPostsAsync(groupId);

            foreach (var post in groupPosts)
            {
                post.IsDeleted = true;
                _unitOfWork.Posts.Update(post);
            }

            // 2. Soft-delete the group.
            var trackedGroup =
                await _unitOfWork.Groups.GetTrackedGroupByIdAsync(
                    groupId);

            if (trackedGroup != null)
            {
                trackedGroup.IsDeleted = true;
                trackedGroup.DeletionReason = reason.Trim();
                trackedGroup.DeletedAt = DateTime.UtcNow;
                trackedGroup.DeletedByUserId = userId;
            }

            // 3. Notify the owner when deletion was performed
            // by a system administrator.
            if (isAdmin && group.AdminId != userId)
            {
                await _notificationService.CreateNotificationAsync(
                    receiverId: group.AdminId,
                    message:
                        $"Your group '{group.Name}' was deleted by an administrator. Reason: {reason.Trim()}",
                    type: NotificationType.SystemAlert,
                    senderId: userId,
                    targetId: groupId);
            }

            var affectedRows =
                await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<bool>> JoinGroupAsync(
            Guid groupId,
            Guid userId)
        {
            var group =
                await _unitOfWork.Groups.GetByIdAsync(groupId);

            if (group == null || group.IsDeleted)
                return Result<bool>.Failure("Group not found.");

            var isAlreadyMember =
                await _unitOfWork.Groups.IsMemberAsync(
                    userId,
                    groupId);

            if (isAlreadyMember)
                return Result<bool>.Failure(
                    "You are already a member of this group.");

            var isBanned =
                _unitOfWork.Groups.IsUserBannedFromGroup(
                    userId,
                    groupId);

            var validation =
                _groupDomainService.CanJoinGroupDirectly(
                    userId,
                    group.IsPrivate,
                    isBanned);

            if (!validation.IsSuccess)
                return Result<bool>.Failure(
                    validation.Error);

            var newMember = new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow,
                IsBanned = false
            };

            _unitOfWork.Groups.AddMember(newMember);

            var affectedRows =
                await _unitOfWork.CompleteAsync();

            if (affectedRows > 0 &&
                group.AdminId != userId)
            {
                var userProfile =
                    await _userService.GetProfileAsync(userId);

                var userName =
                    userProfile.Value?.Name ?? "A new member";

                await _notificationService.CreateNotificationAsync(
                    receiverId: group.AdminId,
                    message:
                        $"{userName} joined your group '{group.Name}'",
                    type: NotificationType.GroupInvitation,
                    senderId: userId,
                    targetId: groupId);
            }

            return Result<bool>.Success(
                affectedRows > 0);
        }

        public async Task<Result<bool>> LeaveGroupAsync(
            Guid groupId,
            Guid userId)
        {
            var group =
                await _unitOfWork.Groups.GetByIdAsync(groupId);

            if (group == null || group.IsDeleted)
                return Result<bool>.Failure("Group not found.");

            var member =
                await _unitOfWork.Groups
                    .GetMemberByUserAndGroupAsync(
                        groupId,
                        userId);

            if (member == null)
                return Result<bool>.Failure(
                    "You are not a member of this group.");

            var isOwner =
                group.AdminId == userId;

            var totalMembers =
                group.GroupMembers?.Count(m => !m.IsBanned) ?? 0;

            var validation =
                _groupDomainService.CanLeaveGroup(
                    userId,
                    groupId,
                    isOwner,
                    totalMembers);

            if (!validation.IsSuccess)
                return Result<bool>.Failure(
                    validation.Error);

            // Owner must transfer ownership before leaving.
            if (isOwner)
            {
                var replacement =
                    await _unitOfWork.Groups
                        .GetEarliestEligibleMemberForOwnershipTransferAsync(
                            groupId,
                            userId);

                if (replacement == null)
                {
                    return Result<bool>.Failure(
                        "No eligible member is available to become the new group owner.");
                }

                var trackedGroup =
                    await _unitOfWork.Groups
                        .GetTrackedGroupByIdAsync(groupId);

                if (trackedGroup == null)
                {
                    return Result<bool>.Failure(
                        "Group could not be loaded for ownership transfer.");
                }

                trackedGroup.AdminId =
                    replacement.UserId;

                replacement.Role =
                    GroupRole.Admin;

                await _notificationService.CreateNotificationAsync(
                    receiverId: replacement.UserId,
                    message:
                        $"You are now the owner of the group '{group.Name}'.",
                    type: NotificationType.SystemAlert,
                    senderId: userId,
                    targetId: groupId);
            }

            _unitOfWork.Groups.RemoveMember(member);

            var affectedRows =
                await _unitOfWork.CompleteAsync();

            if (affectedRows > 0 && !isOwner)
            {
                var userProfile =
                    await _userService.GetProfileAsync(userId);

                var userName =
                    userProfile.Value?.Name ?? "A member";

                await _notificationService.CreateNotificationAsync(
                    receiverId: group.AdminId,
                    message:
                        $"{userName} left the group '{group.Name}'",
                    type: NotificationType.SystemAlert,
                    senderId: userId,
                    targetId: groupId);
            }

            return Result<bool>.Success(
                affectedRows > 0);
        }

        public async Task<Result<PagedResult<GroupMemberDto>>>
            GetMembersPagedAsync(
                Guid groupId,
                string? search,
                int page,
                int pageSize,
                Guid groupAdminId)
        {
            var (members, totalCount) =
                await _unitOfWork.Groups
                    .GetMembersPagedAsync(
                        groupId,
                        search,
                        page,
                        pageSize);

            var items = members
                .Select(m => new GroupMemberDto
                {
                    UserId = m.UserId,
                    UserName =
                        m.User?.Name ?? "Community Member",
                    UserAvatarUrl =
                        m.User?.ProfilePictureUrl,
                    Role = m.Role,
                    IsOwner =
                        m.UserId == groupAdminId,
                    JoinedAt = m.JoinedAt
                })
                .ToList();

            var pagedResult =
                new PagedResult<GroupMemberDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages =
                        (int)Math.Ceiling(
                            (double)totalCount /
                            pageSize)
                };

            return Result<PagedResult<GroupMemberDto>>
                .Success(pagedResult);
        }

        public async Task<Result<IEnumerable<GroupMemberDto>>>
            GetGroupMembersAsync(Guid groupId)
        {
            var group =
                await _unitOfWork.Groups.GetByIdAsync(groupId);

            if (group == null || group.IsDeleted)
            {
                return Result<IEnumerable<GroupMemberDto>>
                    .Failure("Group not found.");
            }

            var members =
                await _unitOfWork.Groups
                    .GetGroupMembersAsync(groupId);

            var dtos = members
                .Select(m => new GroupMemberDto
                {
                    UserId = m.UserId,
                    UserName =
                        m.User?.Name ?? "Community Member",
                    UserAvatarUrl =
                        m.User?.ProfilePictureUrl,
                    Role = m.Role,
                    IsOwner =
                        m.UserId == group.AdminId,
                    JoinedAt = m.JoinedAt
                })
                .ToList();

            return Result<IEnumerable<GroupMemberDto>>
                .Success(dtos);
        }

        public async Task<Result<bool>> PromoteMemberAsync(
            Guid groupId,
            Guid targetUserId,
            Guid actionUserId)
        {
            var group =
                await _unitOfWork.Groups.GetByIdAsync(groupId);

            if (group == null || group.IsDeleted)
                return Result<bool>.Failure(
                    "Group not found.");

            var actionUserRole =
                _unitOfWork.Groups.GetUserRoleInGroup(
                    actionUserId,
                    groupId);

            if (!actionUserRole.HasValue)
            {
                return Result<bool>.Failure(
                    "You are not a member of this group.");
            }

            var targetRole =
                _unitOfWork.Groups.GetUserRoleInGroup(
                    targetUserId,
                    groupId);

            if (!targetRole.HasValue)
            {
                return Result<bool>.Failure(
                    "Target user is not a member of this group.");
            }

            var validation =
                _groupDomainService.CanPromoteMember(
                    actionUserId,
                    actionUserRole.Value,
                    targetUserId,
                    targetRole.Value,
                    group.AdminId);

            if (!validation.IsSuccess)
                return Result<bool>.Failure(
                    validation.Error);

            var member =
                await _unitOfWork.Groups
                    .GetMemberByUserAndGroupAsync(
                        groupId,
                        targetUserId);

            if (member == null)
                return Result<bool>.Failure(
                    "Member not found in this group.");

            string newRoleName;

            if (member.Role == GroupRole.Member)
            {
                member.Role = GroupRole.CoAdmin;
                newRoleName = "Co-Admin";
            }
            else if (member.Role == GroupRole.CoAdmin)
            {
                member.Role = GroupRole.Admin;
                newRoleName = "Admin";
            }
            else
            {
                return Result<bool>.Failure(
                    "User is already at the highest membership role.");
            }

            var affectedRows =
                await _unitOfWork.CompleteAsync();

            if (affectedRows > 0)
            {
                await _notificationService.CreateNotificationAsync(
                    receiverId: targetUserId,
                    message:
                        $"You have been promoted to {newRoleName} in '{group.Name}'",
                    type: NotificationType.SystemAlert,
                    senderId: actionUserId,
                    targetId: groupId);
            }

            return Result<bool>.Success(
                affectedRows > 0);
        }

        public async Task<Result<bool>> DemoteMemberAsync(
            Guid groupId,
            Guid targetUserId,
            Guid actionUserId)
        {
            var group =
                await _unitOfWork.Groups.GetByIdAsync(groupId);

            if (group == null || group.IsDeleted)
                return Result<bool>.Failure(
                    "Group not found.");

            var actionUserRole =
                _unitOfWork.Groups.GetUserRoleInGroup(
                    actionUserId,
                    groupId);

            if (!actionUserRole.HasValue)
            {
                return Result<bool>.Failure(
                    "You are not a member of this group.");
            }

            var targetRole =
                _unitOfWork.Groups.GetUserRoleInGroup(
                    targetUserId,
                    groupId);

            if (!targetRole.HasValue)
            {
                return Result<bool>.Failure(
                    "Target user is not a member of this group.");
            }

            var validation =
                _groupDomainService.CanDemoteMember(
                    actionUserId,
                    actionUserRole.Value,
                    targetUserId,
                    targetRole.Value,
                    group.AdminId);

            if (!validation.IsSuccess)
                return Result<bool>.Failure(
                    validation.Error);

            var member =
                await _unitOfWork.Groups
                    .GetMemberByUserAndGroupAsync(
                        groupId,
                        targetUserId);

            if (member == null)
                return Result<bool>.Failure(
                    "Member not found in this group.");

            string newRoleName;

            if (member.Role == GroupRole.Admin)
            {
                member.Role = GroupRole.CoAdmin;
                newRoleName = "Co-Admin";
            }
            else if (member.Role == GroupRole.CoAdmin)
            {
                member.Role = GroupRole.Member;
                newRoleName = "Member";
            }
            else
            {
                return Result<bool>.Failure(
                    "Regular members cannot be demoted further.");
            }

            var affectedRows =
                await _unitOfWork.CompleteAsync();

            if (affectedRows > 0)
            {
                await _notificationService.CreateNotificationAsync(
                    receiverId: targetUserId,
                    message:
                        $"Your leadership role in '{group.Name}' has been changed to {newRoleName}",
                    type: NotificationType.SystemAlert,
                    senderId: actionUserId,
                    targetId: groupId);
            }

            return Result<bool>.Success(
                affectedRows > 0);
        }

        public async Task<Result<bool>> KickMemberAsync(
            Guid groupId,
            Guid targetUserId,
            Guid adminId)
        {
            var group =
                await _unitOfWork.Groups.GetByIdAsync(groupId);

            if (group == null || group.IsDeleted)
                return Result<bool>.Failure(
                    "Group not found.");

            var actionUserRole =
                _unitOfWork.Groups.GetUserRoleInGroup(
                    adminId,
                    groupId);

            if (!actionUserRole.HasValue)
            {
                return Result<bool>.Failure(
                    "You are not a member of this group.");
            }

            var targetRole =
                _unitOfWork.Groups.GetUserRoleInGroup(
                    targetUserId,
                    groupId);

            if (!targetRole.HasValue)
            {
                return Result<bool>.Failure(
                    "Target user is not a member of this group.");
            }

            var validation =
                _groupDomainService.CanKickMember(
                    adminId,
                    actionUserRole.Value,
                    targetUserId,
                    targetRole.Value,
                    group.AdminId);

            if (!validation.IsSuccess)
                return Result<bool>.Failure(
                    validation.Error);

            var memberToKick =
                await _unitOfWork.Groups
                    .GetMemberByUserAndGroupAsync(
                        groupId,
                        targetUserId);

            if (memberToKick == null)
                return Result<bool>.Failure(
                    "Member not found in this group.");

            _unitOfWork.Groups.RemoveMember(
                memberToKick);

            var affectedRows =
                await _unitOfWork.CompleteAsync();

            if (affectedRows > 0)
            {
                await _notificationService.CreateNotificationAsync(
                    receiverId: targetUserId,
                    message:
                        $"You have been removed from the group '{group.Name}'",
                    type: NotificationType.SystemAlert,
                    senderId: adminId,
                    targetId: groupId);
            }

            return Result<bool>.Success(
                affectedRows > 0);
        }

        // ==================== Private Group Join Requests ====================

        public async Task<Result<bool>> SubmitJoinRequestAsync(
            Guid userId,
            SubmitJoinRequestDto dto)
        {
            var group =
                await _unitOfWork.Groups
                    .GetByIdAsync(dto.GroupId);

            if (group == null || group.IsDeleted)
                return Result<bool>.Failure(
                    "Group not found.");

            var isMember =
                await _unitOfWork.Groups
                    .IsMemberAsync(
                        userId,
                        dto.GroupId);

            var isBanned =
                _unitOfWork.Groups
                    .IsUserBannedFromGroup(
                        userId,
                        dto.GroupId);

            var existingPending =
                await _unitOfWork.Groups
                    .GetPendingJoinRequestAsync(
                        dto.GroupId,
                        userId);

            var validation =
                _groupDomainService.CanSubmitJoinRequest(
                    userId,
                    group.IsPrivate,
                    isMember,
                    isBanned,
                    existingPending != null);

            if (!validation.IsSuccess)
                return Result<bool>.Failure(
                    validation.Error);

            var request = new GroupJoinRequest
            {
                GroupId = dto.GroupId,
                UserId = userId,
                Reason = dto.Reason?.Trim(),
                Status = GroupJoinRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Groups.AddJoinRequest(
                request);

            var affectedRows =
                await _unitOfWork.CompleteAsync();

            if (affectedRows > 0)
            {
                var userProfile =
                    await _userService.GetProfileAsync(userId);

                var userName =
                    userProfile.Value?.Name ?? "A user";

                var adminMembers =
                    await _unitOfWork.Groups
                        .GetGroupAdminsAsync(dto.GroupId);

                var leaderIds =
                    adminMembers
                        .Select(m => m.UserId)
                        .Append(group.AdminId)
                        .Distinct()
                        .ToList();

                foreach (var leaderId in leaderIds)
                {
                    await _notificationService
                        .CreateNotificationAsync(
                            receiverId: leaderId,
                            message:
                                $"{userName} requested to join your private group '{group.Name}'.",
                            type: NotificationType.GroupInvitation,
                            senderId: userId,
                            targetId: dto.GroupId);
                }
            }

            return Result<bool>.Success(
                affectedRows > 0);
        }

        public async Task<Result<PagedResult<GroupJoinRequestDto>>>
            GetPendingJoinRequestsAsync(
                Guid groupId,
                Guid actionUserId,
                int page,
                int pageSize)
        {
            var group =
                await _unitOfWork.Groups.GetByIdAsync(
                    groupId);

            if (group == null || group.IsDeleted)
            {
                return Result<PagedResult<GroupJoinRequestDto>>
                    .Failure("Group not found.");
            }

            var actionRole =
                _unitOfWork.Groups
                    .GetUserRoleInGroup(
                        actionUserId,
                        groupId);

            if (!actionRole.HasValue)
            {
                return Result<PagedResult<GroupJoinRequestDto>>
                    .Failure(
                        "You are not a member of this group.");
            }

            var validation =
                _groupDomainService
                    .CanReviewJoinRequest(
                        actionUserId,
                        actionRole.Value,
                        group.AdminId == actionUserId);

            if (!validation.IsSuccess)
            {
                return Result<PagedResult<GroupJoinRequestDto>>
                    .Failure(validation.Error);
            }

            var (requests, totalCount) =
                await _unitOfWork.Groups
                    .GetPendingJoinRequestsPagedAsync(
                        groupId,
                        page,
                        pageSize);

            var dtos = requests
                .Select(r => new GroupJoinRequestDto
                {
                    Id = r.Id,
                    GroupId = r.GroupId,
                    GroupName = group.Name,
                    UserId = r.UserId,
                    UserName =
                        r.User?.Name ?? "Applicant",
                    UserAvatarUrl =
                        r.User?.ProfilePictureUrl,
                    Reason = r.Reason,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToList();

            var pagedResult =
                new PagedResult<GroupJoinRequestDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages =
                        (int)Math.Ceiling(
                            (double)totalCount /
                            pageSize)
                };

            return Result<PagedResult<GroupJoinRequestDto>>
                .Success(pagedResult);
        }

        public async Task<Result<bool>> ReviewJoinRequestAsync(
            Guid actionUserId,
            ReviewJoinRequestDto dto)
        {
            var request =
                await _unitOfWork.Groups
                    .GetJoinRequestByIdAsync(
                        dto.RequestId);

            if (request == null)
                return Result<bool>.Failure(
                    "Join request not found.");

            if (request.Status !=
                GroupJoinRequestStatus.Pending)
            {
                return Result<bool>.Failure(
                    "This join request has already been reviewed.");
            }

            var group =
                await _unitOfWork.Groups
                    .GetByIdAsync(request.GroupId);

            if (group == null || group.IsDeleted)
            {
                return Result<bool>.Failure(
                    "Associated group not found or has been deleted.");
            }

            var actionRole =
                _unitOfWork.Groups
                    .GetUserRoleInGroup(
                        actionUserId,
                        request.GroupId);

            if (!actionRole.HasValue)
            {
                return Result<bool>.Failure(
                    "You are not a member of this group.");
            }

            var validation =
                _groupDomainService
                    .CanReviewJoinRequest(
                        actionUserId,
                        actionRole.Value,
                        group.AdminId == actionUserId);

            if (!validation.IsSuccess)
                return Result<bool>.Failure(
                    validation.Error);

            request.ReviewedAt =
                DateTime.UtcNow;

            request.ReviewedByUserId =
                actionUserId;

            if (dto.Approve)
            {
                request.Status =
                    GroupJoinRequestStatus.Accepted;

                var isAlreadyMember =
                    await _unitOfWork.Groups
                        .IsMemberAsync(
                            request.UserId,
                            request.GroupId);

                if (!isAlreadyMember)
                {
                    var newMember = new GroupMember
                    {
                        GroupId = request.GroupId,
                        UserId = request.UserId,
                        Role = GroupRole.Member,
                        JoinedAt = DateTime.UtcNow,
                        IsBanned = false
                    };

                    _unitOfWork.Groups
                        .AddMember(newMember);
                }

                await _unitOfWork.CompleteAsync();

                await _notificationService
                    .CreateNotificationAsync(
                        receiverId: request.UserId,
                        message:
                            $"Your request to join '{group.Name}' was approved!",
                        type: NotificationType.SystemAlert,
                        senderId: actionUserId,
                        targetId: request.GroupId);
            }
            else
            {
                request.Status =
                    GroupJoinRequestStatus.Rejected;

                await _unitOfWork.CompleteAsync();

                await _notificationService
                    .CreateNotificationAsync(
                        receiverId: request.UserId,
                        message:
                            $"Your request to join '{group.Name}' was not accepted.",
                        type: NotificationType.SystemAlert,
                        senderId: actionUserId,
                        targetId: request.GroupId);
            }

            return Result<bool>.Success(true);
        }

        public async Task<GroupJoinRequestStatus?>
            GetUserJoinRequestStatusAsync(
                Guid groupId,
                Guid userId)
        {
            var request =
                await _unitOfWork.Groups
                    .GetPendingJoinRequestAsync(
                        groupId,
                        userId);

            return request?.Status;
        }

        public async Task<Result<int>>
            GetPendingJoinRequestsCountAsync(
                Guid groupId,
                Guid actionUserId)
        {
            var group =
                await _unitOfWork.Groups
                    .GetByIdAsync(groupId);

            if (group == null || group.IsDeleted)
                return Result<int>.Success(0);

            var actionRole =
                _unitOfWork.Groups
                    .GetUserRoleInGroup(
                        actionUserId,
                        groupId);

            if (group.AdminId != actionUserId &&
                actionRole != GroupRole.Admin)
            {
                return Result<int>.Success(0);
            }

            var count =
                await _unitOfWork.Groups
                    .GetPendingJoinRequestsCountAsync(
                        groupId);

            return Result<int>.Success(count);
        }

        public Task<GroupRole?> GetUserRoleInGroupAsync(
            Guid groupId,
            Guid userId)
        {
            return Task.FromResult(
                _unitOfWork.Groups
                    .GetUserRoleInGroup(
                        userId,
                        groupId));
        }

        public async Task<Result<bool>> IsMemberAsync(
            Guid groupId,
            Guid userId)
        {
            var isMember =
                await _unitOfWork.Groups
                    .IsMemberAsync(
                        userId,
                        groupId);

            return Result<bool>.Success(
                isMember);
        }

        public async Task<Result<IEnumerable<GroupResponseDto>>>
            GetUserGroupsAsync(Guid userId)
        {
            var groups =
                await _unitOfWork.Groups
                    .GetGroupsByUserIdAsync(userId);

            var response =
                _mapper.Map<IEnumerable<GroupResponseDto>>(
                    groups);

            return Result<IEnumerable<GroupResponseDto>>
                .Success(response);
        }

        public async Task<Result<IEnumerable<GroupResponseDto>>>
            GetRecommendedGroupsAsync(
                Guid userId,
                int count = 5)
        {
            var groups =
                await _unitOfWork.Groups
                    .GetRecommendedGroupsAsync(
                        userId,
                        count);

            var dtos = groups
                .Select(g =>
                {
                    var dto =
                        _mapper.Map<GroupResponseDto>(g);

                    dto.AdminName =
                        g.Admin?.Name ?? "Group Owner";

                    dto.MembersCount =
                        g.GroupMembers?
                            .Count(m => !m.IsBanned) ?? 0;

                    dto.IsCurrentUserMember = false;

                    return dto;
                })
                .ToList();

            return Result<IEnumerable<GroupResponseDto>>
                .Success(dtos);
        }

        public async Task<Result<int>> GetGroupsCountAsync()
        {
            var count =
                await _unitOfWork.Groups.CountAsync();

            return Result<int>.Success(count);
        }

        public async Task<Result<IEnumerable<DeletedGroupDto>>>
            GetDeletedGroupsAsync()
        {
            var deletedGroups =
                await _unitOfWork.Groups
                    .GetDeletedGroupsAsync();

            var dtos =
                new List<DeletedGroupDto>();

            foreach (var group in deletedGroups)
            {
                var deletedByName = "System";

                if (group.DeletedByUserId.HasValue)
                {
                    var user =
                        await _unitOfWork.Users
                            .GetByIdAsync(
                                group.DeletedByUserId.Value);

                    if (user != null)
                        deletedByName = user.Name;
                }

                dtos.Add(new DeletedGroupDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    ImageUrl = group.ImageUrl,
                    IsPrivate = group.IsPrivate,
                    CreatedAt = group.CreatedAt,
                    DeletedAt = group.DeletedAt,
                    DeletionReason =
                        group.DeletionReason
                        ?? "No reason provided",
                    AdminId = group.AdminId,
                    OwnerName =
                        group.Admin?.Name
                        ?? "Unknown Owner",
                    DeletedByUserId =
                        group.DeletedByUserId,
                    DeletedByName =
                        deletedByName
                });
            }

            return Result<IEnumerable<DeletedGroupDto>>
                .Success(dtos);
        }
    }
}