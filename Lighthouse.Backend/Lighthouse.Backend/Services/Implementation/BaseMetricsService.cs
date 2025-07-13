using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Services.Implementation
{
    public abstract class BaseMetricsService
    {
        private static readonly Cache<string, object> metricsCache = new Cache<string, object>();
        protected readonly int refreshRateInMinutes;
        private readonly IServiceProvider serviceProvider;
        private IForecastService? forecastService;

        protected BaseMetricsService(int refreshRateInMinutes, IServiceProvider serviceProvider)
        {
            this.refreshRateInMinutes = refreshRateInMinutes;
            this.serviceProvider = serviceProvider;
        }

        protected IForecastService ForecastService => forecastService ??= serviceProvider.GetRequiredService<IForecastService>();

        protected ForecastPredictabilityScore GetMultiItemForecastPredictabilityScore(RunChartData throughput, DateTime startDate, DateTime endDate)
        {
            var numberOfDays = (endDate - startDate).Days + 1;

            var howManyForecast = ForecastService.HowMany(throughput, numberOfDays);

            return new ForecastPredictabilityScore(howManyForecast);
        }

        protected static Dictionary<int, List<WorkItemBase>> GenerateThroughputRunChart(DateTime startDate, DateTime endDate, IEnumerable<WorkItemBase> items)
        {
            return GenerateRunChartByDay(startDate, endDate, items, GetClosedIndexForItem);
        }

        protected static Dictionary<int, List<WorkItemBase>> GenerateCreationRunChart(DateTime startDate, DateTime endDate, IEnumerable<WorkItemBase> items)
        {
            return GenerateRunChartByDay(startDate, endDate, items, GetCreatedIndexForItem);
        }

        protected static Dictionary<int, List<WorkItemBase>> GenerateStartedRunChart(DateTime startDate, DateTime endDate, IEnumerable<WorkItemBase> items)
        {
            return GenerateRunChartByDay(startDate, endDate, items, GetStartedIndexForItem);
        }

        protected static Dictionary<int, List<WorkItemBase>> GenerateRunChartByDay(DateTime startDate, DateTime endDate, IEnumerable<WorkItemBase> items, Func<DateTime, WorkItemBase, int> getDayIndex)
        {
            var totalDays = (endDate - startDate).Days + 1;

            var runChartData = InitializeRunChartDictionary(totalDays);

            foreach (var item in items)
            {
                var dayIndex = getDayIndex(startDate, item);
                if (dayIndex >= 0 && dayIndex < totalDays)
                {
                    runChartData[dayIndex].Add(item);
                }
            }

            return runChartData;
        }

        protected static Dictionary<int, List<WorkItemBase>> GenerateWorkInProgressByDay(DateTime startDate, DateTime endDate, IEnumerable<WorkItemBase> items)
        {
            var totalDays = (endDate - startDate).Days + 1;
            var runChartData = InitializeRunChartDictionary(totalDays);

            for (var index = 0; index < totalDays; index++)
            {
                var currentDate = startDate.AddDays(index);
                var itemsInProgressOnDay = items.Where(i => WasItemProgressOnDay(currentDate, i));

                runChartData[index].AddRange(itemsInProgressOnDay);
            }

            return runChartData;
        }

        protected static int GetClosedIndexForItem(DateTime startDate, WorkItemBase item)
        {
            return GetDateIndexBasedOnStartDate(startDate, item.ClosedDate);
        }

        protected static int GetCreatedIndexForItem(DateTime startDate, WorkItemBase item)
        {
            return GetDateIndexBasedOnStartDate(startDate, item.CreatedDate);
        }

        protected static int GetStartedIndexForItem(DateTime startDate, WorkItemBase item)
        {
            return GetDateIndexBasedOnStartDate(startDate, item.StartedDate);
        }

        protected static int GetDateIndexBasedOnStartDate(DateTime startDate, DateTime? date)
        {
            if (!date.HasValue)
            {
                return -1;
            }

            return (date.Value.Date - startDate.Date).Days;
        }

        protected static bool WasItemProgressOnDay(DateTime day, WorkItemBase item)
        {
            if (!item.StartedDate.HasValue)
            {
                return false;
            }

            var wasStartedOnOrAfterDay = item.StartedDate.Value.Date <= day.Date;
            var wasClosedOnOrAfterDay = !item.ClosedDate.HasValue || item.ClosedDate.Value.Date > day.Date;

            return wasStartedOnOrAfterDay && wasClosedOnOrAfterDay;
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
            var entityKeys = metricsCache.Keys.Where(k => k != null && k.StartsWith($"{entity.Id}_")).ToList();
            foreach (var entry in entityKeys)
            {
                metricsCache.Remove(entry);
            }
        }

        private static Dictionary<int, List<WorkItemBase>> InitializeRunChartDictionary(int totalDays)
        {
            var runChartData = new Dictionary<int, List<WorkItemBase>>();

            for (var index = 0; index < totalDays; index++)
            {
                runChartData[index] = new List<WorkItemBase>();
            }

            return runChartData;
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