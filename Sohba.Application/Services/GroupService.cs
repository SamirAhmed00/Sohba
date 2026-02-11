using AutoMapper;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Domain_Rules.Interface;
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

        public async Task<GroupResponseDto> CreateGroupAsync(GroupCreateDto groupDto, Guid adminId)
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

            group.GroupMembers = new List<GroupMember> { adminMember };

            await _unitOfWork.CompleteAsync();

            return _mapper.Map<GroupResponseDto>(group);
        }

        public async Task<bool> JoinGroupAsync(Guid groupId, Guid userId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null) return false;

            // Check if user is already banned via IGroupRepository
            bool isBanned = _unitOfWork.Groups.IsUserBannedFromGroup(userId, groupId);

            var validation = _groupDomainService.CanJoinGroup(userId, false, isBanned);
            if (!validation.IsSuccess) throw new Exception(validation.Error);

            var newMember = new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                Role = GroupRole.Member, 
                JoinedAt = DateTime.UtcNow,
                IsBanned = false
            };

            _unitOfWork.Groups.Update(group); 

            return await _unitOfWork.CompleteAsync() > 0;
        }

        public async Task<IEnumerable<GroupMemberDto>> GetGroupMembersAsync(Guid groupId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
            if (group == null) return new List<GroupMemberDto>();

            return _mapper.Map<IEnumerable<GroupMemberDto>>(group.GroupMembers);
        }
    }
}
