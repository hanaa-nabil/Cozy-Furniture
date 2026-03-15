using Furniture.Application.DTOs.Profile;
using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
using Furniture.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Furniture.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly ICacheService _cacheService;

        public ProfileController(IProfileService profileService, ICacheService cacheService)
        {
            _profileService = profileService;
            _cacheService = cacheService;
        }

        private string UserId => User.FindFirstValue(AuthConstants.Claims.UserId)!;
        // GET api/profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(AuthConstants.Claims.UserId);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _cacheService.RemoveAsync($"profile:{userId}"); 

            var result = await _profileService.GetProfileAsync(userId);
            return Ok(result);
        }

        // PUT api/profile
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var updated = await _profileService.UpdateProfileAsync(UserId, dto);
            return Ok(updated);
        }

        // PUT api/profile/picture
        [HttpPut("picture")]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            var url = await _profileService.UpdateProfilePictureAsync(UserId, file);
            return Ok(new { profilePictureUrl = url });
        }

        // DELETE api/profile/picture
        [HttpDelete("picture")]
        public async Task<IActionResult> DeleteProfilePicture()
        {
            await _profileService.DeleteProfilePictureAsync(UserId);
            return NoContent();
        }
    }
}
