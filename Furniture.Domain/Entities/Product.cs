using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        //eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiJhMGMzNjRhYS04ZWEzLTQ3MGUtODM2OC1mOTgyYWRjYzZkZGEiLCJlbWFpbCI6ImFkbWluQGZ1cm5pdHVyZS5jb20iLCJqdGkiOiI2YmU3YzdiMi00ZmQ0LTQ4MTctYjdiMy0zOTQ4YWFhZDEyMGIiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbiIsImV4cCI6MTc3MjA3MTg2MywiaXNzIjoiQ296eSIsImF1ZCI6IllvdXJBcHBVc2VycyJ9.A4wIHVIvFLdNgnA5kRi81U1FPDCD_7xhXqWbYZ2DjWM
    }
}
