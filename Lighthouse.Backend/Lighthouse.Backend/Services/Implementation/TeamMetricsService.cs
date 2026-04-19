using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class TeamMetricsService(
        ILogger<TeamMetricsService> logger,
        IWorkItemRepository workItemRepository,
        IRepository<Feature> featureRepository,
        IAppSettingService appSettingService,
        IServiceProvider serviceProvider,
        IRepository<BlackoutPeriod> blackoutPeriodRepository)
        : BaseMetricsService(appSettingService.GetTeamDataRefreshSettings().Interval, serviceProvider),
            ITeamMetricsService
    {
        private const string ThroughputMetricIdentifier = "Throughput";
        private const string FeatureWipMetricIdentifier = "FeatureWIP";

        public IEnumerable<Feature> GetCurrentFeaturesInProgressForTeam(Team team)
        {
            logger.LogDebug("Getting Feature Wip for Team {TeamName}", team.Name);

            return GetFromCacheIfExists<IEnumerable<Feature>, Team>(team, FeatureWipMetricIdentifier, () =>
            {
                var activeWorkItemsForTeam = GetInProgressWorkItemsForTeam(team);
                var featureReferences = activeWorkItemsForTeam.Select(wi => wi.ParentReferenceId).Distinct().ToList();

                var features = new List<Feature>();
                foreach (var featureReference in featureReferences)
                {
                    var feature = featureRepository.GetByPredicate(wi => wi.ReferenceId == featureReference);
                    if (feature != null)
                    {
                        features.Add(feature);
                    }
                    else
                    {
                        var reference = string.IsNullOrEmpty(featureReference) ? "Unknown" : featureReference;

                        var unknownFeature = new Feature
                        {
                            Id = -1,
                            ReferenceId = reference,
                            Name = $"{reference} (Item not tracked by Lighthouse)",
                            Type = string.Empty,
                            State = string.Empty,
                            StateCategory = StateCategories.Unknown,
                            Url = string.Empty,
                            Order = string.Empty,
                        };

                        features.Add(unknownFeature);
                    }
                }

                logger.LogDebug("Finished updating Feature Wip for Team {TeamName} - Found {FeatureWIP} Features in Progress", team.Name, featureReferences.Count);

                return features;
            }, logger);
        }

        public IEnumerable<WorkItem> GetCurrentWipForTeam(Team team)
        {
            return GetWipSnapshotForTeam(team, DateTime.UtcNow.Date);
        }

        public ForecastInputCandidatesDto GetForecastInputCandidates(Team team)
        {
            logger.LogDebug("Getting Forecast Input Candidates for Team {TeamName}", team.Name);

            var currentWipCount = GetCurrentWipForTeam(team).Count();

            var backlogCount = workItemRepository
                .GetAllByPredicate(i => i.TeamId == team.Id &&
                    (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.ToDo))
                .Count();

            var features = featureRepository.GetAll()
                .Where(f => f.FeatureWork.Any(fw => fw.TeamId == team.Id && fw.RemainingWorkItems > 0))
                .Select(f => new FeatureCandidateDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    RemainingWork = f.FeatureWork
                        .Where(fw => fw.TeamId == team.Id)
                        .Sum(fw => fw.RemainingWorkItems)
                })
                .ToList();

            return new ForecastInputCandidatesDto
            {
                CurrentWipCount = currentWipCount,
                BacklogCount = backlogCount,
                Features = features
            };
        }

        public RunChartData GetCurrentThroughputForTeamForecast(Team team)
        {
            logger.LogDebug("Getting Current Throughput for Team {TeamName}", team.Name);

            return GetFromCacheIfExists(team, ThroughputMetricIdentifier, () =>
            {
                if (team.UseFixedDatesForThroughput)
                {
                    var endDate = DateTime.UtcNow.Date;
                    var startDate = team.ThroughputHistoryStartDate ?? endDate.AddDays(-(team.ThroughputHistory - 1));
                    var fixedEndDate = team.ThroughputHistoryEndDate ?? endDate;

                    return GetBlackoutAwareThroughputForTeam(team, startDate, fixedEndDate);
                }

                return GetBlackoutAwareThroughputForTeam(team, team.ThroughputHistory);
            }, logger);
        }

        public RunChartData GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Throughput for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"Throughput_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var closedItemsOfTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && i.StateCategory == StateCategories.Done);
                return new RunChartData(GenerateThroughputRunChart(startDate, endDate, closedItemsOfTeam));
            }, logger);
        }

        public RunChartData GetBlackoutAwareThroughputForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            var throughput = GetThroughputForTeam(team, startDate, endDate);
            var blackoutPeriods = blackoutPeriodRepository.GetAll().ToList();
            return FilterBlackoutDaysFromRunChart(throughput, startDate, endDate, blackoutPeriods);
        }

        public ProcessBehaviourChart GetThroughputProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate)
        {
            return BuildThroughputProcessBehaviourChart(team, startDate, endDate,
                (s, e) => GetThroughputForTeam(team, s, e));
        }

        public ProcessBehaviourChart GetWipProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate)
        {
            return BuildDailyRunChartProcessBehaviourChart(team, startDate, endDate,
                (s, e) => GetWorkInProgressOverTimeForTeam(team, s, e));
        }

        public ProcessBehaviourChart GetTotalWorkItemAgeProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate)
        {
            return BuildTotalWorkItemAgeProcessBehaviourChart(team, startDate, endDate,
                (s, e) => GetTotalWorkItemAgeOverTimeForTeam(team, s, e));
        }

        public ProcessBehaviourChart GetCycleTimeProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate)
        {
            return BuildCycleTimeProcessBehaviourChart(team, startDate, endDate,
                (s, e) => GetClosedItemsForTeam(team, s, e));
        }

        public RunChartData GetStartedItemsForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Started Items for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"StartedItems_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var startedItems = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && (i.StateCategory == StateCategories.Done || i.StateCategory == StateCategories.Doing));
                return new RunChartData(GenerateStartedRunChart(startDate, endDate, startedItems));
            }, logger);
        }

        public RunChartData GetArrivalsForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            return GetStartedItemsForTeam(team, startDate, endDate);
        }

        public ProcessBehaviourChart GetArrivalsProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate)
        {
            return BuildDailyRunChartProcessBehaviourChart(team, startDate, endDate,
                (s, e) => GetArrivalsForTeam(team, s, e));
        }

        public RunChartData GetCreatedItemsForTeam(Team team, IEnumerable<string> workItemTypes, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Created Items of type {WorkItemTypes} for Team {TeamName} between {StartDate} and {EndDate}", string.Join(", ", workItemTypes), team.Name, startDate.Date, endDate.Date);

            var includedWorkItems = workItemTypes.Select(itemTypes => itemTypes.ToLowerInvariant()).ToList();

            var allItemsForTeam = workItemRepository.GetAllByPredicate(item => item.TeamId == team.Id).ToList();
            var creationRunChart = GenerateCreationRunChart(startDate, endDate, allItemsForTeam.Where(item => includedWorkItems.Contains(item.Type.ToLowerInvariant())));

            var throughput = new RunChartData(creationRunChart);

            return throughput;
        }

        public RunChartData GetWorkInProgressOverTimeForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting WIP Over Time for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"WipOverTime_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var itemsFromTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.Done)).ToList();
                return new RunChartData(GenerateWorkInProgressByDay(startDate, endDate, itemsFromTeam));
            }, logger);
        }

        private (int[] Values, int[][] WorkItemIdsPerDay) GetTotalWorkItemAgeOverTimeForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Total Work Item Age for over Time for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            var itemsFromTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.Done)).ToList();
            var wiaOverTime = GenerateTotalWorkItemAgeByDay(startDate, endDate, itemsFromTeam);

            logger.LogDebug("Finished updating Total Work Item Age Over Time for Team {TeamName}", team.Name);

            return wiaOverTime;
        }

        public ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(team, $"ForecastPredictabilityScore_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var throughput = GetBlackoutAwareThroughputForTeam(team, startDate, endDate);
                return GetMultiItemForecastPredictabilityScore(throughput);
            }, logger);
        }

        public IEnumerable<WorkItem> GetClosedItemsForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Data for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            var closedItemsInDateRange = GetWorkItemsClosedInDateRange(team, startDate, endDate);

            return closedItemsInDateRange.ToList();
        }

        public IEnumerable<PercentileValue> GetCycleTimePercentilesForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Percentiles for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"CycleTimePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var closedItemsInDateRange = GetWorkItemsClosedInDateRange(team, startDate, endDate);
                var cycleTimes = closedItemsInDateRange.Select(i => i.CycleTime).Where(ct => ct > 0).ToList();

                return (IEnumerable<PercentileValue>)
                [
                    new PercentileValue(50, PercentileCalculator.CalculatePercentile(cycleTimes, 50)),
                    new PercentileValue(70, PercentileCalculator.CalculatePercentile(cycleTimes, 70)),
                    new PercentileValue(85, PercentileCalculator.CalculatePercentile(cycleTimes, 85)),
                    new PercentileValue(95, PercentileCalculator.CalculatePercentile(cycleTimes, 95))
                ];
            }, logger);
        }

        public EstimationVsCycleTimeResponse GetEstimationVsCycleTimeData(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Estimation vs Cycle Time Data for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"EstimationVsCycleTime_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var closedItems = GetWorkItemsClosedInDateRange(team, startDate, endDate);
                return BuildEstimationVsCycleTimeResponse(team, closedItems);
            }, logger);
        }

        public int GetTotalWorkItemAge(Team team)
        {
            return GetTotalWorkItemAge(team, DateTime.UtcNow.Date);
        }

        public int GetTotalWorkItemAge(Team team, DateTime endDate)
        {
            logger.LogDebug("Getting Total Work Item Age snapshot for Team {TeamName} at {EndDate}", team.Name, endDate.Date);

            var totalWorkItemAge = GetFromCacheIfExists(team, $"TotalWorkItemAge_{endDate:yyyy-MM-dd}", () =>
            {

                var items = workItemRepository.GetAllByPredicate(
                    i => i.TeamId == team.Id &&
                         (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.Done));

                var (values, _) = GenerateTotalWorkItemAgeByDay(endDate, endDate, items);
                return new InfoMetric(values[0]);
            }
            , logger);

            return totalWorkItemAge.Value;
        }

        public IEnumerable<WorkItem> GetWipSnapshotForTeam(Team team, DateTime endDate)
        {
            logger.LogDebug("Getting WIP snapshot for Team {TeamName} at {EndDate}", team.Name, endDate.Date);

            return GetFromCacheIfExists(team, $"WipSnapshot_{endDate:yyyy-MM-dd}", () =>
            {
                var items = workItemRepository.GetAllByPredicate(
                    i => i.TeamId == team.Id &&
                         (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.Done));

                return GenerateWorkInProgressByDay(endDate, endDate, items)[0].OfType<WorkItem>();
            }
            , logger);
        }

        public ThroughputInfoDto GetThroughputInfoForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Throughput Info for Team {TeamName} from {StartDate} to {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"ThroughputInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentThroughput = GetThroughputForTeam(team, startDate, endDate);
                var periodDays = (endDate.Date - startDate.Date).Days + 1;
                var previousEnd = startDate.AddDays(-1);
                var previousStart = startDate.AddDays(-periodDays);
                var previousThroughput = GetThroughputForTeam(team, previousStart, previousEnd);

                return BuildThroughputInfoDto(currentThroughput.Total, previousThroughput.Total, periodDays, startDate, endDate, previousStart, previousEnd);
            }, logger);
        }

        public ArrivalsInfoDto GetArrivalsInfoForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Arrivals Info for Team {TeamName} from {StartDate} to {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"ArrivalsInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
                         {
                             var currentArrivals = GetArrivalsForTeam(team, startDate, endDate);
                             var periodDays = (endDate.Date - startDate.Date).Days + 1;
                             var previousEnd = startDate.AddDays(-1);
                             var previousStart = startDate.AddDays(-periodDays);
                             var previousArrivals = GetArrivalsForTeam(team, previousStart, previousEnd);

                             return BuildArrivalsInfoDto(currentArrivals.Total, previousArrivals.Total, periodDays, startDate, endDate, previousStart, previousEnd);
                         }, logger);
        }

        public void InvalidateTeamMetrics(Team team)
        {
            InvalidateMetrics(team, logger);
        }

        public async Task UpdateTeamMetrics(Team team)
        {
            InvalidateTeamMetrics(team);

            team.RefreshUpdateTime();
            UpdateFeatureWipForTeam(team);

            await workItemRepository.Save();
        }

        private void UpdateFeatureWipForTeam(Team team)
        {
            if (!team.AutomaticallyAdjustFeatureWIP)
            {
                return;
            }

            var featureWip = GetCurrentFeaturesInProgressForTeam(team).Count();
            team.FeatureWIP = featureWip;
        }

        private IEnumerable<WorkItem> GetWorkItemsClosedInDateRange(Team team, DateTime startDate, DateTime endDate)
        {
            var closedItemsOfTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && i.StateCategory == StateCategories.Done).ToList();

            var closedItemsInDateRange = closedItemsOfTeam.Where(i => i.ClosedDate.HasValue && i.ClosedDate.Value.Date >= startDate.Date && i.ClosedDate.Value.Date <= endDate.Date);
            return closedItemsInDateRange;
        }

        private IEnumerable<WorkItem> GetInProgressWorkItemsForTeam(Team team)
        {
            return workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && i.StateCategory == StateCategories.Doing);
        }

        private RunChartData GetBlackoutAwareThroughputForTeam(Team team, int effectiveDaysNeeded)
        {
            var blackoutPeriods = blackoutPeriodRepository.GetAll().ToList();

            var endDate = DateTime.UtcNow.Date;
            var rollingStart = endDate.AddDays(-(effectiveDaysNeeded - 1));

            var blackoutDayIndices = blackoutPeriods.GetBlackoutDayIndices(rollingStart, endDate);
            while ((endDate - rollingStart).Days + 1 - blackoutDayIndices.Count < effectiveDaysNeeded)
            {
                var deficit = effectiveDaysNeeded - ((endDate - rollingStart).Days + 1 - blackoutDayIndices.Count);
                rollingStart = rollingStart.AddDays(-deficit);
                blackoutDayIndices = blackoutPeriods.GetBlackoutDayIndices(rollingStart, endDate);
            }

            var rollingThroughput = GetThroughputForTeam(team, rollingStart, endDate);
            var filtered = FilterBlackoutDays(rollingThroughput, blackoutDayIndices);

            if (filtered.History > effectiveDaysNeeded)
            {
                filtered = TrimToLatestDays(filtered, effectiveDaysNeeded);
            }

            return filtered;
        }

        private static RunChartData FilterBlackoutDaysFromRunChart(RunChartData runChart, DateTime startDate, DateTime endDate, List<BlackoutPeriod> blackoutPeriods)
        {
            var blackoutDayIndices = blackoutPeriods.GetBlackoutDayIndices(startDate, endDate);
            return FilterBlackoutDays(runChart, blackoutDayIndices);
        }

        internal static RunChartData FilterBlackoutDays(RunChartData runChart, HashSet<int> blackoutDayIndices)
        {
            var filteredData = new Dictionary<int, List<WorkItemBase>>();
            var newIndex = 0;

            for (var i = 0; i < runChart.History; i++)
            {
                if (!blackoutDayIndices.Contains(i))
                {
                    filteredData[newIndex] = runChart.WorkItemsPerUnitOfTime[i];
                    newIndex++;
                }
            }

            return new RunChartData(filteredData);
        }

        internal static RunChartData TrimToLatestDays(RunChartData runChart, int daysToKeep)
        {
            if (runChart.History <= daysToKeep)
            {
                return runChart;
            }

            var offset = runChart.History - daysToKeep;
            var trimmedData = new Dictionary<int, List<WorkItemBase>>();

            for (var i = 0; i < daysToKeep; i++)
            {
                trimmedData[i] = runChart.WorkItemsPerUnitOfTime[offset + i];
            }

            return new RunChartData(trimmedData);
        }
    }
}
