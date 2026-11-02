using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnFood.Infrastructure.CacheService
{
    public class InMemoryCacheService : ICacheService
    {
        private readonly MemoryCache _cache;
        private ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();

        public InMemoryCacheService()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public Task<T> Get<T>(string key)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public void Set<T>(string key, T cacheEntry, int expireTime = 0)
        {
            throw new NotImplementedException();
        }
    }

}
