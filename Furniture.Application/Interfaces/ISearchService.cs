using Furniture.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.Interfaces
{
    public interface ISearchService
    {
        Task<GlobalSearchResult> SearchAsync(string query, int limit = 5);
    }
}
