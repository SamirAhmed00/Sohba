using Sohba.Application.DTOs.UserAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IUserSettingsService
    {
        Task<Result<UserSettingsDto>> GetSettingsAsync(Guid userId);
        Task<Result> UpdateSettingsAsync(Guid userId, UserSettingsDto settings);
        Task<Result> UpdateEmailAsync(Guid userId, string newEmail);
        Task<Result> UpdatePasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task<Result> DeactivateAccountAsync(Guid userId);
        Task<Result> DeleteAccountAsync(Guid userId);
    }
}
