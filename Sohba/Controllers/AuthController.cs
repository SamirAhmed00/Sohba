using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Entities.UserAggregate;
using System.Security.Claims;

namespace Sohba.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<User> _signInManager; 
        private readonly UserManager<User> _userManager;     

        public AuthController(
            IAuthService authService,
            SignInManager<User> signInManager,
            UserManager<User> userManager)
        {
            _authService = authService;
            _signInManager = signInManager;
            _userManager = userManager;
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
            Console.WriteLine($"🔐 Login attempt - Email: {loginDto.Email}, RememberMe: {loginDto.RememberMe}");

            if (!ModelState.IsValid)
                return View(loginDto);

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(loginDto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                loginDto.Password,
                loginDto.RememberMe,
                lockoutOnFailure: true);

            Console.WriteLine($"📊 SignIn result: {result.Succeeded}, RememberMe: {loginDto.RememberMe}");

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(loginDto);
            }


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
                ModelState.AddModelError("", result.Error);
                return View(registerDto);
            }

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
            try
            {
                if (!ModelState.IsValid)
                    return Json(BaseResponseDto<object>.FailureResponse("Invalid email address."));

                // Fallback URL pointing back to Auth/ResetPassword
                var fallbackUrl = Url.Action("ResetPassword", "Auth", null, Request.Scheme);

                var result = await _authService.ForgotPasswordAsync(model.Email, fallbackUrl);

                // For security reasons, don't reveal if the user exists
                return Json(BaseResponseDto<object>.SuccessResponse(null));
            }
            catch (Exception ex)
            {
                return Json(BaseResponseDto<object>.FailureResponse($"An error occurred: {ex.Message}"));
            }
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
            try
            {
                if (!ModelState.IsValid)
                    return Json(BaseResponseDto<object>.FailureResponse("Invalid payload."));

                var result = await _authService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);

                if (result.IsSuccess)
                    return Json(BaseResponseDto<object>.SuccessResponse(null));

                return Json(BaseResponseDto<object>.FailureResponse(result.Error));
            }
            catch (Exception ex)
            {
                return Json(BaseResponseDto<object>.FailureResponse($"An error occurred: {ex.Message}"));
            }
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