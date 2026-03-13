using Furniture.Application.DTOs.Cart;
using Furniture.Application.Interfaces;
using Furniture.Domain.Entities;
using Furniture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Infrastructure.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context) => _context = context;

        public async Task<List<CartItemResponseDto>> GetCartAsync(string userId)
        {
            return await _context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .Select(c => new CartItemResponseDto
                {
                    Id = c.Id,
                    ProductId = c.ProductId,
                    ProductName = c.Product.Name,
                    UnitPrice = c.Product.Price,
                    Quantity = c.Quantity,
                    Subtotal = c.Quantity * c.Product.Price
                }).ToListAsync();
        }

        public async Task AddToCartAsync(string userId, CartItemDto dto)
        {
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == dto.ProductId);

            if (existing != null)
                existing.Quantity += dto.Quantity;
            else
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });

            await _context.SaveChangesAsync();
        }

        public async Task UpdateCartItemAsync(string userId, int cartItemId, int quantity)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item == null) throw new Exception("Cart item not found");
            item.Quantity = quantity;
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(string userId, int cartItemId)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);
            if (item == null) throw new Exception("Cart item not found");
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        public async Task ClearCartAsync(string userId)
        {
            var items = _context.CartItems.Where(c => c.UserId == userId);
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}
