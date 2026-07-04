using Sohba.Application.DTOs.UserAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IUserService
    {

        // Profile
        Task<Result<UserResponseDto>> GetProfileAsync(Guid userId);
        Task<Result<bool>> UpdateProfileAsync(Guid userId, UserRequestDto updateDto);

       // Get profile with privacy enforcement(current user for permission check)
        Task<Result<UserResponseDto>> GetProfileAsync(Guid userId, Guid currentUserId);

        // Admin
        Task<Result<IEnumerable<UserResponseDto>>> GetAllUsersAsync(); 
        Task<Result<bool>> DeleteUserAsync(Guid userId);
        Task<Result<IEnumerable<UserResponseDto>>> GetUsersByStatusAsync(string status);
    }
}
