using Furniture.Application.DTOs.Notification;
using Furniture.Application.Interfaces;
using Furniture.Domain.Entities;
using Furniture.Hubs;
using Furniture.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Infrastructure.Services
{
    public class NotificationService(
     ApplicationDbContext context,
     IHubContext<NotificationHub> hub) : INotificationService
    {
        public async Task NotifyOrderPlacedAsync(Order order)
        {
            var notification = new Notification
            {
                Title = "New Order Received",
                Message = $"Order #{order.Id} placed for ${order.TotalPrice:F2}",
                Type = "OrderPlaced",
                OrderId = order.Id,
                Target = "Admin",
                UserId = null 
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            await hub.Clients.Group("admins")
                .SendAsync("ReceiveNotification", MapToDto(notification));
        }

        public async Task NotifyOrderStatusChangedAsync(Order order)
        {
            var notification = new Notification
            {
                Title = "Order Status Updated",
                Message = $"Your order #{order.Id} is now {order.Status}.",
                Type = "StatusChanged",
                Target = "User",
                UserId = order.UserId,  
                OrderId = order.Id
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            await hub.Clients.Group($"user_{order.UserId}")
                .SendAsync("ReceiveNotification", MapToDto(notification));
        }
       
        public async Task NotifyLowStockAsync(Product product)
        {
            var notification = new Notification
            {
                Title = "Low Stock Alert",
                Message = $"'{product.Name}' has only {product.Stock} units left.",
                Type = "LowStock",
                ProductId = product.Id,
                Target = "Admin"
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            await hub.Clients.Group("admins")
                .SendAsync("ReceiveNotification", MapToDto(notification));
        }
        public async Task<List<NotificationResponseDto>> GetAllNotificationsAsync()
        {
            return await context.Notifications
                .Where(n => n.Target == "Admin")
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => MapToDto(n))
                .ToListAsync();
        }
        public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(string userId)
        {
            return await context.Notifications
                .Where(n => n.Target == "User" && n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => MapToDto(n))
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int id, string userId)
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id &&
                    (n.UserId == userId ))
                ?? throw new KeyNotFoundException("Notification not found.");

            notification.IsRead = true;
            await context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await context.Notifications
                .Where(n => !n.IsRead && (n.UserId == userId ))
                .ToListAsync();

            notifications.ForEach(n => n.IsRead = true);
            await context.SaveChangesAsync();
        }

        private static NotificationResponseDto MapToDto(Notification n) => new()
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            Target = n.Target,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            OrderId = n.OrderId,
            ProductId = n.ProductId
        };
    }
}
