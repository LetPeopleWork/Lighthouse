using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public abstract class BaseMetricsService
    {
        private static readonly Cache<string, object> metricsCache = new Cache<string, object>();
        protected readonly int refreshRateInMinutes;

        protected BaseMetricsService(int refreshRateInMinutes)
        {
            this.refreshRateInMinutes = refreshRateInMinutes;
        }

        protected static int[] GenerateThroughputByDay(DateTime startDate, DateTime endDate, IQueryable<WorkItemBase> items)
        {
            var totalDays = (endDate - startDate).Days + 1;
            var runChartData = new int[totalDays];

            foreach (var index in items.Select(i => GetThroughputIndexForItem(startDate, i)))
            {
                if (index >= 0 && index < totalDays)
                {
                    runChartData[index]++;
                }
            }

            return runChartData;
        }

        protected static int[] GenerateWorkInProgressByDay(DateTime startDate, DateTime endDate, IEnumerable<WorkItemBase> items)
        {
            var totalDays = (endDate - startDate).Days + 1;
            var runChartData = new int[totalDays];

            for (var index = 0; index < runChartData.Length; index++)
            {
                var currentDate = startDate.AddDays(index);
                var itemsInProgressOnDay = items.Count(i => WasItemProgressOnDay(currentDate, i));

                runChartData[index] = itemsInProgressOnDay;
            }

            return runChartData;
        }

        protected static int GetThroughputIndexForItem(DateTime startDate, WorkItemBase item)
        {
            if (!item.ClosedDate.HasValue)
            {
                return -1;
            }

            return (item.ClosedDate.Value.Date - startDate).Days;
        }

        protected static bool WasItemProgressOnDay(DateTime day, WorkItemBase item)
        {
            if (!item.StartedDate.HasValue)
            {
                return false;
            }

            if (!item.ClosedDate.HasValue)
            {
                return true;
            }

            return item.StartedDate?.Date <= day.Date && item.ClosedDate?.Date > day.Date;
        }

        protected TMetric GetFromCacheIfExists<TMetric, TEntity>(TEntity entity, string metricIdentifier, Func<TMetric> calculateMetric, ILogger logger) where TMetric : class where TEntity : class, IEntity
        {
            var cacheKey = GetCacheKey(entity.Id, metricIdentifier);

            var cachedMetric = GetMetricFromCache<TMetric>(cacheKey);

            if (cachedMetric != null)
            {
                logger.LogDebug("Found {CacheKey} in Cache - Don't calculate", cacheKey);
                return cachedMetric;
            }

            logger.LogDebug("Did not find {CacheKey} in Cache - Recalculating", cacheKey);
            var metric = calculateMetric();

            StoreMetricInCache(cacheKey, metric);

            return metric;
        }

        protected static void InvalidateMetrics<TEntity>(TEntity entity, ILogger logger) where TEntity : class, IEntity
        {
            logger.LogInformation("Invalidating Metrics for Entity Id: {EntityId}", entity.Id);
            var entityKeys = metricsCache.Keys.Where(k => k.StartsWith($"{entity.Id}_")).ToList();
            foreach (var entry in entityKeys)
            {
                metricsCache.Remove(entry);
            }
        }

        private void StoreMetricInCache<TMetric>(string key, TMetric metric) where TMetric : class
        {
            metricsCache.Remove(key);
            metricsCache.Store(key, metric, TimeSpan.FromMinutes(refreshRateInMinutes));
        }

        private static TMetric? GetMetricFromCache<TMetric>(string key) where TMetric : class
        {
            var metric = metricsCache.Get(key);

            return metric as TMetric;
        }

        private static string GetCacheKey(int entityId, string metricIdentifier)
        {
            return $"{entityId}_{metricIdentifier}";
        }
    }
}