using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.DTOs.Common
{
    public class CategoryQueryParameters : QueryParameters
    {
        public bool? IsActive { get; set; }
    }
}
