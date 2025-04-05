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

        private readonly ILogger<TeamMetricsService> logger;
        private readonly IRepository<WorkItem> workItemRepository;

        private readonly int refreshRateInMinutes;

        public TeamMetricsService(ILogger<TeamMetricsService> logger, IRepository<WorkItem> workItemRepository, IAppSettingService appSettingService)
        {
            this.logger = logger;
            this.workItemRepository = workItemRepository;

            refreshRateInMinutes = appSettingService.GetThroughputRefreshSettings().Interval;
        }

        public List<string> GetFeaturesInProgressForTeam(Team team)
        {
            logger.LogInformation("Getting Feature Wip for Team {TeamName}", team.Name);

            return GetFromCacheIfExists(team, featureWipMetricIdentifier, () =>
            {
                var activeWorkItemsForTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && i.StateCategory == StateCategories.Doing);
                var featureReferences = activeWorkItemsForTeam.Select(wi => wi.ParentReferenceId).Distinct().ToList();

                logger.LogInformation("Finished updating Feature Wip for Team {TeamName} - Found {FeatureWIP} Features in Progress", team.Name, featureReferences.Count);

                return featureReferences;
            });
        }

        public Throughput GetThroughputForTeam(Team team)
        {
            logger.LogDebug("Getting Throughput for Team {TeamName}", team.Name);

            return GetFromCacheIfExists(team, throughputMetricIdentifier, () =>
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-(team.ThroughputHistory - 1));
                var endDate = DateTime.UtcNow;

                if (team.UseFixedDatesForThroughput)
                {
                    startDate = team.ThroughputHistoryStartDate ?? startDate;
                    endDate = team.ThroughputHistoryEndDate ?? endDate;
                }

                var closedItemsOfTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && i.StateCategory == StateCategories.Done);
                var throughputByDay = GenerateThroughputByDay(startDate, endDate, closedItemsOfTeam);

                logger.LogDebug("Finished updating Throughput for Team {TeamName}", team.Name);

                var throughput = new Throughput(throughputByDay);

                return throughput;
            });
        }

        public void InvalidateTeamMetrics(Team team)
        {
            var teamKeys = metricsCache.Keys.Where(k => k.StartsWith($"{team.Id}_")).ToList();
            foreach (var entry in teamKeys)
            {
                metricsCache.Remove(entry);
            }
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
