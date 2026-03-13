using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.DTOs.Common
{
    public class OrderQueryParameters : QueryParameters
    {
        public string? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
