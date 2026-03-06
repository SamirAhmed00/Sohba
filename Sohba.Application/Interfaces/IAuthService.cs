using Sohba.Application.DTOs.UserAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);
        Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto);
        Task<Result> LogoutAsync();
        Task<Result<AuthResponseDto>> GetCurrentUserAsync(Guid userId);

    }
}
