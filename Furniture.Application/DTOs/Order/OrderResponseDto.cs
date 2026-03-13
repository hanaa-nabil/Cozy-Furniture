using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.DTOs.Order
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public List<OrderItemResponseDto> Items { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public string ShippingAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
