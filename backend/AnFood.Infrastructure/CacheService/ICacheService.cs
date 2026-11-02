using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Infrastructure.CacheService
{
    public interface ICacheService
    {
        void Remove(string key);
        void Set<T>(string key, T cacheEntry, int expireTime = 0);
        Task<T> Get<T>(string key);
    }
}
