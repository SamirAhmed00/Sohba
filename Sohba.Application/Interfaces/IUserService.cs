using Sohba.Application.DTOs.UserAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IUserService
    {
        Task<Result<UserResponseDto>> GetProfileAsync(Guid userId);
        Task<Result<bool>> UpdateProfileAsync(Guid userId, UserRequestDto updateDto);
    }
}
