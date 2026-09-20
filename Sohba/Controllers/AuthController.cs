using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Application.Settings;
using Sohba.Domain.Entities.UserAggregate;

namespace Sohba.Controllers
{
    [EnableRateLimiting("Auth")]
    public class AuthController : Controller
    {
        private const string RefreshTokenCookieName = "Sohba.RefreshToken";

        private readonly IAuthService _authService;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            IAuthService authService,
            SignInManager<User> signInManager,
            ILogger<AuthController> logger,
            UserManager<User> userManager,
            IRefreshTokenService refreshTokenService,
            IJwtService jwtService,
            Microsoft.Extensions.Options.IOptions<JwtSettings> jwtSettings)
        {
            _authService = authService;
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _refreshTokenService = refreshTokenService;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings.Value;
        }

        private CookieOptions RefreshCookieOptions() => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(
        _jwtSettings.RefreshTokenLifetimeDays),
            Path = "/Auth"
        };

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

            // Issue the refresh token for the JWT/SignalR surface. Failure is logged but must
            // never block a successful cookie login.
            var refreshResult = await _refreshTokenService.IssueAsync(
                result.Value.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
            if (refreshResult.IsSuccess)
            {
                Response.Cookies.Append(RefreshTokenCookieName, refreshResult.Value, RefreshCookieOptions());
            }
            else
            {
                _logger.LogWarning("Refresh token issuance failed after login for {Email}: {Error}",
                    loginDto.Email, refreshResult.Error);
            }

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
            // Revoke the refresh token (defense in depth) and clear its cookie.
            var refreshResult = await _refreshTokenService.RevokeAsync(
                Request.Cookies[RefreshTokenCookieName], HttpContext.Connection.RemoteIpAddress?.ToString());
            if (refreshResult.IsFailure)
            {
                _logger.LogWarning("Refresh token revocation during logout failed: {Error}", refreshResult.Error);
            }
            Response.Cookies.Delete(RefreshTokenCookieName);

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



        // REFRESH TOKEN ENDPOINTS (JWT / SignalR surface only)
        // The raw refresh token lives ONLY in an HttpOnly, Secure,
        // SameSite=Lax cookie scoped to /Auth. It is never returned
        // in a response body, never logged, never placed in meta tags.

        [HttpPost]
        [EnableRateLimiting("TokenRefresh")]
        public async Task<IActionResult> Refresh()
        {
            var rawToken = Request.Cookies[RefreshTokenCookieName];
            var requestIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _refreshTokenService.RotateAsync(rawToken, requestIp);
            if (result.IsFailure)
            {
                Response.Cookies.Delete(RefreshTokenCookieName);
                return new JsonResult(BaseResponseDto.FailureResponse(result.Error))
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }

            // Re-check account state: a blocked / deactivated / deleted account must not
            // be able to mint new access tokens.
            var user = await _userManager.FindByIdAsync(result.Value.UserId.ToString());
            if (user == null || user.IsBlocked || !user.IsActive || user.IsDeleted)
            {
                await _refreshTokenService.RevokeAllForUserAsync(result.Value.UserId, requestIp);
                Response.Cookies.Delete(RefreshTokenCookieName);
                return new JsonResult(BaseResponseDto.FailureResponse("Account unavailable."))
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _jwtService.GenerateToken(user, roles);

            Response.Cookies.Append(RefreshTokenCookieName, result.Value.NewRawToken, RefreshCookieOptions());

            return Json(new { success = true, accessToken });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke()
        {
            var result = await _refreshTokenService.RevokeAsync(
                Request.Cookies[RefreshTokenCookieName], HttpContext.Connection.RemoteIpAddress?.ToString());

            Response.Cookies.Delete(RefreshTokenCookieName);
            return Json(new BaseResponseDto { Success = result.IsSuccess, Error = result.Error });
        }
    }
}