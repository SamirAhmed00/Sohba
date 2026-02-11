using Sohba.Application.DTOs.UserAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IAuthService
    {
        Task<UserResponseDto> RegisterAsync(UserRequestDto registerDto);
        Task<string> LoginAsync(string email, string password); // Returns JWT Token
    }
}
