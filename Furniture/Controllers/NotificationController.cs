using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Furniture.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private string UserId => User.FindFirstValue(AuthConstants.Claims.UserId)!;

        public NotificationController(INotificationService notificationService)
            => _notificationService = notificationService;

        // GET api/notification
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _notificationService.GetUserNotificationsAsync(UserId);
            return Ok(new { Success = true, Data = result });
        }
        [HttpGet("admin")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAllAdmin()
        {
            var result = await _notificationService.GetAllNotificationsAsync();
            return Ok(new { Success = true, Data = result });
        }
        // PUT api/notification/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id, UserId);
            return Ok(new { Success = true });
        }

        // PUT api/notification/read-all
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsReadAsync(UserId);
            return Ok(new { Success = true });
        }
    }
}
