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

            _unitOfWork.Groups.Add(group);
         
            var adminMember = new GroupMember
            {
                GroupId = group.Id,
                UserId = adminId,
                Role = GroupRole.Admin,
                JoinedAt = DateTime.UtcNow,
                IsBanned = false
            };

            
            _unitOfWork.Groups.AddMember(adminMember);

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

        public async Task<Result<IEnumerable<GroupMemberDto>>> GetGroupMembersAsync(Guid groupId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null)
                return Result<IEnumerable<GroupMemberDto>>.Failure("Group not found.");

            var members = _mapper.Map<IEnumerable<GroupMemberDto>>(group.GroupMembers);
            return Result<IEnumerable<GroupMemberDto>>.Success(members);
        }
    }
}
