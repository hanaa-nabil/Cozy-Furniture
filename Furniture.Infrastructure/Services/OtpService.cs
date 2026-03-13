using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
using Furniture.Domain.Entities;
using Furniture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Infrastructure.Services
{
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public OtpService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> GenerateOtpAsync(string email)
        {
            // Invalidate any existing OTP
            await InvalidateOtpAsync(email);

            // Generate random 6-digit OTP
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();

            var expiryMinutes = Convert.ToInt32(_configuration[AuthConstants.OtpSettings.ExpiryInMinutes] ?? "5");

            var otpCode = new OtpCode
            {
                Email = email,
                Code = otp,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                IsUsed = false
            };

            _context.OtpCodes.Add(otpCode);
            await _context.SaveChangesAsync();

            return otp;
        }

        public async Task<bool> ValidateOtpAsync(string email, string otp)
        {
            var otpCode = await _context.OtpCodes
                .Where(o => o.Email == email && o.Code == otp && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpCode == null)
                return false;

            if (DateTime.UtcNow > otpCode.ExpiresAt)
                return false;

            return true;
        }

        public async Task InvalidateOtpAsync(string email)
        {
            var existingOtps = await _context.OtpCodes
                .Where(o => o.Email == email && !o.IsUsed)
                .ToListAsync();

            foreach (var otp in existingOtps)
            {
                otp.IsUsed = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
