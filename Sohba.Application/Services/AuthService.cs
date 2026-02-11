using AutoMapper;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AuthService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<UserResponseDto> RegisterAsync(UserRequestDto registerDto)
        {
            // Check if email already exists
            if (_unitOfWork.Users.EmailExists(registerDto.Email))
                throw new Exception("Email already registered.");

            // Map DTO to User Entity
            var user = _mapper.Map<User>(registerDto);

            /* * [IDENTITY_TRANSITION_NOTE]
             * Currently, we are assigning the password directly to PasswordHash.
             * This is temporary and insecure. Once ASP.NET Core Identity is integrated:
             * 1. This manual assignment will be removed.
             * 2. Identity's 'UserManager.CreateAsync' will handle hashing automatically.
             * 3. Identity's 'PasswordOptions' will handle password complexity rules.
             */
            user.PasswordHash = registerDto.Password;
            user.CreatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Add(user);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<UserResponseDto>(user);
        }

        public async Task<string> LoginAsync(string email, string password)
        {
            // Find user by email or username
            var user = await _unitOfWork.Users.GetByUsernameAsync(email);

            if (user == null) throw new Exception("Invalid credentials.");

            /* * [IDENTITY_TRANSITION_NOTE]
             * Current verification is a simple string comparison.
             * After Identity migration:
             * 1. 'SignInManager.PasswordSignInAsync' will handle secure verification.
             * 2. Features like Account Lockout and Two-Factor Auth will be enabled.
             */
            if (user.PasswordHash != password)
                throw new Exception("Invalid credentials.");

            // Placeholder for JWT Token Generation logic
            return "Temporary_JWT_Token_Will_Be_Generated_Here";
        }
    }
}
