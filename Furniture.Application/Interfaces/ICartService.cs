using Furniture.Application.DTOs.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.Interfaces
{
    public interface ICartService
    {
        Task<List<CartItemResponseDto>> GetCartAsync(string userId);
        Task<CartItemResponseDto> AddToCartAsync(string userId, CartItemDto dto);
        Task UpdateCartItemAsync(string userId, int cartItemId, int quantity);
        Task RemoveFromCartAsync(string userId, int cartItemId);
        Task ClearCartAsync(string userId);
    }
}
