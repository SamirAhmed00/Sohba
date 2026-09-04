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
            var isBlockedByOwner = await _friendshipRepository.IsUserBlockedAsync(userId, currentUserId);

            var isPrivateAccount = user.IsPrivateAccount;

            var canView = _profileDomainService.CanViewProfile(
                currentUserId,
                userId,
                isPrivateAccount,
                isFriend,
                isBlockedByOwner
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

            user.Name = updateDto.Name;
            user.Bio = updateDto.Bio;

            if (updateDto.ProfilePictureUrl != null)
            {
                user.ProfilePictureUrl = updateDto.ProfilePictureUrl;
            }

            if (updateDto.BackgroundImageUrl != null)
            {
                user.BackgroundImageUrl = updateDto.BackgroundImageUrl;
            }

            _unitOfWork.Users.Update(user);
            var affectedRows = await _unitOfWork.CompleteAsync();

            return Result<bool>.Success(affectedRows >= 0);
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
            user.IsActive = false;
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
                    filteredUsers = allUsers.Where(u => !u.IsDeleted && !u.IsBlocked && u.IsActive);
                    break;

                case "deactivated":
                    filteredUsers = allUsers.Where(u => !u.IsDeleted && !u.IsBlocked && !u.IsActive);
                    break;

                case "blocked":
                    filteredUsers = allUsers.Where(u => u.IsBlocked);
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

            user.IsActive = false;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<Result> ReactivateAccountAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return Result.Failure("User not found.");

            user.IsActive = true;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }


        public async Task<Result> DeleteMyAccountAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return Result.Failure("User not found.");

            user.IsDeleted = true;
            user.IsActive = false;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }


        public async Task<Result> BlockUserAccountAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return Result.Failure("User not found.");

            user.IsBlocked = true;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<Result> UnblockUserAccountAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return Result.Failure("User not found.");

            user.IsBlocked = false;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }
    }
}

