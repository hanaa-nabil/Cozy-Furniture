using Furniture.Application.DTOs.Notification;
using Furniture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.Interfaces
{
    public interface INotificationService
    {
        Task NotifyOrderPlacedAsync(Order order);
        Task NotifyOrderStatusChangedAsync(Order order);
        Task NotifyLowStockAsync(Product product);
        Task<List<NotificationResponseDto>> GetUserNotificationsAsync(string userId);
        Task<List<NotificationResponseDto>> GetAllNotificationsAsync();
        Task MarkAsReadAsync(int id, string userId);
        Task MarkAllAsReadAsync(string userId);
    }
}
