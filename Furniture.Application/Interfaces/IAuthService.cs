using Furniture.Application.DTOs.Auth;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto); 
        Task<AuthResponseDto> ResendConfirmationOtpAsync(ResendOtpDto resendOtpDto); 
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpDto verifyOtpDto);
        Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
        Task<AuthResponseDto> ExternalLoginAsync(ExternalLoginDto dto);

       // Task<AuthResponseDto> UploadProfilePictureAsync(string userId, IFormFile file);
    }
}
