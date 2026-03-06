using AutoMapper;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IProfileDomainService _profileDomainService;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, IProfileDomainService profileDomainService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _profileDomainService = profileDomainService;
        }

        public async Task<Result<UserResponseDto>> GetProfileAsync(Guid userId)
        {
            
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
                return Result<UserResponseDto>.Failure("User profile not found.");

            
            var response = _mapper.Map<UserResponseDto>(user);
            return Result<UserResponseDto>.Success(response);
        }

        public async Task<Result<bool>> UpdateProfileAsync(Guid userId, UserRequestDto updateDto)
        {
            
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return Result<bool>.Failure("User not found.");

            var validation = _profileDomainService.CanUpdateProfile(userId, user.Id);
            if (!validation.IsSuccess)
                return Result<bool>.Failure(validation.Error);

            _mapper.Map(updateDto, user);

            _unitOfWork.Users.Update(user);
            var affectedRows = await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(affectedRows > 0);
        }

        public async Task<Result<IEnumerable<UserResponseDto>>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<UserResponseDto>>(users);
            return Result<IEnumerable<UserResponseDto>>.Success(dtos);
        }

        public async Task<Result<bool>> DeleteUserAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return Result<bool>.Failure("User not found");

            // Soft delete
            user.IsDeleted = true;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<IEnumerable<UserResponseDto>>> GetUsersByStatusAsync(string status)
        {
            var allUsers = await _unitOfWork.Users.GetAllAsync();

            IEnumerable<User> filteredUsers;

            switch (status.ToLower())
            {
                case "active":
                    var blockedUsers = await _unitOfWork.Friendships.GetBlockedUsersAsync(Guid.Empty); // Need Edit ?
                    var blockedIds = blockedUsers.Select(b => b.FriendUserId).ToList();
                    filteredUsers = allUsers.Where(u => !blockedIds.Contains(u.Id));
                    break;

                case "blocked":
                    blockedUsers = await _unitOfWork.Friendships.GetBlockedUsersAsync(Guid.Empty);
                    filteredUsers = allUsers.Where(u => blockedUsers.Any(b => b.FriendUserId == u.Id));
                    break;

                default: 
                    filteredUsers = allUsers;
                    break;
            }

            var dtos = _mapper.Map<IEnumerable<UserResponseDto>>(filteredUsers);
            return Result<IEnumerable<UserResponseDto>>.Success(dtos);
        }
    }
}
