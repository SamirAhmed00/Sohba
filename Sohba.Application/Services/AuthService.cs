    using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Interfaces;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly JwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        JwtService jwtService,
        IMapper mapper,
        IEmailService emailService,
        ILogger<AuthService> logger,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed: email {Email} already registered", registerDto.Email);
            return Result<AuthResponseDto>.Failure("Email already registered.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = registerDto.Name,
            UserName = registerDto.Email,
            Email = registerDto.Email,
            DateOfBirth = registerDto.DateOfBirth,
            Bio = registerDto.Bio ?? "",
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed for email {Email}: {Errors}", registerDto.Email, errors);
            return Result<AuthResponseDto>.Failure(errors);
        }

        _logger.LogInformation("User registered successfully: {UserId}, email {Email}", user.Id, registerDto.Email);

        await _userManager.AddToRoleAsync(user, "User");
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtService.GenerateToken(user, roles);

        var response = _mapper.Map<AuthResponseDto>(user);
        response.Token = token;
        response.Roles = roles.ToList();

        return Result<AuthResponseDto>.Success(response);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
        {
            var deletedUser = await _unitOfWork.Users.GetByEmailIncludingDeletedAsync(loginDto.Email);
            if (deletedUser != null && deletedUser.IsDeleted)
            {
                _logger.LogWarning("Login rejected: account is deleted for email {Email}", loginDto.Email);
                return Result<AuthResponseDto>.Failure("This account has been deleted and is no longer available.");
            }


            _logger.LogWarning("Login failed: user not found for email {Email}", loginDto.Email);
            return Result<AuthResponseDto>.Failure("Invalid email or password.");
        }

        if (user.IsBlocked)
        {
            _logger.LogWarning("Login rejected: account is blocked for user {UserId} ({Email})", user.Id, loginDto.Email);
            return Result<AuthResponseDto>.Failure("Your account has been blocked. Please contact support.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Login blocked: account locked out for user {UserId} ({Email})", user.Id, loginDto.Email);
            return Result<AuthResponseDto>.Failure("Account locked out. Try again later.");
        }
        if (!result.Succeeded)
        {
            _logger.LogWarning("Login failed: invalid password for user {UserId} ({Email})", user.Id, loginDto.Email);
            return Result<AuthResponseDto>.Failure("Invalid email or password.");
        }

        _logger.LogInformation("User logged in: {UserId} ({Email})", user.Id, loginDto.Email);

        // Sign in with cookie (for MVC)
        if (loginDto.RememberMe)
        {
            await _signInManager.SignInAsync(user, isPersistent: true);
        }
        else
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtService.GenerateToken(user, roles);

        var response = _mapper.Map<AuthResponseDto>(user);
        response.Token = token;
        response.Roles = roles.ToList();

        return Result<AuthResponseDto>.Success(response);
    }

    public async Task<Result> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return Result.Success();
    }

    public async Task<Result<AuthResponseDto>> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Result<AuthResponseDto>.Failure("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        var response = _mapper.Map<AuthResponseDto>(user);
        response.Roles = roles.ToList();

        return Result<AuthResponseDto>.Success(response);
    }

    public async Task<Result> ForgotPasswordAsync(string email, string fallbackUrl)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for unknown email {Email}", email);
            return Result.Failure("User not found.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        _logger.LogInformation("Password reset token generated for user {UserId} ({Email})", user.Id, email);

        // Instead of hardcoding base url, we ideally want to construct callback URL properly. The controller will pass it. 
        var resetLink = $"{fallbackUrl}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        await _emailService.SendEmailAsync(
            email, 
            "Sohba Password Reset", 
            $"Please reset your password by clicking here: <a href='{resetLink}'>Reset Password</a>", 
            isHtml: true);

        _logger.LogInformation("Password reset email sent to {Email}", email);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Password reset failed: user not found for email {Email}", email);
            return Result.Failure("User not found.");
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Password reset failed for user {UserId} ({Email}): {Errors}", user.Id, email, errors);
            return Result.Failure(errors);
        }

        _logger.LogInformation("Password reset successful for user {UserId} ({Email})", user.Id, email);
        return Result.Success();
    }
}