using AnFood.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnFood.Infrastructure.CacheService
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _distributedCache;

        public RedisCacheService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<T?> GetCacheAsync<T>(string key)
        {
            var serializedValue = await _distributedCache.GetStringAsync(key);

            if (string.IsNullOrEmpty(serializedValue))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(serializedValue);
        }

        public async Task RemoveCacheAsync(string key)
        {
            await _distributedCache.RemoveAsync(key);
        }

        public async Task SetCacheAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            var serializedValue = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime ?? TimeSpan.FromMinutes(30)
            };
            await _distributedCache.SetStringAsync(key, serializedValue, options);
        }

    }
}
