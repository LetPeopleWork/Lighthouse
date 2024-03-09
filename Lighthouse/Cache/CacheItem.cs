namespace Lighthouse.Cache
{
    public class CacheItem<T>
    {
        public CacheItem(T value, TimeSpan expiresAfter)
        {
            Value = value;
            ExpiresAfter = expiresAfter;
        }

        public T Value { get; }

        internal DateTimeOffset Created { get; } = DateTimeOffset.Now;

        internal TimeSpan ExpiresAfter { get; }
    }
}
