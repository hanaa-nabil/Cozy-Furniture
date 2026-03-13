using Furniture.Application.DTOs.Common;
using Furniture.Application.DTOs.Order;
using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Furniture.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService) => _orderService = orderService;

        private string UserId => User.FindFirstValue(AuthConstants.Claims.UserId)!;

        // POST api/order
        //[HttpPost]
        //public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        //{
        //   // var result = await _orderService.CreateOrderFromCartAsync(UserId, dto);
        //    return Ok(result);
        //}

        // GET api/order
        // OrderController.cs
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAll([FromQuery] OrderQueryParameters query)
        {
            var result = await _orderService.GetAllAsync(query);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("my-orders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryParameters query)
        {
            var userId = User.FindFirstValue(AuthConstants.Claims.UserId);
            var result = await _orderService.GetUserOrdersAsync(userId!, query);
            return Ok(new { Success = true, Data = result });
        }
        // PUT api/order/{id}/cancel
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            await _orderService.CancelOrderAsync(UserId, id);
            return Ok();
        }
    }
}