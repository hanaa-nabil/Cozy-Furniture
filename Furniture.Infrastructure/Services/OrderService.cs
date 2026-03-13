using Furniture.Application.DTOs.Common;
using Furniture.Application.DTOs.Order;
using Furniture.Application.Interfaces;
using Furniture.Domain.Entities;
using Furniture.Domain.Enums;
using Furniture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Infrastructure.Services
{
    public class OrderService(ApplicationDbContext context) : IOrderService
    {
        public async Task<OrderResponseDto> CreateOrderFromCartAsync(string userId)
        {
            var cartItems = await context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                throw new InvalidOperationException("Cart is empty.");

            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                TotalPrice = cartItems.Sum(c => c.Product.Price * c.Quantity),
                OrderItems = cartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Product.Price
                }).ToList()
            };

            context.Orders.Add(order);
            context.CartItems.RemoveRange(cartItems);
            await context.SaveChangesAsync();

            return MapToDto(order);
        }

        public async Task<OrderResponseDto> CreateAsync(string userId, CreateOrderDto dto)
        {
            var cartItems = await context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            if (!cartItems.Any())
                throw new InvalidOperationException("Cannot create an order from an empty cart.");

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = dto.ShippingAddress,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                TotalPrice = cartItems.Sum(c => c.Quantity * c.Product.Price),
                OrderItems = cartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Product.Price
                }).ToList()
            };

            context.Orders.Add(order);
            context.CartItems.RemoveRange(cartItems);
            await context.SaveChangesAsync();

            return MapToDto(order);
        }

        public async Task<PagedResult<OrderResponseDto>> GetAllAsync(OrderQueryParameters query)
        {
            var q = context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.ToLower();
                q = q.Where(o => o.Id.ToString().Contains(term) ||
                                 o.User.Email!.ToLower().Contains(term));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<OrderStatus>(query.Status, ignoreCase: true, out var statusFilter))
                q = q.Where(o => o.Status == statusFilter);

            // Date range
            if (query.From.HasValue)
                q = q.Where(o => o.CreatedAt >= query.From);

            if (query.To.HasValue)
                q = q.Where(o => o.CreatedAt <= query.To);

            // Sorting
            q = query.SortBy?.ToLower() switch
            {
                "total" => query.SortOrder == "desc"
                                ? q.OrderByDescending(o => o.TotalPrice)
                                : q.OrderBy(o => o.TotalPrice),
                "created" => query.SortOrder == "desc"
                                ? q.OrderByDescending(o => o.CreatedAt)
                                : q.OrderBy(o => o.CreatedAt),
                _ => q.OrderByDescending(o => o.CreatedAt)
            };

            var totalCount = await q.CountAsync();

            var items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<OrderResponseDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<PagedResult<OrderResponseDto>> GetUserOrdersAsync(string userId, OrderQueryParameters query)
        {
            var q = context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<OrderStatus>(query.Status, ignoreCase: true, out var statusFilter))
                q = q.Where(o => o.Status == statusFilter);

            // Date range
            if (query.From.HasValue)
                q = q.Where(o => o.CreatedAt >= query.From);

            if (query.To.HasValue)
                q = q.Where(o => o.CreatedAt <= query.To);

            // Sorting
            q = query.SortBy?.ToLower() switch
            {
                "total" => query.SortOrder == "desc"
                                ? q.OrderByDescending(o => o.TotalPrice)
                                : q.OrderBy(o => o.TotalPrice),
                "created" => query.SortOrder == "desc"
                                ? q.OrderByDescending(o => o.CreatedAt)
                                : q.OrderBy(o => o.CreatedAt),
                _ => q.OrderByDescending(o => o.CreatedAt)
            };

            var totalCount = await q.CountAsync();

            var items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<OrderResponseDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<OrderResponseDto?> GetByIdAsync(int id)
        {
            var order = await context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order is null ? null : MapToDto(order);
        }

        public async Task<OrderResponseDto> UpdateStatusAsync(Guid id, string status)
        {
            var order = await context.Orders.FindAsync(id)
                ?? throw new KeyNotFoundException("Order not found.");

            if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var orderStatus))
                throw new ArgumentException(
                    $"Invalid status '{status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}");

            order.Status = orderStatus;
            await context.SaveChangesAsync();

            return MapToDto(order);
        }

        public async Task CancelOrderAsync(string userId, int orderId)
        {
            var order = await context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.Status != OrderStatus.Pending)
                throw new InvalidOperationException("Only pending orders can be cancelled.");

            order.Status = OrderStatus.Cancelled;
            await context.SaveChangesAsync();
        }

        // -------------------------------------------------------------------------
        private static OrderResponseDto MapToDto(Order o) => new()
        {
            Id = o.Id,
            TotalPrice = o.TotalPrice,
            Status = o.Status.ToString(),   // enum → string
            ShippingAddress = o.ShippingAddress,
            CreatedAt = o.CreatedAt,
            Items = o.OrderItems?.Select(i => new OrderItemResponseDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Quantity * i.UnitPrice
            }).ToList() ?? new()
        };
    }
}