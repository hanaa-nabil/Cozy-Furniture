using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.DTOs.Common
{
    public class GlobalSearchResult
    {
        public List<SearchResult> Products { get; set; } = new();
        public List<SearchResult> Categories { get; set; } = new();
        public int TotalCount => Products.Count + Categories.Count;
    }
}
