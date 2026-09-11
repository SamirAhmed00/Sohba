using AutoMapper;
using Microsoft.Extensions.Logging;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
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
        private readonly Microsoft.AspNetCore.Identity.UserManager<User> _userManager;
        private readonly ILogger<UserService> _logger;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, IProfileDomainService profileDomainService, IFriendshipRepository friendshipRepository, Microsoft.AspNetCore.Identity.UserManager<User> userManager, ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _profileDomainService = profileDomainService;
            _friendshipRepository = friendshipRepository;
            _userManager = userManager;
            _logger = logger;
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

        public async Task<Result<PagedResult<UserResponseDto>>> GetUsersAdminPagedAsync(string? search, string? status, int page, int pageSize)
        {
            var (users, totalCount) = await _unitOfWork.Users.GetUsersAdminPagedAsync(search, status, page, pageSize);
            var dtos = _mapper.Map<IEnumerable<UserResponseDto>>(users).ToList();

            var pagedResult = new PagedResult<UserResponseDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Result<PagedResult<UserResponseDto>>.Success(pagedResult);
        }

        public async Task<Result> PromoteUserToAdminAsync(Guid targetUserId, Guid actorAdminId)
        {
            var targetUser = await _userManager.FindByIdAsync(targetUserId.ToString());
            if (targetUser == null) return Result.Failure("Target user not found.");

            var actorUser = await _userManager.FindByIdAsync(actorAdminId.ToString());
            if (actorUser == null) return Result.Failure("Acting administrator not found.");

            if (actorUser.Role != UserRole.Owner && actorUser.Role != UserRole.Admin)
                return Result.Failure("Unauthorized: Only administrators can promote users.");

            if (targetUser.Role == UserRole.Owner)
                return Result.Failure("The Owner account cannot be modified.");

            if (targetUser.Role == UserRole.Admin)
                return Result.Failure("User is already an administrator.");

            targetUser.Role = UserRole.Admin;
            targetUser.PromotedByAdminUserId = actorAdminId;

            var result = await _userManager.AddToRoleAsync(targetUser, "Admin");
            if (!result.Succeeded)
                return Result.Failure("Failed to assign Admin role in Identity store.");

            _unitOfWork.Users.Update(targetUser);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("User {TargetUserId} promoted to Admin by Admin {ActorAdminId}", targetUserId, actorAdminId);
            return Result.Success();
        }

        public async Task<Result> DemoteAdminToUserAsync(Guid targetUserId, Guid actorAdminId)
        {
            if (targetUserId == actorAdminId)
                return Result.Failure("Self-demotion is prohibited. You cannot remove your own administrative privileges.");

            var targetUser = await _userManager.FindByIdAsync(targetUserId.ToString());
            if (targetUser == null) return Result.Failure("Target user not found.");

            var actorUser = await _userManager.FindByIdAsync(actorAdminId.ToString());
            if (actorUser == null) return Result.Failure("Acting administrator not found.");

            if (actorUser.Role != UserRole.Owner && actorUser.Role != UserRole.Admin)
                return Result.Failure("Unauthorized.");

            // Strict Owner protection
            if (targetUser.Role == UserRole.Owner)
                return Result.Failure("OWNER ACCOUNT PROTECTED: Normal administrators cannot remove, demote, block, or delete the platform Owner.");

            // Hierarchy rule: A promoted Admin cannot demote the admin who promoted them (unless actor is Owner)
            if (actorUser.Role != UserRole.Owner && actorUser.PromotedByAdminUserId.HasValue && actorUser.PromotedByAdminUserId.Value == targetUserId)
            {
                return Result.Failure("ACTION BLOCKED: You cannot remove or demote the administrator who granted your administrative privileges. A higher-level administrator or the Sohba Owner must perform this action.");
            }

            // Ensure at least one admin remains
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count <= 1)
                return Result.Failure("Action blocked: At least one administrator must remain active on the platform.");

            targetUser.Role = UserRole.User;
            targetUser.PromotedByAdminUserId = null;

            var result = await _userManager.RemoveFromRoleAsync(targetUser, "Admin");
            if (!result.Succeeded)
                return Result.Failure("Failed to remove Admin role in Identity store.");

            _unitOfWork.Users.Update(targetUser);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Admin {TargetUserId} demoted to User by {ActorAdminId}", targetUserId, actorAdminId);
            return Result.Success();
        }

        public async Task<Result> CanManageTargetRoleAsync(Guid targetUserId, Guid actorAdminId)
        {
            if (targetUserId == actorAdminId) return Result.Failure("Self-modification is blocked.");

            var targetUser = await _userManager.FindByIdAsync(targetUserId.ToString());
            if (targetUser == null) return Result.Failure("User not found.");

            var actorUser = await _userManager.FindByIdAsync(actorAdminId.ToString());
            if (actorUser == null) return Result.Failure("Admin not found.");

            var actorIsOwner = await _userManager.IsInRoleAsync(actorUser, "Owner");
            if (actorIsOwner) return Result.Success();

            var targetIsOwner = await _userManager.IsInRoleAsync(targetUser, "Owner");
            if (targetIsOwner) return Result.Failure("Owner is protected.");

            if (actorUser.PromotedByAdminUserId.HasValue && actorUser.PromotedByAdminUserId.Value == targetUserId)
                return Result.Failure("Cannot modify your promoter.");

            return Result.Success();
        }

    }
}

