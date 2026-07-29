using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Entities.UserAggregate;
using System.Security.Claims;

namespace Sohba.Controllers
{
    [EnableRateLimiting("Auth")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<User> _signInManager; 
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            


            if (!ModelState.IsValid)
                return View(loginDto);

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogInformation("Login attempt for email {Email}, RememberMe: {RememberMe}", loginDto.Email, loginDto.RememberMe);
                ModelState.AddModelError("", "Invalid email or password.");
                return View(loginDto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                loginDto.Password,
                loginDto.RememberMe,
                lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login blocked: account locked out for user {UserId} ({Email})", user.Id, loginDto.Email);
                ModelState.AddModelError("", "Account locked out. Try again later.");
                return View(loginDto);
            }


            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login attempt: invalid password for user {UserId} ({Email})", user.Id, loginDto.Email);
                ModelState.AddModelError("", "Invalid email or password.");
                return View(loginDto);
            }

            _logger.LogInformation("SignIn result for email {Email}: Succeeded={Succeeded}, IsLockedOut={IsLockedOut}, IsNotAllowed={IsNotAllowed}",loginDto.Email, result.Succeeded, result.IsLockedOut, result.IsNotAllowed);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return View(registerDto);

            var result = await _authService.RegisterAsync(registerDto);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Registration failed for email {Email}: {Error}", registerDto.Email, result.Error);
                ModelState.AddModelError("", result.Error);
                return View(registerDto);
            }

            _logger.LogInformation("New user registered: email {Email}, name {Name}", registerDto.Email, registerDto.Name);
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync(); 
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            // -- TO CHECK : The Old Code Using Try Catch --
            //try
            //{
            //    if (!ModelState.IsValid)
            //        return Json(BaseResponseDto<object>.FailureResponse("Invalid email address."));

            //    // Fallback URL pointing back to Auth/ResetPassword
            //    var fallbackUrl = Url.Action("ResetPassword", "Auth", null, Request.Scheme);

            //    var result = await _authService.ForgotPasswordAsync(model.Email, fallbackUrl);

            //    // For security reasons, don't reveal if the user exists
            //    return Json(BaseResponseDto<object>.SuccessResponse(null));
            //}
            //catch (Exception ex)
            //{
            //    return Json(BaseResponseDto<object>.FailureResponse($"An error occurred: {ex.Message}"));
            //}

            if (!ModelState.IsValid)
                return Json(BaseResponseDto<object>.FailureResponse("Invalid email address."));

            _logger.LogInformation("Password reset requested for email {Email}", model.Email);

            var fallbackUrl = Url.Action("ResetPassword", "Auth", null, Request.Scheme);

            var result = await _authService.ForgotPasswordAsync(model.Email, fallbackUrl);

            if (result.IsFailure)
            {
                _logger.LogWarning("Password reset failed for email {Email}: {Error}", model.Email, result.Error);
            }

            return Json(BaseResponseDto<object>.SuccessResponse(null));
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return BadRequest("Invalid password reset token.");

            var model = new ResetPasswordDto { Email = email, Token = token };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            // -- TO CHECK : The Old Code Using Try Catch --
            //try
            //{
            //    if (!ModelState.IsValid)
            //        return Json(BaseResponseDto<object>.FailureResponse("Invalid payload."));

            //    var result = await _authService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);

            //    if (result.IsSuccess)
            //        return Json(BaseResponseDto<object>.SuccessResponse(null));

            //    return Json(BaseResponseDto<object>.FailureResponse(result.Error));
            //}
            //catch (Exception ex)
            //{
            //    return Json(BaseResponseDto<object>.FailureResponse($"An error occurred: {ex.Message}"));
            //}

            if (!ModelState.IsValid)
                return Json(BaseResponseDto<object>.FailureResponse("Invalid payload."));

            _logger.LogInformation("Password reset attempt for email {Email}", model.Email);

            var result = await _authService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Password reset successful for email {Email}", model.Email);
                return Json(BaseResponseDto<object>.SuccessResponse(null));
            }


            _logger.LogWarning("Password reset failed for email {Email}: {Error}", model.Email, result.Error);
            return Json(BaseResponseDto<object>.FailureResponse(result.Error));            
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}