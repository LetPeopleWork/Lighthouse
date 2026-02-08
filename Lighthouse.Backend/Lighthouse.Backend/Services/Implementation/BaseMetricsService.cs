using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Services.Implementation
{
    public abstract class BaseMetricsService(int refreshRateInMinutes, IServiceProvider serviceProvider)
    {
        private static readonly Cache<string, object> MetricsCache = new();

        private IForecastService ForecastService => field ??= serviceProvider.GetRequiredService<IForecastService>();

        protected ForecastPredictabilityScore GetMultiItemForecastPredictabilityScore(RunChartData throughput, DateTime startDate, DateTime endDate)
        {
            var numberOfDays = (endDate - startDate).Days + 1;

            var howManyForecast = ForecastService.HowMany(throughput, numberOfDays);

            return new ForecastPredictabilityScore(howManyForecast);
        }

        protected static ProcessBehaviourChart BuildThroughputProcessBehaviourChart(
            WorkTrackingSystemOptionsOwner owner,
            DateTime displayStart,
            DateTime displayEnd,
            Func<DateTime, DateTime, RunChartData> getThroughput)
        {
            var baselineStart = owner.ProcessBehaviourChartBaselineStartDate;
            var baselineEnd = owner.ProcessBehaviourChartBaselineEndDate;

            if (baselineStart == null && baselineEnd == null)
            {
                return ProcessBehaviourChart.NotReady(BaselineStatus.BaselineMissing, "Baseline dates are not configured.");
            }

            var validation = BaselineValidationService.Validate(baselineStart, baselineEnd, owner.DoneItemsCutoffDays);
            if (!validation.IsValid)
            {
                return ProcessBehaviourChart.NotReady(BaselineStatus.BaselineInvalid, validation.ErrorMessage);
            }

            var baselineThroughput = getThroughput(baselineStart!.Value, baselineEnd!.Value);
            var displayThroughput = getThroughput(displayStart, displayEnd);

            var baselineValues = ExtractDailyCounts(baselineThroughput);
            var displayValues = ExtractDailyCounts(displayThroughput);

            var xmrResult = XmRCalculator.Calculate(baselineValues, displayValues, clampLnplToZero: true);

            var dataPoints = BuildDataPoints(displayThroughput, displayStart, xmrResult);

            return new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                XAxisKind = XAxisKind.Date,
                Average = xmrResult.Average,
                UpperNaturalProcessLimit = xmrResult.UpperNaturalProcessLimit,
                LowerNaturalProcessLimit = xmrResult.LowerNaturalProcessLimit,
                DataPoints = dataPoints,
            };
        }

        private static double[] ExtractDailyCounts(RunChartData runChartData)
        {
            var totalDays = runChartData.History;
            var values = new double[totalDays];

            for (var i = 0; i < totalDays; i++)
            {
                values[i] = runChartData.GetCountOnDay(i);
            }

            return values;
        }

        private static ProcessBehaviourChartDataPoint[] BuildDataPoints(
            RunChartData displayThroughput,
            DateTime displayStart,
            XmRResult xmrResult)
        {
            var totalDays = displayThroughput.History;
            var dataPoints = new ProcessBehaviourChartDataPoint[totalDays];

            for (var i = 0; i < totalDays; i++)
            {
                var date = displayStart.AddDays(i).ToString("yyyy-MM-dd");
                var workItems = displayThroughput.WorkItemsPerUnitOfTime[i];
                var workItemIds = workItems.Select(w => w.Id).ToArray();

                dataPoints[i] = new ProcessBehaviourChartDataPoint(
                    date,
                    workItems.Count,
                    xmrResult.SpecialCauseClassifications[i],
                    workItemIds);
            }

            return dataPoints;
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

        private static Dictionary<int, List<WorkItemBase>> GenerateRunChartByDay(DateTime startDate, DateTime endDate, IEnumerable<WorkItemBase> items, Func<DateTime, WorkItemBase, int> getDayIndex)
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

        private static int GetClosedIndexForItem(DateTime startDate, WorkItemBase item)
        {
            return GetDateIndexBasedOnStartDate(startDate, item.ClosedDate);
        }

        private static int GetCreatedIndexForItem(DateTime startDate, WorkItemBase item)
        {
            return GetDateIndexBasedOnStartDate(startDate, item.CreatedDate);
        }

        private static int GetStartedIndexForItem(DateTime startDate, WorkItemBase item)
        {
            return GetDateIndexBasedOnStartDate(startDate, item.StartedDate);
        }

        private static int GetDateIndexBasedOnStartDate(DateTime startDate, DateTime? date)
        {
            if (!date.HasValue)
            {
                return -1;
            }

            return (date.Value.Date - startDate.Date).Days;
        }

        private static bool WasItemProgressOnDay(DateTime day, WorkItemBase item)
        {
            if (!item.StartedDate.HasValue || (!item.ClosedDate.HasValue && item.StateCategory == StateCategories.Done))
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
            var entityKeys = MetricsCache.Keys.Where(k => k.StartsWith($"{entity.Id}_")).ToList();
            foreach (var entry in entityKeys)
            {
                MetricsCache.Remove(entry);
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
            MetricsCache.Remove(key);
            MetricsCache.Store(key, metric, TimeSpan.FromMinutes(refreshRateInMinutes));
        }

        private static TMetric? GetMetricFromCache<TMetric>(string key) where TMetric : class
        {
            var metric = MetricsCache.Get(key);

            return metric as TMetric;
        }

        private static string GetCacheKey(int entityId, string metricIdentifier)
        {
            return $"{entityId}_{metricIdentifier}";
        }
    }
}