using Furniture.Application.DTOs.Profile;
using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
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

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        private string UserId => User.FindFirstValue(AuthConstants.Claims.UserId)!;
        // GET api/profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var profile = await _profileService.GetProfileAsync(UserId);
            return Ok(profile);
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
