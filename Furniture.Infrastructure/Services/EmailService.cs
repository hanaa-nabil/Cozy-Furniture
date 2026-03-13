using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService, IEmailTemplateService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            return await SendInternalAsync(toEmail, subject, body, isHtml: false);
        }

        public async Task<bool> SendHtmlEmailAsync(string toEmail, string subject, string htmlBody)
        {
            return await SendInternalAsync(toEmail, subject, htmlBody, isHtml: true);
        }

        public string GenerateEmailConfirmationTemplate(
            string otp,
            string userName,
            string expiryMinutes,
            string confirmUrl)
        {
            var safeUserName = WebUtility.HtmlEncode(userName);
            var safeOtp = WebUtility.HtmlEncode(otp);
            var safeExpiry = WebUtility.HtmlEncode(expiryMinutes);
            var safeConfirmUrl = confirmUrl; // already built with Uri.EscapeDataString in AuthService

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Confirmation</title>
</head>
<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='min-height: 100vh; padding: 40px 20px;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='max-width: 600px; background: rgba(255, 255, 255, 0.98); border-radius: 24px; box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3); overflow: hidden;'>

                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 50px 40px; text-align: center;'>
                            <span style='font-size: 48px;'>✉️</span>
                            <h1 style='margin: 20px 0 0 0; color: #ffffff; font-size: 32px; font-weight: 700; letter-spacing: -0.5px;'>Welcome Aboard!</h1>
                            <p style='margin: 10px 0 0 0; color: rgba(255, 255, 255, 0.95); font-size: 16px;'>We're excited to have you here</p>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style='padding: 50px 40px;'>
                            <p style='margin: 0 0 20px 0; color: #4a5568; font-size: 16px; line-height: 1.6;'>
                                Hi <strong style='color: #2d3748;'>{safeUserName}</strong>,
                            </p>
                            <p style='margin: 0 0 30px 0; color: #4a5568; font-size: 16px; line-height: 1.6;'>
                                Thank you for joining us! To complete your registration and unlock all features, please verify your email address using the code below:
                            </p>

                            <!-- OTP Box -->
                            <table width='100%' cellpadding='0' cellspacing='0' style='margin: 0 0 30px 0;'>
                                <tr>
                                    <td align='center' style='background: linear-gradient(135deg, #f6f8fb 0%, #e9ecf5 100%); border: 2px dashed #667eea; border-radius: 16px; padding: 30px;'>
                                        <p style='margin: 0 0 15px 0; color: #718096; font-size: 13px; text-transform: uppercase; letter-spacing: 1px; font-weight: 600;'>Your Verification Code</p>
                                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 12px; padding: 20px; display: inline-block;'>
                                            <span style='font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #ffffff; font-family: ""Courier New"", monospace;'>{safeOtp}</span>
                                        </div>
                                        <p style='margin: 15px 0 0 0; color: #e53e3e; font-size: 13px; font-weight: 500;'>
                                            ⏱️ Expires in {safeExpiry} minutes
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <!-- CTA Button -->
                            <table width='100%' cellpadding='0' cellspacing='0' style='margin: 0 0 30px 0;'>
                                <tr>
                                    <td align='center'>
                                        <a href='{safeConfirmUrl}' style='display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; text-decoration: none; padding: 16px 40px; border-radius: 12px; font-weight: 600; font-size: 16px; box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);'>
                                            Verify Email Address
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Info Box -->
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td style='background: #f7fafc; border-left: 4px solid #667eea; border-radius: 8px; padding: 20px;'>
                                        <p style='margin: 0 0 10px 0; color: #2d3748; font-size: 14px; font-weight: 600;'>💡 Quick Tip:</p>
                                        <p style='margin: 0; color: #4a5568; font-size: 14px; line-height: 1.6;'>
                                            Copy and paste the code above, or click the button to verify automatically. If you didn't create an account, you can safely ignore this email.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background: #f7fafc; padding: 30px 40px; border-top: 1px solid #e2e8f0;'>
                            <p style='margin: 0 0 10px 0; color: #718096; font-size: 13px; text-align: center; line-height: 1.6;'>
                                Need help? Contact us at <a href='mailto:support@yourapp.com' style='color: #667eea; text-decoration: none; font-weight: 600;'>support@yourapp.com</a>
                            </p>
                            <p style='margin: 0; color: #a0aec0; font-size: 12px; text-align: center;'>
                                © 2026 Your App Name. All rights reserved.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Single SMTP send method — eliminates the duplicated setup code that
        /// previously existed in SendEmailAsync and SendHtmlEmailAsync.
        /// </summary>
        private async Task<bool> SendInternalAsync(
            string toEmail,
            string subject,
            string body,
            bool isHtml)
        {
            try
            {
                var smtpHost = _configuration[AuthConstants.EmailSettings.SmtpHost];
                var smtpPort = Convert.ToInt32(_configuration[AuthConstants.EmailSettings.SmtpPort]);
                var smtpUsername = _configuration[AuthConstants.EmailSettings.SmtpUsername];
                var smtpPassword = _configuration[AuthConstants.EmailSettings.SmtpPassword];
                var fromEmail = _configuration[AuthConstants.EmailSettings.FromEmail];
                var fromName = _configuration[AuthConstants.EmailSettings.FromName];

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail!, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (SmtpException ex)
            {
                // Log the exception here via ILogger if you have one injected
                // e.g. _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                _ = ex;
                return false;
            }
        }
        public string GenerateOtpEmailTemplate(string otp, string userName, string expiryMinutes)
        {
            var safeUserName = WebUtility.HtmlEncode(userName);
            var safeOtp = WebUtility.HtmlEncode(otp);
            var safeExpiry = WebUtility.HtmlEncode(expiryMinutes);

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Your OTP Code</title>
</head>
<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='min-height: 100vh; padding: 40px 20px;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='max-width: 600px; background: rgba(255, 255, 255, 0.98); border-radius: 24px; box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3); overflow: hidden;'>

                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 50px 40px; text-align: center;'>
                            <span style='font-size: 48px;'>🔐</span>
                            <h1 style='margin: 20px 0 0 0; color: #ffffff; font-size: 32px; font-weight: 700; letter-spacing: -0.5px;'>Password Reset</h1>
                            <p style='margin: 10px 0 0 0; color: rgba(255, 255, 255, 0.95); font-size: 16px;'>We received a request to reset your password</p>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style='padding: 50px 40px;'>
                            <p style='margin: 0 0 20px 0; color: #4a5568; font-size: 16px; line-height: 1.6;'>
                                Hi <strong style='color: #2d3748;'>{safeUserName}</strong>,
                            </p>
                            <p style='margin: 0 0 30px 0; color: #4a5568; font-size: 16px; line-height: 1.6;'>
                                Use the verification code below to reset your password. If you didn't request this, you can safely ignore this email.
                            </p>

                            <!-- OTP Box -->
                            <table width='100%' cellpadding='0' cellspacing='0' style='margin: 0 0 30px 0;'>
                                <tr>
                                    <td align='center' style='background: linear-gradient(135deg, #f6f8fb 0%, #e9ecf5 100%); border: 2px dashed #667eea; border-radius: 16px; padding: 30px;'>
                                        <p style='margin: 0 0 15px 0; color: #718096; font-size: 13px; text-transform: uppercase; letter-spacing: 1px; font-weight: 600;'>Your Reset Code</p>
                                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 12px; padding: 20px; display: inline-block;'>
                                            <span style='font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #ffffff; font-family: ""Courier New"", monospace;'>{safeOtp}</span>
                                        </div>
                                        <p style='margin: 15px 0 0 0; color: #e53e3e; font-size: 13px; font-weight: 500;'>
                                            ⏱️ Expires in {safeExpiry} minutes
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Warning Box -->
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td style='background: #fff5f5; border-left: 4px solid #fc8181; border-radius: 8px; padding: 20px;'>
                                        <p style='margin: 0 0 8px 0; color: #c53030; font-size: 14px; font-weight: 600;'>
                                            🔒 Security Notice:
                                        </p>
                                        <p style='margin: 0; color: #742a2a; font-size: 14px; line-height: 1.6;'>
                                            Never share this code with anyone. Our team will never ask for your OTP. If you didn't request a password reset, please secure your account immediately.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background: #f7fafc; padding: 30px 40px; border-top: 1px solid #e2e8f0;'>
                            <p style='margin: 0 0 10px 0; color: #718096; font-size: 13px; text-align: center; line-height: 1.6;'>
                                Need help? Contact us at <a href='mailto:support@yourapp.com' style='color: #667eea; text-decoration: none; font-weight: 600;'>support@yourapp.com</a>
                            </p>
                            <p style='margin: 0; color: #a0aec0; font-size: 12px; text-align: center;'>
                                © 2026 Your App Name. All rights reserved.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}