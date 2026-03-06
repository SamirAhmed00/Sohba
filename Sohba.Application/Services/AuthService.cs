using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Domain.Common;
using Sohba.Domain.Entities.UserAggregate;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly JwtService _jwtService;
    private readonly IMapper _mapper;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        JwtService jwtService,
        IMapper mapper)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
            return Result<AuthResponseDto>.Failure("Email already registered.");

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
            return Result<AuthResponseDto>.Failure(errors);
        }

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
            return Result<AuthResponseDto>.Failure("Invalid email or password.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return Result<AuthResponseDto>.Failure("Account locked out. Try again later.");
        if (!result.Succeeded)
            return Result<AuthResponseDto>.Failure("Invalid email or password.");

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
}