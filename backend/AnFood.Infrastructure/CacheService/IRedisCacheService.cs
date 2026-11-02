using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Infrastructure.CacheService
{
    public interface IRedisCacheService
    {
        Task SetCacheAsync<T>(string key, T value, TimeSpan? expirationTime = null);
        Task<T?> GetCacheAsync<T>(string key);
        Task RemoveCacheAsync(string key);
    }
}
