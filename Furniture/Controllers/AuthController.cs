using Furniture.Application.DTOs.Auth;
using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Furniture.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);

            return result.Success ? Ok(result)
                : result.Message == AuthConstants.ErrorMessages.UserAlreadyExists
                    ? Conflict(result)
                    : BadRequest(result);
        }


        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);

            return result.Success ? Ok(result)
                : result.Message == AuthConstants.ErrorMessages.TooManyAttempts
                    ? StatusCode(429, result)
                    : Unauthorized(result);
        }

        // POST api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", errors)
                });
            }

            // Always returns 200 to prevent user enumeration
            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
            return Ok(result);
        }

        // POST api/auth/verify-otp
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto verifyOtpDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", errors)
                });
            }

            var result = await _authService.VerifyOtpAsync(verifyOtpDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", errors)
                });
            }

            var result = await _authService.ResetPasswordAsync(resetPasswordDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST api/auth/change-password (requires JWT)
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var userId = User.FindFirstValue(AuthConstants.Claims.UserId);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new AuthResponseDto { Success = false, Message = "Unauthorized" });

            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST api/auth/confirm-email
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto confirmEmailDto)
        {

            var result = await _authService.ConfirmEmailAsync(confirmEmailDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // POST api/auth/resend-confirmation-otp
        [HttpPost("resend-confirmation-otp")]
        public async Task<IActionResult> ResendConfirmationOtp([FromBody] ResendOtpDto resendOtpDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", errors)
                });
            }

            // Always returns 200 to prevent user enumeration
            var result = await _authService.ResendConfirmationOtpAsync(resendOtpDto);
            return Ok(result);
        }

        // POST api/auth/external-login
        [HttpPost("external-login")]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", errors)
                });
            }

            var result = await _authService.ExternalLoginAsync(dto);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

       
    }
}