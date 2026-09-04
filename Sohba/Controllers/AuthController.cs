using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Entities.UserAggregate;

namespace Sohba.Controllers
{
    [EnableRateLimiting("Auth")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            SignInManager<User> signInManager,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto loginDto, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(loginDto);

            var result = await _authService.LoginAsync(loginDto);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Login failed for email {Email}: {Error}", loginDto.Email, result.Error);
                ModelState.AddModelError("", result.Error);
                return View(loginDto);
            }

            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

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
            TempData["SuccessMessage"] = "Account created successfully! Please sign in.";
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
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _logger.LogInformation("Password reset requested for email {Email}", model.Email);

            var fallbackUrl = Url.Action("ResetPassword", "Auth", null, Request.Scheme);

            var result = await _authService.ForgotPasswordAsync(model.Email, fallbackUrl!);

            if (result.IsFailure)
            {
                _logger.LogWarning("Password reset dispatch failed for email {Email}: {Error}", model.Email, result.Error);
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            ViewBag.Message = "If your email is registered, you will receive a password reset link shortly.";
            return View();
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
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _logger.LogInformation("Password reset attempt for email {Email}", model.Email);

            var result = await _authService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Password reset successful for email {Email}", model.Email);
                TempData["SuccessMessage"] = "Your password has been reset successfully. Please sign in.";
                return RedirectToAction("Login");
            }

            _logger.LogWarning("Password reset failed for email {Email}: {Error}", model.Email, result.Error);
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}