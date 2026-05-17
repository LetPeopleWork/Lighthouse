using System.Collections.Concurrent;

namespace Lighthouse.Backend.Cache
{
    public class Cache<TKey, TValue>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheItem<TValue>> cache = new();

        public IEnumerable<TKey> Keys { get => cache.Keys; }

        public void Store(TKey key, TValue value, TimeSpan expiresAfter)
        {
            cache[key] = new CacheItem<TValue>(value, expiresAfter);
        }

        public void Remove(TKey key)
        {
            cache.TryRemove(key, out _);
        }

        public TValue? Get(TKey key)
        {
            if (!cache.TryGetValue(key, out var cached))
            {
                return default;
            }

            if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
            {
                cache.TryRemove(key, out _);
                return default;
            }

            return cached.Value;
        }
    }
}
