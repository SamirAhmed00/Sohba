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
        private readonly IFriendshipRepository _friendshipRepository;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, IProfileDomainService profileDomainService, IFriendshipRepository friendshipRepository)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _profileDomainService = profileDomainService;
            _friendshipRepository = friendshipRepository;
        }

        // Original method (kept for backward compatibility)
        public async Task<Result<UserResponseDto>> GetProfileAsync(Guid userId)
        {
            // Call the new overload with the same userId as current user (owner)
            return await GetProfileAsync(userId, userId);
        }

        //  NEW: Get profile with privacy enforcement
        public async Task<Result<UserResponseDto>> GetProfileAsync(Guid userId, Guid currentUserId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
                return Result<UserResponseDto>.Failure("User profile not found.");

            //  PRIVACY CHECK: Verify user can view this profile
            var isFriend = await _friendshipRepository.AreFriendsAsync(currentUserId, userId);
            var isBlocked = await _friendshipRepository.IsUserBlockedAsync(currentUserId, userId);

            var isPrivateAccount = user.IsPrivateAccount; 

            var canView = _profileDomainService.CanViewProfile(
                currentUserId,
                userId,
                isPrivateAccount,
                isFriend,
                isBlocked
            );

            if (!canView.IsSuccess)
                return Result<UserResponseDto>.Failure(canView.Error);

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
                    var blockedUsers = await _friendshipRepository.GetBlockedUsersAsync(Guid.Empty);
                    var blockedIds = blockedUsers.Select(b => b.FriendUserId).ToList();
                    filteredUsers = allUsers.Where(u => !blockedIds.Contains(u.Id));
                    break;

                case "blocked":
                    blockedUsers = await _friendshipRepository.GetBlockedUsersAsync(Guid.Empty);
                    filteredUsers = allUsers.Where(u => blockedUsers.Any(b => b.FriendUserId == u.Id));
                    break;

                default:
                    filteredUsers = allUsers;
                    break;
            }

            var dtos = _mapper.Map<IEnumerable<UserResponseDto>>(filteredUsers);
            return Result<IEnumerable<UserResponseDto>>.Success(dtos);
        }

        public async Task<Result<int>> GetUsersCountAsync()
        {
            var count = await _unitOfWork.Users.CountAsync();
            return Result<int>.Success(count);
        }

        public async Task<Result<IEnumerable<UserResponseDto>>> GetRecentUsersAsync(int count)
        {
            var users = await _unitOfWork.Users.GetRecentAsync(count);
            var dtos = _mapper.Map<IEnumerable<UserResponseDto>>(users);
            return Result<IEnumerable<UserResponseDto>>.Success(dtos);
        }

        public async Task<Result> DeactivateAccountAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return Result.Failure("User not found.");

            // Domain rule: mark the account as deactivated (soft disable)
            user.IsActive = false;      
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<Result> DeleteMyAccountAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return Result.Failure("User not found.");

            // Delete related data (posts, comments, friendships, saved collections) per domain rules
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // NOTE: implement the cascade deletion carefully in the repository
                // (delete friendships, group memberships, page admin rows, posts, comments, saved)
                _unitOfWork.Users.Delete(user);
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();
                return Result.Success();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}

