using Furniture.Application.DTOs.Profile;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.Interfaces
{
    public interface IProfileService
    {
        Task<ProfileDto> GetProfileAsync(string userId);
        Task<ProfileDto> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task<string> UpdateProfilePictureAsync(string userId, IFormFile file);
        Task DeleteProfilePictureAsync(string userId);
    }
}
