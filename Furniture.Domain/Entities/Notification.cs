using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }        // "OrderPlaced", "StatusChanged", "LowStock"
        public string? UserId { get; set; }     // null = admin-only notification
        public ApplicationUser? User { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? OrderId { get; set; }      
        public int? ProductId { get; set; }
        public string Target { get; set; } = "Customer";
    }
}
