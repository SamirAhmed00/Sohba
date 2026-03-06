using Microsoft.AspNetCore.Identity;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class UserSettingsService : IUserSettingsService
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public UserSettingsService(UserManager<User> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UserSettingsDto>> GetSettingsAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result<UserSettingsDto>.Failure("User not found");

            // TODO: Load user settings from database (you need to add settings table)
            var settings = new UserSettingsDto
            {
                Email = user.Email,
                Name = user.Name,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsPrivateAccount = false, // Default
                ShowActivityStatus = true,
                EmailNotifications = true,
                PushNotifications = true,
                WeeklyDigest = false,
                LastPasswordChanged = null // You need to track this
            };

            return Result<UserSettingsDto>.Success(settings);
        }

        public async Task<Result> UpdateSettingsAsync(Guid userId, UserSettingsDto settings)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found");

            user.Name = settings.Name;
            user.Bio = settings.Bio;
            user.ProfilePictureUrl = settings.ProfilePictureUrl;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

            // TODO: Update other settings in settings table

            return Result.Success();
        }

        public async Task<Result> UpdateEmailAsync(Guid userId, string newEmail)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found");

            var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
            var result = await _userManager.ChangeEmailAsync(user, newEmail, token);

            if (!result.Succeeded)
                return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

            return Result.Success();
        }

        public async Task<Result> UpdatePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
                return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

            return Result.Success();
        }

        public async Task<Result> DeactivateAccountAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found");

            user.IsDeleted = true;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

            return Result.Success();
        }

        public async Task<Result> DeleteAccountAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

            return Result.Success();
        }
    }

}
