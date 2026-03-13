using Furniture.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Furniture.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public CacheService(IDistributedCache cache) => _cache = cache;

        public async Task<T?> GetAsync<T>(string key)
        {
            var data = await _cache.GetStringAsync(key);
            if (data == null) return default;
            return JsonSerializer.Deserialize<T>(data);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiry)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };
            var data = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, data, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            // Redis doesn't natively support prefix delete via IDistributedCache
            // We track keys manually with a key registry
            var registryKey = $"__keys__{prefix}";
            var registry = await GetAsync<List<string>>(registryKey) ?? new List<string>();

            foreach (var key in registry)
                await _cache.RemoveAsync(key);

            await _cache.RemoveAsync(registryKey);
        }

        public async Task RegisterKeyAsync(string prefix, string key)
        {
            var registryKey = $"__keys__{prefix}";
            var registry = await GetAsync<List<string>>(registryKey) ?? new List<string>();

            if (!registry.Contains(key))
            {
                registry.Add(key);
                await SetAsync(registryKey, registry, TimeSpan.FromHours(24));
            }
        }
    }
}
