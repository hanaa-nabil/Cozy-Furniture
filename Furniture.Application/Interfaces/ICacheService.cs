using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan expiry);
        Task RemoveAsync(string key);
        Task RemoveByPrefixAsync(string prefix);
        Task RegisterKeyAsync(string prefix, string key);
    }
}
