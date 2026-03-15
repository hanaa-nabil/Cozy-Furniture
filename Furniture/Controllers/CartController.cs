using Furniture.Application.DTOs.Cart;
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
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService) => _cartService = cartService;

        private string UserId => User.FindFirstValue(AuthConstants.Claims.UserId)!;

        // GET api/cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var result = await _cartService.GetCartAsync(UserId);
            return Ok(result);
        }

        // POST api/cart
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] CartItemDto dto)
        {
            var result = await _cartService.AddToCartAsync(UserId, dto);
            return Ok(new { Success = true, Data = result });
        }

        // PUT api/cart/{id}?quantity=3
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromQuery] int quantity)
        {
            await _cartService.UpdateCartItemAsync(UserId, id, quantity);
            return Ok();
        }

        // DELETE api/cart/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            await _cartService.RemoveFromCartAsync(UserId, id);
            return Ok();
        }

        // DELETE api/cart/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> Clear()
        {
            await _cartService.ClearCartAsync(UserId);
            return Ok();
        }
    }
}