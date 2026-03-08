using AutoMapper;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Domain_Rules.Logic;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IGroupDomainService _groupDomainService;

        public GroupService(IUnitOfWork unitOfWork, IMapper mapper, IGroupDomainService groupDomainService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _groupDomainService = groupDomainService;
        }

        public async Task<Result<GroupResponseDto>> CreateGroupAsync(GroupCreateDto groupDto, Guid adminId)
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


            group.GroupMembers = new List<GroupMember> { adminMember };
         
            _unitOfWork.Groups.Add(group);

            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<GroupResponseDto>(group);
            return Result<GroupResponseDto>.Success(response);
        }

        public async Task<Result<bool>> JoinGroupAsync(Guid groupId, Guid userId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null)
                return Result<bool>.Failure("Group not found.");

            bool isBanned = _unitOfWork.Groups.IsUserBannedFromGroup(userId, groupId);


            var validation = _groupDomainService.CanJoinGroup(userId, false, isBanned);
            if (!validation.IsSuccess)
                return Result<bool>.Failure(validation.Error);

            var newMember = new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                Role = GroupRole.Member,
                JoinedAt = DateTime.UtcNow,
                IsBanned = false
            };

            _unitOfWork.Groups.AddMember(newMember);

            var affectedRows = await _unitOfWork.CompleteAsync();
            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<IEnumerable<GroupResponseDto>>> GetAllGroupsAsync(Guid? currentUserId = null)
        {
            var groups = await _unitOfWork.Groups.GetAllAsync();

            var response = groups.Select(g => {
                var dto = _mapper.Map<GroupResponseDto>(g);

                dto.AdminName = g.Admin?.Name ?? "System Admin";
                dto.MembersCount = g.GroupMembers?.Count ?? 0;

                dto.IsCurrentUserMember = currentUserId.HasValue && g.GroupMembers != null &&
                                         g.GroupMembers.Any(m => m.UserId == currentUserId.Value); 

                return dto;
            }).ToList();

            return Result<IEnumerable<GroupResponseDto>>.Success(response);
        }

        public async Task<Result<GroupResponseDto>> GetGroupByIdAsync(Guid groupId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null) return Result<GroupResponseDto>.Failure("Group not found.");

            var response = _mapper.Map<GroupResponseDto>(group);
            return Result<GroupResponseDto>.Success(response);
        }

        public async Task<Result<bool>> DeleteGroupAsync(Guid groupId, Guid userId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null) return Result<bool>.Failure("Group not found.");

            // Domain Rule: Check if user is owner
            var validation = _groupDomainService.CanDeleteGroup(userId, group.AdminId);
            if (!validation.IsSuccess) return Result<bool>.Failure(validation.Error);

            _unitOfWork.Groups.Delete(group);
            var affectedRows = await _unitOfWork.CompleteAsync();
            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<IEnumerable<GroupMemberDto>>> GetGroupMembersAsync(Guid groupId)
        {
            try
            {
                var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
                if (group == null)
                    return Result<IEnumerable<GroupMemberDto>>.Failure("Group not found.");

                var members = group.GroupMembers ?? new List<GroupMember>();

                var memberDtos = members.Select(m => new GroupMemberDto
                {
                    UserId = m.UserId,
                    UserName = m.User?.Name ?? "Unknown", 
                    Role = m.Role.ToString(),
                    JoinedAt = m.JoinedAt
                }).ToList();

                return Result<IEnumerable<GroupMemberDto>>.Success(memberDtos);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<GroupMemberDto>>.Failure($"Error loading members: {ex.Message}");
            }
        }

        public async Task<Result<bool>> KickMemberAsync(Guid groupId, Guid targetUserId, Guid adminId)
        {
            var adminRole = _unitOfWork.Groups.GetUserRoleInGroup(adminId, groupId);
            var targetRole = _unitOfWork.Groups.GetUserRoleInGroup(targetUserId, groupId);

            var validation = _groupDomainService.CanKickMember(adminId, adminRole, targetUserId, targetRole);
            if (!validation.IsSuccess) return Result<bool>.Failure(validation.Error);

            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            var memberToKick = group.GroupMembers.FirstOrDefault(m => m.UserId == targetUserId);
            if (memberToKick != null)
            {
                _unitOfWork.Groups.RemoveMember(memberToKick);
                await _unitOfWork.CompleteAsync();
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> LeaveGroupAsync(Guid groupId, Guid userId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null)
                return Result<bool>.Failure("Group not found.");

            var member = group.GroupMembers.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
                return Result<bool>.Failure("You are not a member of this group.");

            var isAdmin = member.Role == GroupRole.Admin;
            var adminCount = group.GroupMembers.Count(m => m.Role == GroupRole.Admin);

            var validation = _groupDomainService.CanLeaveGroup(userId, groupId, isAdmin, adminCount);
            if (!validation.IsSuccess)
                return Result<bool>.Failure(validation.Error);

            _unitOfWork.Groups.RemoveMember(member);
            var affectedRows = await _unitOfWork.CompleteAsync();
            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<IEnumerable<GroupResponseDto>>> GetUserGroupsAsync(Guid userId)
        {
            var groups = await _unitOfWork.Groups.GetGroupsByUserIdAsync(userId); 
            var response = _mapper.Map<IEnumerable<GroupResponseDto>>(groups);
            return Result<IEnumerable<GroupResponseDto>>.Success(response);
        }

        public async Task<Result<GroupResponseDto>> UpdateGroupAsync(GroupUpdateDto updateDto, Guid userId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(updateDto.Id);
            if (group == null)
                return Result<GroupResponseDto>.Failure("Group not found.");

            var validation = _groupDomainService.CanUpdateGroup(userId, group.Id, group.AdminId);
            if (!validation.IsSuccess)
                return Result<GroupResponseDto>.Failure(validation.Error);

            _mapper.Map(updateDto, group);
            _unitOfWork.Groups.Update(group);
            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<GroupResponseDto>(group);
            return Result<GroupResponseDto>.Success(response);
        }

        public async Task<Result<IEnumerable<GroupResponseDto>>> GetRecommendedGroupsAsync(Guid userId, int count = 5)
        {
            var groups = await _unitOfWork.Groups.GetAllAsync();

            var recommended = groups
                .Where(g => g.GroupMembers == null || !g.GroupMembers.Any(m => m.UserId == userId))
                .OrderByDescending(g => g.GroupMembers?.Count ?? 0) 
                .Take(count)
                .Select(g => {
                    var dto = _mapper.Map<GroupResponseDto>(g);
                    dto.AdminName = g.Admin?.Name ?? "System Admin";
                    dto.MembersCount = g.GroupMembers?.Count ?? 0;
                    dto.IsCurrentUserMember = false;
                    return dto;
                })
                .ToList();

            return Result<IEnumerable<GroupResponseDto>>.Success(recommended);
        }
    }
}
