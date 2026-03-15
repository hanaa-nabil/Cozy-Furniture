using Furniture.Application.DTOs.Auth;
using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
using Furniture.Domain.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IImageService _imageService;
        private readonly ICacheService _cacheService; // ✅ for rate limiting + role cache

        private static readonly HashSet<string> AllowedRegistrationRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            AuthConstants.Roles.Customer,
            AuthConstants.Roles.Seller
        };

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IOtpService otpService,
            IEmailService emailService,
            IEmailTemplateService emailTemplateService,
            IImageService imageService,
            ICacheService cacheService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _otpService = otpService;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
            _imageService = imageService;
            _cacheService = cacheService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            if (!AllowedRegistrationRoles.Contains(registerDto.Role))
                return Fail(AuthConstants.ErrorMessages.InvalidRole);

            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
                return Fail(AuthConstants.ErrorMessages.UserAlreadyExists);

            // Upload profile picture if provided
            string? profilePictureUrl = null;
            if (registerDto.ProfilePicture != null && registerDto.ProfilePicture.Length > 0)
            {
                try
                {
                    profilePictureUrl = await _imageService.UploadImageAsync(
                        registerDto.ProfilePicture, "profiles");
                }
                catch (ArgumentException ex)
                {
                    return Fail(ex.Message);
                }
                catch
                {
                    return Fail(AuthConstants.ErrorMessages.ImageUploadFailed);
                }
            }

            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                IsEmailConfirmed = false,
                ProfilePictureUrl = profilePictureUrl
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Fail(errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, registerDto.Role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return Fail(errors);
            }

            try
            {
                var emailSent = await SendConfirmationEmailAsync(
                    registerDto.Email, registerDto.FirstName ?? "User");

                if (!emailSent)
                {
                    await _userManager.DeleteAsync(user);
                    return Fail(AuthConstants.ErrorMessages.OtpSendFailed);
                }
            }
            catch (Exception ex)
            {
                //  Redis down or SMTP error — rollback so no orphan user left in DB
                await _userManager.DeleteAsync(user);
                return Fail(
                    ex.Message.Contains("redis", StringComparison.OrdinalIgnoreCase)
                        ? "Email service is temporarily unavailable. Please try again later."
                        : AuthConstants.ErrorMessages.OtpSendFailed
                );
            }

            //  No token here — user must confirm email first
            return new AuthResponseDto
            {
                Success = true,
                Message = AuthConstants.SuccessMessages.RegistrationPendingConfirmation,
                Email = user.Email
            };
        }

        public async Task<AuthResponseDto> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto)
        {
            var user = await _userManager.FindByEmailAsync(confirmEmailDto.Email);
            if (user == null)
                return Fail(AuthConstants.ErrorMessages.UserNotFound);

            bool isOtpValid;
            try
            {
                isOtpValid = await _otpService.ValidateOtpAsync(
                    confirmEmailDto.Email, confirmEmailDto.Otp);
            }
            catch (Exception ex)
            {
                return Fail(
                    ex.Message.Contains("redis", StringComparison.OrdinalIgnoreCase)
                        ? "Verification service is temporarily unavailable. Please try again later."
                        : AuthConstants.ErrorMessages.InvalidOtp
                );
            }

            if (!isOtpValid)
                return Fail(AuthConstants.ErrorMessages.InvalidOtp);

            user.IsEmailConfirmed = true;
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            // Invalidate OTP 
            try
            {
                await _otpService.InvalidateOtpAsync(confirmEmailDto.Email);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var (token, expiresAt) = GenerateJwtToken(user, userRoles);

            return new AuthResponseDto
            {
                Success = true,
                Message = AuthConstants.SuccessMessages.LoginSuccess,
                Token = token,
                ExpiresAt = expiresAt,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = userRoles.ToList(),
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }

        public async Task<AuthResponseDto> ResendConfirmationOtpAsync(ResendOtpDto resendOtpDto)
        {
            var user = await _userManager.FindByEmailAsync(resendOtpDto.Email);

            // Same response whether user doesn't exist or is already confirmed — prevents enumeration
            if (user == null || user.IsEmailConfirmed)
                return new AuthResponseDto { Success = true, Message = AuthConstants.SuccessMessages.OtpSent };

            var emailSent = await SendConfirmationEmailAsync(resendOtpDto.Email, user.FirstName ?? "User");
            if (!emailSent)
                return Fail(AuthConstants.ErrorMessages.OtpSendFailed);

            return new AuthResponseDto { Success = true, Message = AuthConstants.SuccessMessages.OtpSent };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var attemptKey = $"{AuthConstants.CacheKeys.LoginAttempts}{loginDto.Email}";
            var attempts = await _cacheService.GetAsync<int?>(attemptKey) ?? 0;

            if (attempts >= 5)
                return Fail(AuthConstants.ErrorMessages.TooManyAttempts);

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                await _cacheService.SetAsync(attemptKey, attempts + 1, TimeSpan.FromMinutes(15));
                return Fail(AuthConstants.ErrorMessages.InvalidCredentials);
            }

            if (!user.IsEmailConfirmed)
                return Fail(AuthConstants.ErrorMessages.EmailNotConfirmed);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
            {
                await _cacheService.SetAsync(attemptKey, attempts + 1, TimeSpan.FromMinutes(15));
                return Fail(AuthConstants.ErrorMessages.InvalidCredentials);
            }

            await _cacheService.RemoveAsync(attemptKey);

            var userRoles = await _userManager.GetRolesAsync(user);
            var (token, expiresAt) = GenerateJwtToken(user, userRoles);

            return new AuthResponseDto
            {
                Success = true,
                Message = AuthConstants.SuccessMessages.LoginSuccess,
                Token = token,
                ExpiresAt = expiresAt,
                Email = user.Email,
                FirstName = user.FirstName,  
                LastName = user.LastName,  
                Roles = userRoles.ToList(),
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }

        public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);

            if (user == null)
                return new AuthResponseDto { Success = true, Message = AuthConstants.SuccessMessages.OtpSent };

            var otp = await _otpService.GenerateOtpAsync(forgotPasswordDto.Email);
            var expiryMinutes = _configuration[AuthConstants.OtpSettings.ExpiryInMinutes] ?? "5";
            var emailBody = _emailTemplateService.GenerateOtpEmailTemplate(otp, user.FirstName ?? "User", expiryMinutes);

            var emailSent = await _emailService.SendHtmlEmailAsync(
                forgotPasswordDto.Email,
                AuthConstants.EmailTemplates.OtpSubject,
                emailBody);

            if (!emailSent)
                return Fail(AuthConstants.ErrorMessages.OtpSendFailed);

            return new AuthResponseDto { Success = true, Message = AuthConstants.SuccessMessages.OtpSent };
        }

        public async Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpDto verifyOtpDto)
        {
            var user = await _userManager.FindByEmailAsync(verifyOtpDto.Email);
            if (user == null)
                return Fail(AuthConstants.ErrorMessages.UserNotFound);

            bool isValid;
            try
            {
                isValid = await _otpService.ValidateOtpAsync(verifyOtpDto.Email, verifyOtpDto.Otp);
            }
            catch (Exception ex)
            {
                return Fail(
                    ex.Message.Contains("redis", StringComparison.OrdinalIgnoreCase)
                        ? "Verification service is temporarily unavailable. Please try again later."
                        : AuthConstants.ErrorMessages.InvalidOtp
                );
            }

            if (!isValid)
                return Fail(AuthConstants.ErrorMessages.InvalidOtp);

            // Confirm email
            user.IsEmailConfirmed = true;
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            try { await _otpService.InvalidateOtpAsync(verifyOtpDto.Email); }
            catch {  }

            var userRoles = await _userManager.GetRolesAsync(user);
            var (token, expiresAt) = GenerateJwtToken(user, userRoles);

            return new AuthResponseDto
            {
                Success = true,
                Message = AuthConstants.SuccessMessages.OtpVerified,
                Token = token,
                ExpiresAt = expiresAt,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = userRoles.ToList(),
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }

        public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var isOtpValid = await _otpService.ValidateOtpAsync(resetPasswordDto.Email, resetPasswordDto.Otp);
            if (!isOtpValid)
                return Fail(AuthConstants.ErrorMessages.InvalidOtp);

            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
                return Fail(AuthConstants.ErrorMessages.UserNotFound);

            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                return Fail(AuthConstants.ErrorMessages.PasswordResetFailed);

            var addResult = await _userManager.AddPasswordAsync(user, resetPasswordDto.NewPassword);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return Fail(errors);
            }

            await _otpService.InvalidateOtpAsync(resetPasswordDto.Email);

            return new AuthResponseDto { Success = true, Message = AuthConstants.SuccessMessages.PasswordResetSuccess };
        }

        public async Task<AuthResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Fail(AuthConstants.ErrorMessages.UserNotFound);

            var result = await _userManager.ChangePasswordAsync(
                user,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Fail(errors);
            }

            return new AuthResponseDto { Success = true, Message = AuthConstants.SuccessMessages.PasswordChanged };
        }

        public async Task<AuthResponseDto> ExternalLoginAsync(ExternalLoginDto dto)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);
            }
            catch
            {
                return Fail(AuthConstants.ErrorMessages.InvalidToken);
            }

            if (payload == null)
                return Fail(AuthConstants.ErrorMessages.InvalidToken);

            var user = await _userManager.FindByEmailAsync(payload.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    IsEmailConfirmed = true,
                    EmailConfirmed = true,
                    ProfilePictureUrl = payload.Picture
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Fail(errors);
                }

                await _userManager.AddToRoleAsync(user, AuthConstants.Roles.Customer);
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var (token, expiresAt) = GenerateJwtToken(user, userRoles);

            return new AuthResponseDto
            {
                Success = true,
                Message = AuthConstants.SuccessMessages.LoginSuccess,
                Token = token,
                ExpiresAt = expiresAt,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = userRoles.ToList(),
                ProfilePictureUrl = user.ProfilePictureUrl
            };
        }

        public async Task<AuthResponseDto> UploadProfilePictureAsync(string userId, IFormFile file)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Fail(AuthConstants.ErrorMessages.UserNotFound);

            try
            {
                var url = await _imageService.UploadImageAsync(file, "profiles");
                user.ProfilePictureUrl = url;
                await _userManager.UpdateAsync(user);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Profile picture uploaded successfully",
                    ProfilePictureUrl = url
                };
            }
            catch (ArgumentException ex)
            {
                return Fail(ex.Message);
            }
            catch
            {
                return Fail(AuthConstants.ErrorMessages.ImageUploadFailed);
            }
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private static AuthResponseDto Fail(string message) =>
            new AuthResponseDto { Success = false, Message = message };


        private async Task<bool> SendConfirmationEmailAsync(string email, string firstName)
        {
            var otp = await _otpService.GenerateOtpAsync(email);
            var expiryMinutes = _configuration[AuthConstants.OtpSettings.ExpiryInMinutes] ?? "5";
            var confirmUrl = _configuration[AuthConstants.ConfirmationUrls.FrontendConfirmUrl]
                                    ?? "https://yourapp.com/confirm-email";

            confirmUrl = $"{confirmUrl}?email={Uri.EscapeDataString(email)}&otp={otp}";

            var emailBody = _emailTemplateService.GenerateEmailConfirmationTemplate(
                otp, firstName, expiryMinutes, confirmUrl);

            return await _emailService.SendHtmlEmailAsync(
                email,
                AuthConstants.EmailTemplates.EmailConfirmationSubject,
                emailBody);
        }

        private (string Token, DateTime ExpiresAt) GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(AuthConstants.Claims.UserId, user.Id),
                new Claim(AuthConstants.Claims.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration[AuthConstants.JwtSettings.Secret]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddYears(100);
            var token = new JwtSecurityToken(
                issuer: _configuration[AuthConstants.JwtSettings.Issuer],
                audience: _configuration[AuthConstants.JwtSettings.Audience],
                claims: claims,
                expires: expiry,
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
        }
    }
}