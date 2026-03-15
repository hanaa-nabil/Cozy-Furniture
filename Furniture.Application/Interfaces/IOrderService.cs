using Furniture.Application.DTOs.Common;
using Furniture.Application.DTOs.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.Interfaces
{
    public interface IOrderService
    {
        Task CancelOrderAsync(string userId, int orderId);
        Task<PagedResult<OrderResponseDto>> GetAllAsync(OrderQueryParameters query);
        Task<PagedResult<OrderResponseDto>> GetUserOrdersAsync(string userId, OrderQueryParameters query);  // add this
        Task<OrderResponseDto?> GetByIdAsync(int id);
        Task<OrderResponseDto> CreateAsync(string userId, CreateOrderDto dto);
        Task<OrderResponseDto> UpdateStatusAsync(int id, string status);

    }
}
