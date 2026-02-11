using Sohba.Application.DTOs.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDto> GetProfileAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UserRequestDto updateDto);
    }
}
