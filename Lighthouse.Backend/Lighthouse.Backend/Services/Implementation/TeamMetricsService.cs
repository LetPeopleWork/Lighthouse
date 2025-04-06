using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.DTO.Metrics;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class TeamMetricsService : ITeamMetricsService
    {
        private static readonly Cache<string, object> metricsCache = new Cache<string, object>();

        private readonly string throughputMetricIdentifier = "Throughput";
        private readonly string featureWipMetricIdentifier = "FeatureWIP";
        private readonly string wipMetricIdentifier = "WIP";

        private readonly ILogger<TeamMetricsService> logger;
        private readonly IWorkItemRepository workItemRepository;
        private readonly IRepository<Feature> featureRepository;

        private readonly int refreshRateInMinutes;

        public TeamMetricsService(ILogger<TeamMetricsService> logger, IWorkItemRepository workItemRepository, IRepository<Feature> featureRepository, IAppSettingService appSettingService)
        {
            this.logger = logger;
            this.workItemRepository = workItemRepository;
            this.featureRepository = featureRepository;
            refreshRateInMinutes = appSettingService.GetThroughputRefreshSettings().Interval;
        }

        public IEnumerable<WorkItemDto> GetCurrentFeaturesInProgressForTeam(Team team)
        {
            logger.LogDebug("Getting Feature Wip for Team {TeamName}", team.Name);

            return GetFromCacheIfExists(team, featureWipMetricIdentifier, () =>
            {
                var activeWorkItemsForTeam = GetInProgressWorkItemsForTeam(team);
                var featureReferences = activeWorkItemsForTeam.Select(wi => wi.ParentReferenceId).Distinct().ToList();

                var featureDtos = new List<WorkItemDto>();
                foreach (var featureReference in featureReferences)
                {
                    var feature = featureRepository.GetByPredicate(wi => wi.ReferenceId == featureReference);
                    if (feature != null)
                    {
                        featureDtos.Add(new WorkItemDto(feature));
                    }
                    else
                    {
                        featureDtos.Add(WorkItemDto.CreateUnknownWorkItemDto(featureReference));
                    }
                }

                logger.LogDebug("Finished updating Feature Wip for Team {TeamName} - Found {FeatureWIP} Features in Progress", team.Name, featureReferences.Count);

                return featureDtos;
            });
        }

        public IEnumerable<WorkItemDto> GetCurrentWipForTeam(Team team)
        {
            logger.LogDebug("Getting WIP for Team {TeamName}", team.Name);

            return GetFromCacheIfExists(team, wipMetricIdentifier, () =>
            {
                var activeWorkItemsForTeam = GetInProgressWorkItemsForTeam(team);

                var itemsInProgress = activeWorkItemsForTeam.Select(wi => new WorkItemDto(wi)).ToList();

                logger.LogDebug("Finished updating Wip for Team {TeamName} - Found {WIP} Items in Progress", team.Name, itemsInProgress.Count);

                return itemsInProgress;
            });
        }

        public Throughput GetCurrentThroughputForTeam(Team team)
        {
            logger.LogDebug("Getting Current Throughput for Team {TeamName}", team.Name);

            return GetFromCacheIfExists(team, throughputMetricIdentifier, () =>
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-(team.ThroughputHistory - 1));
                var endDate = DateTime.UtcNow;

                if (team.UseFixedDatesForThroughput)
                {
                    startDate = team.ThroughputHistoryStartDate ?? startDate;
                    endDate = team.ThroughputHistoryEndDate ?? endDate;
                }

                return GetThroughputForTeam(team, startDate, endDate);
            });
        }

        public Throughput GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Throughput for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            var closedItemsOfTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && i.StateCategory == StateCategories.Done);
            var throughputByDay = GenerateThroughputByDay(startDate, endDate, closedItemsOfTeam);

            logger.LogDebug("Finished updating Throughput for Team {TeamName}", team.Name);

            var throughput = new Throughput(throughputByDay);

            return throughput;
        }

        public IEnumerable<WorkItemDto> GetCycleTimeDataForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Data for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            var closedItemsInDateRange = GetWorkItemsClosedInDateRange(team, startDate, endDate);

            return closedItemsInDateRange.Select(item => new WorkItemDto(item)).ToList();
        }

        public IEnumerable<PercentileValue> GetCycleTimePercentilesForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Percentiles for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);
            var closedItemsInDateRange = GetWorkItemsClosedInDateRange(team, startDate, endDate);

            var cycleTimes = closedItemsInDateRange.Select(CalculateCycleTime).Where(ct => ct > 0).ToList();

            return [
                new PercentileValue(50, PercentileCalculator.CalculatePercentile(cycleTimes, 50)),
                new PercentileValue(70, PercentileCalculator.CalculatePercentile(cycleTimes, 70)),
                new PercentileValue(85, PercentileCalculator.CalculatePercentile(cycleTimes, 85)),
                new PercentileValue(95, PercentileCalculator.CalculatePercentile(cycleTimes, 95))
            ];
        }

        public void InvalidateTeamMetrics(Team team)
        {
            logger.LogInformation("Invalidating Metrics for Team {TeamName} (Id: {TeamId})", team.Name, team.Id);
            var teamKeys = metricsCache.Keys.Where(k => k.StartsWith($"{team.Id}_")).ToList();
            foreach (var entry in teamKeys)
            {
                metricsCache.Remove(entry);
            }
        }

        private IEnumerable<WorkItem> GetWorkItemsClosedInDateRange(Team team, DateTime startDate, DateTime endDate)
        {
            var closedItemsOfTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && i.StateCategory == StateCategories.Done);
            var closedItemsInDateRange = closedItemsOfTeam.Where(i => i.ClosedDate.HasValue && i.ClosedDate >= startDate && i.ClosedDate <= endDate);
            return closedItemsInDateRange;
        }

        private IEnumerable<WorkItem> GetInProgressWorkItemsForTeam(Team team)
        {
            return workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && i.StateCategory == StateCategories.Doing);
        }

        private static int CalculateCycleTime(WorkItem item)
        {
            if (item.ClosedDate.HasValue && item.StartedDate.HasValue)
            {
                return ((int)(item.ClosedDate.Value.Date - item.StartedDate.Value.Date).TotalDays) + 1;
            }

            return 0;
        }

        private static int[] GenerateThroughputByDay(DateTime startDate, DateTime endDate, IQueryable<WorkItem> closedItemsOfTeam)
        {
            var totalDays = (endDate - startDate).Days + 1;
            var throughputByDay = new int[totalDays];

            foreach (var index in closedItemsOfTeam.Select(i => GetThroughputIndexForItem(startDate, i)))
            {
                if (index >= 0 && index < totalDays)
                {
                    throughputByDay[index]++;
                }
            }

            return throughputByDay;
        }

        private static int GetThroughputIndexForItem(DateTime startDate, WorkItem item)
        {
            if (!item.ClosedDate.HasValue)
            {
                return -1;
            }

            return (item.ClosedDate.Value.Date - startDate).Days;
        }

        private TMetric GetFromCacheIfExists<TMetric>(Team team, string metricIdentifier, Func<TMetric> calculateMetric) where TMetric : class
        {
            var cacheKey = GetCacheKey(team, metricIdentifier);

            var cachedMetric = GetMetricFromCache<TMetric>(cacheKey);

            if (cachedMetric != null)
            {
                logger.LogDebug("Found {CacheKey} in Cache - Don't caclulate", cacheKey);
                return cachedMetric;
            }

            logger.LogDebug("Did not find {CacheKey} in Cache - Recalculating", cacheKey);
            var metric = calculateMetric();

            StoreMetricInCache(cacheKey, metric);

            return metric;
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

        private static string GetCacheKey(Team team, string metricIdentifier)
        {
            return $"{team.Id}_{metricIdentifier}";
        }
    }
}
