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
            return BuildDailyRunChartProcessBehaviourChart(owner, displayStart, displayEnd, getThroughput);
        }

        protected static ProcessBehaviourChart BuildDailyRunChartProcessBehaviourChart(
            WorkTrackingSystemOptionsOwner owner,
            DateTime displayStart,
            DateTime displayEnd,
            Func<DateTime, DateTime, RunChartData> getRunChartData)
        {
            var baselineStart = owner.ProcessBehaviourChartBaselineStartDate;
            var baselineEnd = owner.ProcessBehaviourChartBaselineEndDate;
            var baselineConfigured = baselineStart != null || baselineEnd != null;

            if (!baselineConfigured)
            {
                baselineStart = displayStart;
                baselineEnd = displayEnd;
            }

            var validation = BaselineValidationService.Validate(baselineStart, baselineEnd, owner.DoneItemsCutoffDays);
            if (!validation.IsValid)
            {
                return new ProcessBehaviourChart
                {
                    Status = BaselineStatus.BaselineInvalid,
                    StatusReason = validation.ErrorMessage,
                    XAxisKind = XAxisKind.Date,
                    Average = 0,
                    UpperNaturalProcessLimit = 0,
                    LowerNaturalProcessLimit = 0,
                    BaselineConfigured = baselineConfigured,
                    DataPoints = [],
                };
            }

            var baselineData = getRunChartData(baselineStart!.Value, baselineEnd!.Value);
            var displayData = getRunChartData(displayStart, displayEnd);

            var baselineValues = ExtractDailyCounts(baselineData);
            var displayValues = ExtractDailyCounts(displayData);

            var xmrResult = XmRCalculator.Calculate(baselineValues, displayValues);

            var dataPoints = BuildDataPoints(displayData, displayStart, xmrResult);

            return new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                XAxisKind = XAxisKind.Date,
                Average = xmrResult.Average,
                UpperNaturalProcessLimit = xmrResult.UpperNaturalProcessLimit,
                LowerNaturalProcessLimit = xmrResult.LowerNaturalProcessLimit,
                BaselineConfigured = baselineConfigured,
                DataPoints = dataPoints,
            };
        }

        protected static ProcessBehaviourChart BuildTotalWorkItemAgeProcessBehaviourChart(
            WorkTrackingSystemOptionsOwner owner,
            DateTime displayStart,
            DateTime displayEnd,
            Func<DateTime, DateTime, (int[] Values, int[][] WorkItemIdsPerDay)> getDailyValues)
        {
            var baselineStart = owner.ProcessBehaviourChartBaselineStartDate;
            var baselineEnd = owner.ProcessBehaviourChartBaselineEndDate;
            var baselineConfigured = baselineStart != null || baselineEnd != null;

            if (!baselineConfigured)
            {
                baselineStart = displayStart;
                baselineEnd = displayEnd;
            }

            var validation = BaselineValidationService.Validate(baselineStart, baselineEnd, owner.DoneItemsCutoffDays);
            if (!validation.IsValid)
            {
                return new ProcessBehaviourChart
                {
                    Status = BaselineStatus.BaselineInvalid,
                    StatusReason = validation.ErrorMessage,
                    XAxisKind = XAxisKind.Date,
                    Average = 0,
                    UpperNaturalProcessLimit = 0,
                    LowerNaturalProcessLimit = 0,
                    BaselineConfigured = baselineConfigured,
                    DataPoints = [],
                };
            }

            var baselineResult = getDailyValues(baselineStart!.Value, baselineEnd!.Value);
            var displayResult = getDailyValues(displayStart, displayEnd);

            var xmrResult = XmRCalculator.Calculate(baselineResult.Values, displayResult.Values);

            var totalDays = displayResult.Values.Length;
            var dataPoints = new ProcessBehaviourChartDataPoint[totalDays];

            for (var i = 0; i < totalDays; i++)
            {
                var date = displayStart.AddDays(i).ToString("yyyy-MM-dd");
                dataPoints[i] = new ProcessBehaviourChartDataPoint(
                    date,
                    displayResult.Values[i],
                    xmrResult.SpecialCauseClassifications[i],
                    displayResult.WorkItemIdsPerDay[i]);
            }

            return new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                XAxisKind = XAxisKind.Date,
                Average = xmrResult.Average,
                UpperNaturalProcessLimit = xmrResult.UpperNaturalProcessLimit,
                LowerNaturalProcessLimit = xmrResult.LowerNaturalProcessLimit,
                BaselineConfigured = baselineConfigured,
                DataPoints = dataPoints,
            };
        }

        protected static ProcessBehaviourChart BuildCycleTimeProcessBehaviourChart(
            WorkTrackingSystemOptionsOwner owner,
            DateTime displayStart,
            DateTime displayEnd,
            Func<DateTime, DateTime, IEnumerable<WorkItemBase>> getClosedItems)
        {
            var baselineStart = owner.ProcessBehaviourChartBaselineStartDate;
            var baselineEnd = owner.ProcessBehaviourChartBaselineEndDate;
            var baselineConfigured = baselineStart != null || baselineEnd != null;

            if (!baselineConfigured)
            {
                baselineStart = displayStart;
                baselineEnd = displayEnd;
            }

            var validation = BaselineValidationService.Validate(baselineStart, baselineEnd, owner.DoneItemsCutoffDays);
            if (!validation.IsValid)
            {
                return new ProcessBehaviourChart
                {
                    Status = BaselineStatus.BaselineInvalid,
                    StatusReason = validation.ErrorMessage,
                    XAxisKind = XAxisKind.DateTime,
                    Average = 0,
                    UpperNaturalProcessLimit = 0,
                    LowerNaturalProcessLimit = 0,
                    BaselineConfigured = baselineConfigured,
                    DataPoints = [],
                };
            }

            var baselineItems = getClosedItems(baselineStart!.Value, baselineEnd!.Value)
                .Where(i => i.CycleTime > 0)
                .OrderBy(i => i.ClosedDate)
                .ThenBy(i => i.Id)
                .ToList();

            var displayItems = getClosedItems(displayStart, displayEnd)
                .Where(i => i.CycleTime > 0)
                .OrderBy(i => i.ClosedDate)
                .ThenBy(i => i.Id)
                .ToList();

            var baselineValues = baselineItems.Select(i => i.CycleTime).ToArray();
            var displayValues = displayItems.Select(i => i.CycleTime).ToArray();

            var xmrResult = XmRCalculator.Calculate(baselineValues, displayValues);

            var dataPoints = new ProcessBehaviourChartDataPoint[displayItems.Count];

            for (var i = 0; i < displayItems.Count; i++)
            {
                var item = displayItems[i];
                var xValue = item.ClosedDate!.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                dataPoints[i] = new ProcessBehaviourChartDataPoint(
                    xValue,
                    item.CycleTime,
                    xmrResult.SpecialCauseClassifications[i],
                    [item.Id]);
            }

            return new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                XAxisKind = XAxisKind.DateTime,
                Average = xmrResult.Average,
                UpperNaturalProcessLimit = xmrResult.UpperNaturalProcessLimit,
                LowerNaturalProcessLimit = xmrResult.LowerNaturalProcessLimit,
                BaselineConfigured = baselineConfigured,
                DataPoints = dataPoints,
            };
        }

        private static int[] ExtractDailyCounts(RunChartData runChartData)
        {
            var totalDays = runChartData.History;
            var values = new int[totalDays];

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

        protected static (int[] Values, int[][] WorkItemIdsPerDay) GenerateTotalWorkItemAgeByDay(DateTime startDate, DateTime endDate, IEnumerable<WorkItemBase> items)
        {
            var totalDays = (endDate - startDate).Days + 1;
            var values = new int[totalDays];
            var workItemIdsPerDay = new int[totalDays][];

            for (var index = 0; index < totalDays; index++)
            {
                var currentDate = startDate.AddDays(index);
                var itemsInProgressOnDay = items.Where(i => WasItemProgressOnDay(currentDate, i));

                var totalAge = itemsInProgressOnDay.Sum(item => (int)((currentDate.Date - item.StartedDate?.Date)?.TotalDays ?? 0) + 1);
                
                values[index] = totalAge;
                workItemIdsPerDay[index] = itemsInProgressOnDay.Select(i => i.Id).ToArray();
            }

            return (values, workItemIdsPerDay);
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

        protected static EstimationVsCycleTimeResponse BuildEstimationVsCycleTimeResponse(
            WorkTrackingSystemOptionsOwner owner,
            IEnumerable<WorkItemBase> closedItems)
        {
            if (owner.EstimationAdditionalFieldDefinitionId == null)
            {
                return new EstimationVsCycleTimeResponse(
                    EstimationVsCycleTimeStatus.NotConfigured,
                    new EstimationVsCycleTimeDiagnostics(0, 0, 0, 0),
                    owner.EstimationUnit,
                    owner.UseNonNumericEstimation,
                    owner.EstimationCategoryValues,
                    []);
            }

            var fieldId = owner.EstimationAdditionalFieldDefinitionId.Value;
            var items = closedItems.ToList();

            if (items.Count == 0)
            {
                return new EstimationVsCycleTimeResponse(
                    EstimationVsCycleTimeStatus.NoData,
                    new EstimationVsCycleTimeDiagnostics(0, 0, 0, 0),
                    owner.EstimationUnit,
                    owner.UseNonNumericEstimation,
                    owner.EstimationCategoryValues,
                    []);
            }

            var estimates = items.Select(i =>
            {
                i.AdditionalFieldValues.TryGetValue(fieldId, out var value);
                return value;
            }).ToList();
            var batchResult = EstimateNormalizer.NormalizeBatch(
                estimates,
                owner.UseNonNumericEstimation,
                owner.EstimationCategoryValues);

            var diagnostics = new EstimationVsCycleTimeDiagnostics(
                batchResult.TotalCount,
                batchResult.MappedCount,
                batchResult.UnmappedCount,
                batchResult.InvalidCount);

            // Group mapped items by (estimationNumericValue, cycleTime) to create data points
            var dataPoints = new List<EstimationVsCycleTimeDataPoint>();
            var groupedItems = new Dictionary<(double EstimationValue, int CycleTime), (string DisplayValue, List<int> WorkItemIds)>();

            for (var i = 0; i < items.Count; i++)
            {
                var normResult = batchResult.Results[i];
                if (normResult.Status != EstimateNormalizationStatus.Mapped)
                {
                    continue;
                }

                var key = (normResult.NumericValue, items[i].CycleTime);
                if (!groupedItems.TryGetValue(key, out var group))
                {
                    group = (normResult.DisplayValue, new List<int>());
                    groupedItems[key] = group;
                }

                group.WorkItemIds.Add(items[i].Id);
            }

            foreach (var kvp in groupedItems)
            {
                dataPoints.Add(new EstimationVsCycleTimeDataPoint(
                    kvp.Value.WorkItemIds.ToArray(),
                    kvp.Key.EstimationValue,
                    kvp.Value.DisplayValue,
                    kvp.Key.CycleTime));
            }

            var status = dataPoints.Count > 0
                ? EstimationVsCycleTimeStatus.Ready
                : EstimationVsCycleTimeStatus.NoData;

            return new EstimationVsCycleTimeResponse(
                status,
                diagnostics,
                owner.EstimationUnit,
                owner.UseNonNumericEstimation,
                owner.EstimationCategoryValues,
                dataPoints);
        }

        protected static FeatureSizeEstimationResponse BuildFeatureSizeEstimationResponse(
            WorkTrackingSystemOptionsOwner owner,
            IEnumerable<WorkItemBase> allFeatures)
        {
            if (owner.EstimationAdditionalFieldDefinitionId == null)
            {
                return new FeatureSizeEstimationResponse(
                    EstimationVsCycleTimeStatus.NotConfigured,
                    owner.EstimationUnit,
                    owner.UseNonNumericEstimation,
                    owner.EstimationCategoryValues,
                    []);
            }

            var fieldId = owner.EstimationAdditionalFieldDefinitionId.Value;
            var items = allFeatures.ToList();

            if (items.Count == 0)
            {
                return new FeatureSizeEstimationResponse(
                    EstimationVsCycleTimeStatus.NoData,
                    owner.EstimationUnit,
                    owner.UseNonNumericEstimation,
                    owner.EstimationCategoryValues,
                    []);
            }

            var estimates = items.Select(i =>
            {
                i.AdditionalFieldValues.TryGetValue(fieldId, out var value);
                return value;
            }).ToList();

            var batchResult = EstimateNormalizer.NormalizeBatch(
                estimates,
                owner.UseNonNumericEstimation,
                owner.EstimationCategoryValues);

            var featureEstimations = new List<FeatureEstimationDataPoint>();

            for (var i = 0; i < items.Count; i++)
            {
                var normResult = batchResult.Results[i];
                if (normResult.Status != EstimateNormalizationStatus.Mapped)
                {
                    continue;
                }

                featureEstimations.Add(new FeatureEstimationDataPoint(
                    items[i].Id,
                    normResult.NumericValue,
                    normResult.DisplayValue));
            }

            var status = featureEstimations.Count > 0
                ? EstimationVsCycleTimeStatus.Ready
                : EstimationVsCycleTimeStatus.NoData;

            return new FeatureSizeEstimationResponse(
                status,
                owner.EstimationUnit,
                owner.UseNonNumericEstimation,
                owner.EstimationCategoryValues,
                featureEstimations);
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