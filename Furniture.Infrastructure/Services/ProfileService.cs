using Furniture.Application.DTOs.Profile;
using Furniture.Application.Interfaces;
using Furniture.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Infrastructure.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IImageService _imageService;
        private readonly ICacheService _cacheService;

        private const string CachePrefix = "profile";
        private TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public ProfileService(
            UserManager<ApplicationUser> userManager,
            IImageService imageService,
            ICacheService cacheService)
        {
            _userManager = userManager;
            _imageService = imageService;
            _cacheService = cacheService;
        }

        private string CacheKey(string userId) => $"{CachePrefix}:{userId}";

        public async Task<ProfileDto> GetProfileAsync(string userId)
        {
            var cacheKey = CacheKey(userId);

            var cached = await _cacheService.GetAsync<ProfileDto>(cacheKey);
            if (cached != null) return cached;

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            var dto = MapToDto(user);

            await _cacheService.SetAsync(cacheKey, dto, CacheDuration);
            await _cacheService.RegisterKeyAsync(CachePrefix, cacheKey);

            return dto;
        }

        public async Task<ProfileDto> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                user.FirstName = dto.FirstName;

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                user.LastName = dto.LastName;

            if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != user.UserName)
            {
                var existingUser = await _userManager.FindByNameAsync(dto.UserName);
                if (existingUser != null)
                    throw new InvalidOperationException("Username is already taken.");
                user.UserName = dto.UserName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            await _cacheService.RemoveAsync(CacheKey(userId));

            return MapToDto(user);
        }

        public async Task<string> UpdateProfilePictureAsync(string userId, IFormFile file)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            // Delete old picture from Cloudinary if exists
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                var oldPublicId = ExtractPublicId(user.ProfilePictureUrl);
                if (!string.IsNullOrEmpty(oldPublicId))
                    await _imageService.DeleteImageAsync(oldPublicId);
            }

            var url = await _imageService.UploadImageAsync(file, "profiles");

            user.ProfilePictureUrl = url;
            await _userManager.UpdateAsync(user);

            await _cacheService.RemoveAsync(CacheKey(userId));

            return url;
        }

        public async Task DeleteProfilePictureAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (string.IsNullOrEmpty(user.ProfilePictureUrl))
                throw new InvalidOperationException("No profile picture to delete.");

            var publicId = ExtractPublicId(user.ProfilePictureUrl);
            if (!string.IsNullOrEmpty(publicId))
                await _imageService.DeleteImageAsync(publicId);

            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);

            await _cacheService.RemoveAsync(CacheKey(userId));
        }

        // ─── Helpers ───────────────────────────────────────────────────────────

        private static ProfileDto MapToDto(ApplicationUser user) => new()
        {
            Id = user.Id,
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt
        };

        /// <summary>
        /// Extracts the Cloudinary public_id from a secure URL.
        /// e.g. https://res.cloudinary.com/.../furniture/profiles/abc123.webp → furniture/profiles/abc123
        /// </summary>
        private static string? ExtractPublicId(string url)
        {
            try
            {
                var uri = new Uri(url);
                // path: /demo/image/upload/v123456/furniture/profiles/abc.webp
                var segments = uri.AbsolutePath.Split('/');
                var uploadIndex = Array.IndexOf(segments, "upload");
                if (uploadIndex < 0) return null;

                // skip version segment (v123456) if present
                var startIndex = uploadIndex + 1;
                if (startIndex < segments.Length && segments[startIndex].StartsWith("v"))
                    startIndex++;

                var publicIdWithExt = string.Join("/", segments.Skip(startIndex));
                // remove extension
                var dotIndex = publicIdWithExt.LastIndexOf('.');
                return dotIndex > 0 ? publicIdWithExt[..dotIndex] : publicIdWithExt;
            }
            catch { return null; }
        }
    }
}
