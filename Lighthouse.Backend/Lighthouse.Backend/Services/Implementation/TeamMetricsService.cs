using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class TeamMetricsService : BaseMetricsService, ITeamMetricsService
    {
        private readonly string throughputMetricIdentifier = "Throughput";
        private readonly string featureWipMetricIdentifier = "FeatureWIP";
        private readonly string wipMetricIdentifier = "WIP";

        private readonly ILogger<TeamMetricsService> logger;
        private readonly IWorkItemRepository workItemRepository;
        private readonly IRepository<Feature> featureRepository;

        public TeamMetricsService(ILogger<TeamMetricsService> logger, IWorkItemRepository workItemRepository, IRepository<Feature> featureRepository, IAppSettingService appSettingService)
            : base(appSettingService.GetTeamDataRefreshSettings().Interval)
        {
            this.logger = logger;
            this.workItemRepository = workItemRepository;
            this.featureRepository = featureRepository;
        }

        public IEnumerable<Feature> GetCurrentFeaturesInProgressForTeam(Team team)
        {
            logger.LogDebug("Getting Feature Wip for Team {TeamName}", team.Name);

            return GetFromCacheIfExists<IEnumerable<Feature>, Team>(team, featureWipMetricIdentifier, () =>
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
            logger.LogDebug("Getting WIP for Team {TeamName}", team.Name);

            return GetFromCacheIfExists<IEnumerable<WorkItem>, Team>(team, wipMetricIdentifier, () =>
            {
                var activeWorkItemsForTeam = GetInProgressWorkItemsForTeam(team).ToList();

                logger.LogDebug("Finished updating Wip for Team {TeamName} - Found {WIP} Items in Progress", team.Name, activeWorkItemsForTeam.Count);

                return activeWorkItemsForTeam;
            }, logger);
        }

        public RunChartData GetCurrentThroughputForTeam(Team team)
        {
            logger.LogDebug("Getting Current Throughput for Team {TeamName}", team.Name);

            return GetFromCacheIfExists<RunChartData, Team>(team, throughputMetricIdentifier, () =>
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-(team.ThroughputHistory - 1));
                var endDate = DateTime.UtcNow;

                if (team.UseFixedDatesForThroughput)
                {
                    startDate = team.ThroughputHistoryStartDate ?? startDate;
                    endDate = team.ThroughputHistoryEndDate ?? endDate;
                }

                return GetThroughputForTeam(team, startDate, endDate);
            }, logger);
        }

        public RunChartData GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Throughput for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            var closedItemsOfTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && i.StateCategory == StateCategories.Done);
            var throughputByDay = GenerateThroughputRunChart(startDate, endDate, closedItemsOfTeam);

            var throughput = new RunChartData(throughputByDay);

            return throughput;
        }

        public RunChartData GetStartedItemsForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Started Items for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            var startedItems = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && (i.StateCategory == StateCategories.Done || i.StateCategory == StateCategories.Doing));
            var startedItemsByDay = GenerateStartedRunChart(startDate, endDate, startedItems);

            var throughput = new RunChartData(startedItemsByDay);

            return throughput;
        }

        public RunChartData GetCreatedItemsForTeam(Team team, IEnumerable<string> workItemTypes, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Created Items of type {WorkItemTypes} for Team {TeamName} between {StartDate} and {EndDate}", string.Join(", ", workItemTypes), team.Name, startDate.Date, endDate.Date);

            var includedWorkItems = workItemTypes.Select(workItemTypes => workItemTypes.ToLowerInvariant()).ToList();

            var allItemsForTeam = workItemRepository.GetAllByPredicate(item => item.TeamId == team.Id).ToList();
            var creationRunChart = GenerateCreationRunChart(startDate, endDate, allItemsForTeam.Where(item => includedWorkItems.Contains(item.Type.ToLowerInvariant())));

            var throughput = new RunChartData(creationRunChart);

            return throughput;
        }

        public RunChartData GetWorkInProgressOverTimeForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting WIP Over Time for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            var itemsFromTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.Done)).ToList();
            var wipOverTime = GenerateWorkInProgressByDay(startDate, endDate, itemsFromTeam);

            logger.LogDebug("Finished updating WIP Over Time for Team {TeamName}", team.Name);

            var throughput = new RunChartData(wipOverTime);

            return throughput;
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
            var closedItemsInDateRange = GetWorkItemsClosedInDateRange(team, startDate, endDate);

            var cycleTimes = closedItemsInDateRange.Select(i => i.CycleTime).Where(ct => ct > 0).ToList();

            return [
                new PercentileValue(50, PercentileCalculator.CalculatePercentile(cycleTimes, 50)),
                new PercentileValue(70, PercentileCalculator.CalculatePercentile(cycleTimes, 70)),
                new PercentileValue(85, PercentileCalculator.CalculatePercentile(cycleTimes, 85)),
                new PercentileValue(95, PercentileCalculator.CalculatePercentile(cycleTimes, 95))
            ];
        }

        public void InvalidateTeamMetrics(Team team)
        {
            InvalidateMetrics(team, logger);
        }

        public async Task UpdateTeamMetrics(Team team)
        {
            InvalidateTeamMetrics(team);

            team.RefreshUpdateTime();
            UpdateFeatureWIPForTeam(team);

            await workItemRepository.Save();
        }

        private void UpdateFeatureWIPForTeam(Team team)
        {
            if (team.AutomaticallyAdjustFeatureWIP)
            {
                var featureWip = GetCurrentFeaturesInProgressForTeam(team).Count();

                if (featureWip > 0)
                {
                    team.FeatureWIP = featureWip;
                }
            }
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
    }
}
