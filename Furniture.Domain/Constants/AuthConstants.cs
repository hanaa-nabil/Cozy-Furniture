using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Domain.Constants
{
    public static class AuthConstants
    {
        // Roles
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string User = "User";
            public const string Manager = "Manager";
            public const string Seller = "Seller";  
            public const string Customer = "Customer";
        }

        // JWT Settings
        public static class JwtSettings
        {
            public const string Secret = "JwtSettings:Secret";
            public const string Issuer = "JwtSettings:Issuer";
            public const string Audience = "JwtSettings:Audience";
            public const string ExpiryInMinutes = "JwtSettings:ExpiryInMinutes";
        }

        // OTP Settings
        public static class OtpSettings
        {
            public const string ExpiryInMinutes = "OtpSettings:ExpiryInMinutes";
            public const string Length = "OtpSettings:Length";
        }

        // Email Settings
        public static class EmailSettings
        {
            public const string SmtpHost = "EmailSettings:SmtpHost";
            public const string SmtpPort = "EmailSettings:SmtpPort";
            public const string SmtpUsername = "EmailSettings:SmtpUsername";
            public const string SmtpPassword = "EmailSettings:SmtpPassword";
            public const string FromEmail = "EmailSettings:FromEmail";
            public const string FromName = "EmailSettings:FromName";
        }

        // Confirmation URLs
        public static class ConfirmationUrls
        {
            public const string FrontendConfirmUrl = "ConfirmationUrls:FrontendConfirmUrl";
            public const string ApiConfirmUrl = "ConfirmationUrls:ApiConfirmUrl";
        }

        // Error Messages
        public static class ErrorMessages
        {
            public const string InvalidCredentials = "Invalid email or password";
            public const string UserAlreadyExists = "User already exists";
            public const string RegistrationFailed = "Registration failed";
            public const string UserNotFound = "User not found";
            public const string InvalidRole = "Invalid or unauthorized role";
            public const string RoleAssignmentFailed = "Failed to assign role";
            public const string InvalidOtp = "Invalid or expired OTP";
            public const string OtpExpired = "OTP has expired";
            public const string OtpSendFailed = "Failed to send OTP";
            public const string PasswordResetFailed = "Password reset failed";
            public const string EmailSendFailed = "Failed to send email";
            public const string InvalidToken = "Invalid or expired token";
            public const string EmailNotConfirmed = "Please confirm your email before logging in";
            public const string ImageUploadFailed = "Profile picture upload failed. Please try again.";

            public const string TooManyAttempts = "Too many failed attempts. Please try again in 15 minutes.";
        }

        // Success Messages
        public static class SuccessMessages
        {
            public const string RegistrationSuccess = "User registered successfully";
            public const string LoginSuccess = "Login successful";
            public const string OtpSent = "OTP has been sent to your email";
            public const string OtpVerified = "OTP verified successfully";
            public const string PasswordResetSuccess = "Password reset successfully";
            public const string PasswordChanged = "Password changed successfully";
            public const string EmailConfirmed = "Email confirmed successfully";
            public const string RegistrationPendingConfirmation = "Registration successful. Please check your email to confirm your account";
        }

        // Claims
        public static class Claims
        {
            public const string UserId = "userId";
            public const string Email = "email";
            public const string Role = "role";
        }

        // Email Templates
        public static class EmailTemplates
        {
            public const string OtpSubject = "Your OTP Code";
            public const string OtpBody = "Your OTP code is: {0}. This code will expire in {1} minutes.";
            public const string PasswordResetSubject = "Password Reset Request";
            public const string PasswordResetBody = "Your password reset token is: {0}. This token will expire in {1} minutes.";
            public const string EmailConfirmationSubject = "Confirm Your Email";
            public const string EmailConfirmationBody = "Welcome! Your email confirmation OTP is: {0}. This code will expire in {1} minutes.";
        }

        // Cache Keys
        public static class CacheKeys
        {
            public const string OtpPrefix = "OTP_";
            public const string ResetTokenPrefix = "RESET_TOKEN_";
            public const string LoginAttempts = "login_attempts_";  
            public const string RoleExists = "role_exists_";
        }
    }
}