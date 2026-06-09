using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class TeamMetricsService(
        ILogger<TeamMetricsService> logger,
        IWorkItemRepository workItemRepository,
        IRepository<Feature> featureRepository,
        IAppSettingService appSettingService,
        IServiceProvider serviceProvider,
        IBlackoutPeriodService blackoutPeriodService,
        IForecastFilterRuleService forecastFilterRuleService,
        IWorkItemStateTransitionRepository workItemStateTransitionRepository)
        : BaseMetricsService(appSettingService.GetTeamDataRefreshSettings().Interval, serviceProvider),
            ITeamMetricsService
    {
        private const string ForecastStatusMetricIdentifier = "ForecastThroughputStatus";
        private const string FeatureWipMetricIdentifier = "FeatureWIP";
        private static readonly IReadOnlyList<int> DefaultPacePercentiles = [50, 70, 85, 95];
        internal const string EmptyFilteredSampleWarning = "Filter excluded all throughput; showing unfiltered forecast";

        public IEnumerable<Feature> GetCurrentFeaturesInProgressForTeam(Team team, DateTime asOfDate)
        {
            logger.LogDebug("Getting Feature Wip for Team {TeamName} as of {AsOfDate}", team.Name, asOfDate.Date);

            return GetFromCacheIfExists<IEnumerable<Feature>, Team>(team, $"{FeatureWipMetricIdentifier}_{asOfDate:yyyy-MM-dd}", () =>
            {
                var activeWorkItemsForTeam = GetWipSnapshotForTeam(team, asOfDate);
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

                logger.LogDebug("Finished updating Feature Wip for Team {TeamName} as of {AsOfDate} - Found {FeatureWIP} Features in Progress", team.Name, asOfDate.Date, featureReferences.Count);

                return features;
            }, logger);
        }

        public ForecastInputCandidatesDto GetForecastInputCandidates(Team team)
        {
            logger.LogDebug("Getting Forecast Input Candidates for Team {TeamName}", team.Name);

            var currentWipCount = GetWipSnapshotForTeam(team, DateTime.UtcNow.Date).Count();

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

        public RunChartData GetCurrentThroughputForTeamForecast(Team team, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting)
        {
            return GetForecastThroughputStatus(team, mode).Throughput;
        }

        public ForecastThroughputStatus GetForecastThroughputStatus(Team team, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting)
        {
            logger.LogDebug("Getting Forecast Throughput Status for Team {TeamName} (mode: {Mode})", team.Name, mode);

            return GetFromCacheIfExists(team, $"{ForecastStatusMetricIdentifier}_{mode}", () =>
            {
                var unfiltered = ComputeBlackoutAwareThroughput(team);
                var status = ApplyForecastFilter(team, unfiltered, mode);
                return status with { HasSufficientData = ForecastDataSufficiencyPolicy.HasEnoughData(status.Throughput) };
            }, logger);
        }

        private RunChartData ComputeBlackoutAwareThroughput(Team team)
        {
            if (team.UseFixedDatesForThroughput)
            {
                var endDate = DateTime.UtcNow.Date;
                var startDate = team.ThroughputHistoryStartDate ?? endDate.AddDays(-(team.ThroughputHistory - 1));
                var fixedEndDate = team.ThroughputHistoryEndDate ?? endDate;

                return GetBlackoutAwareThroughputForTeam(team, startDate, fixedEndDate, ThroughputFilterMode.SkipFilter);
            }

            return GetBlackoutAwareThroughputForTeam(team, team.ThroughputHistory);
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

        public RunChartData GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate, ThroughputFilterMode mode)
        {
            var unfiltered = GetThroughputForTeam(team, startDate, endDate);

            if (mode == ThroughputFilterMode.SkipFilter)
            {
                return unfiltered;
            }

            return GetFromCacheIfExists(team, $"Throughput_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}_{mode}", () =>
                ApplyForecastFilter(team, unfiltered, mode).Throughput, logger);
        }

        public RunChartData GetBlackoutAwareThroughputForTeam(Team team, DateTime startDate, DateTime endDate, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting)
        {
            var unfiltered = GetFromCacheIfExists(team, $"BlackoutAwareThroughput_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var throughput = GetThroughputForTeam(team, startDate, endDate);
                var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(startDate, endDate);
                return FilterBlackoutDaysFromRunChart(throughput, startDate, endDate, blackoutPeriods);
            }, logger);

            if (mode == ThroughputFilterMode.SkipFilter)
            {
                return unfiltered;
            }

            return GetFromCacheIfExists(team, $"BlackoutAwareThroughput_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}_{mode}", () =>
                ApplyForecastFilter(team, unfiltered, mode).Throughput, logger);
        }

        public ProcessBehaviourChart GetThroughputProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(team, $"ThroughputProcessBehaviour_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                return BuildThroughputProcessBehaviourChart(team, startDate, endDate,
                    (s, e) => GetThroughputForTeam(team, s, e));
            }, logger);
        }

        public ProcessBehaviourChart GetThroughputProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate, ThroughputFilterMode mode)
        {
            return GetFromCacheIfExists(team, $"ThroughputProcessBehaviour_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}_{mode}", () =>
            {
                return BuildThroughputProcessBehaviourChart(team, startDate, endDate,
                    (s, e) => ApplyForecastFilter(team, GetThroughputForTeam(team, s, e), mode).Throughput);
            }, logger);
        }

        public ProcessBehaviourChart GetWipProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(team, $"WipProcessBehaviour_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                return BuildDailyRunChartProcessBehaviourChart(team, startDate, endDate,
                    (s, e) => GetWorkInProgressOverTimeForTeam(team, s, e));
            }, logger);
        }

        public ProcessBehaviourChart GetTotalWorkItemAgeProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(team, $"TotalWorkItemAgeProcessBehaviour_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                return BuildTotalWorkItemAgeProcessBehaviourChart(team, startDate, endDate,
                    (s, e) => GetTotalWorkItemAgeOverTimeForTeam(team, s, e));
            }, logger);
        }

        public ProcessBehaviourChart GetCycleTimeProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(team, $"CycleTimeProcessBehaviour_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                return BuildCycleTimeProcessBehaviourChart(team, startDate, endDate,
                    (s, e) => GetClosedItemsForTeam(team, s, e));
            }, logger);
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

        public ProcessBehaviourChart GetArrivalsProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(team, $"ArrivalsProcessBehaviour_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
             {
                 return BuildDailyRunChartProcessBehaviourChart(team, startDate, endDate,
                     (s, e) => GetStartedItemsForTeam(team, s, e));
             }, logger);
        }

        public RunChartData GetCreatedItemsForTeam(Team team, IEnumerable<string> workItemTypes, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Created Items of type {WorkItemTypes} for Team {TeamName} between {StartDate} and {EndDate}", string.Join(", ", workItemTypes), team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"CreatedItems_{string.Join("-", workItemTypes)}_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
                         {
                             var includedWorkItems = workItemTypes.Select(itemTypes => itemTypes.ToLowerInvariant()).ToList();

                             var allItemsForTeam = workItemRepository.GetAllByPredicate(item => item.TeamId == team.Id).ToList();
                             var creationRunChart = GenerateCreationRunChart(startDate, endDate, allItemsForTeam.Where(item => includedWorkItems.Contains(item.Type.ToLowerInvariant())));

                             var throughput = new RunChartData(creationRunChart);

                             return throughput;
                         }, logger);
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

        public ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForTeam(Team team, DateTime startDate, DateTime endDate, ThroughputFilterMode mode)
        {
            return GetFromCacheIfExists(team, $"ForecastPredictabilityScore_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}_{mode}", () =>
            {
                var throughput = GetBlackoutAwareThroughputForTeam(team, startDate, endDate, mode);
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

                return BuildPercentiles(cycleTimes);
            }, logger);
        }

        public IReadOnlyList<CycleTimeWorkItem> GetCycleTimeDataForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Data for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"CycleTimeData_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
                ComputeCycleTimeData(team, startDate, endDate), logger);
        }

        public IEnumerable<PercentileValue> GetNamedCycleTimePercentilesForTeam(Team team, DateTime startDate, DateTime endDate, int definitionId)
        {
            logger.LogDebug("Getting Named Cycle Time Percentiles for Team {TeamName} definition {DefinitionId} between {StartDate} and {EndDate}", team.Name, definitionId, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"NamedCycleTimePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}_Def_{definitionId}", () =>
                BuildPercentiles(ComputeNamedDurations(team, startDate, endDate, definitionId)), logger);
        }

        private static IEnumerable<PercentileValue> BuildPercentiles(List<int> values) =>
        [
            new PercentileValue(50, PercentileCalculator.CalculatePercentile(values, 50)),
            new PercentileValue(70, PercentileCalculator.CalculatePercentile(values, 70)),
            new PercentileValue(85, PercentileCalculator.CalculatePercentile(values, 85)),
            new PercentileValue(95, PercentileCalculator.CalculatePercentile(values, 95)),
        ];

        private IReadOnlyList<CycleTimeWorkItem> ComputeCycleTimeData(Team team, DateTime startDate, DateTime endDate)
        {
            var allStatesInOrder = team.AllStates.ToList();
            var resolvedDefinitions = team.CycleTimeDefinitions
                .Select(definition => (
                    definition.Id,
                    StartState: ResolveBoundaryState(team, allStatesInOrder, definition.StartState),
                    EndState: ResolveBoundaryState(team, allStatesInOrder, definition.EndState)))
                .ToList();

            var closedItems = GetWorkItemsClosedInDateRange(team, startDate, endDate).ToList();
            var itemsWithTransitions = AssociateSyncedTransitions(closedItems);

            return itemsWithTransitions
                .Select(item => new CycleTimeWorkItem(item, NamedValuesForItem(item, allStatesInOrder, resolvedDefinitions)))
                .ToList();
        }

        private static IReadOnlyList<NamedCycleTimeValue> NamedValuesForItem(
            WorkItem item,
            IReadOnlyList<string> allStatesInOrder,
            IReadOnlyList<(int Id, string StartState, string EndState)> definitions)
        {
            return definitions
                .Select(definition => (definition.Id, days: NamedCycleTimeDays(item, allStatesInOrder, definition.StartState, definition.EndState)))
                .Where(entry => entry.days.HasValue)
                .Select(entry => new NamedCycleTimeValue(entry.Id, entry.days!.Value))
                .ToList();
        }

        private List<int> ComputeNamedDurations(Team team, DateTime startDate, DateTime endDate, int definitionId)
        {
            var definition = team.CycleTimeDefinitions.FirstOrDefault(candidate => candidate.Id == definitionId);
            if (definition == null)
            {
                return [];
            }

            var allStatesInOrder = team.AllStates.ToList();
            var startState = ResolveBoundaryState(team, allStatesInOrder, definition.StartState);
            var endState = ResolveBoundaryState(team, allStatesInOrder, definition.EndState);

            var closedItems = GetWorkItemsClosedInDateRange(team, startDate, endDate).ToList();
            var itemsWithTransitions = AssociateSyncedTransitions(closedItems);

            return itemsWithTransitions
                .Select(item => NamedCycleTimeDays(item, allStatesInOrder, startState, endState))
                .Where(days => days.HasValue)
                .Select(days => days!.Value)
                .ToList();
        }

        private static string ResolveBoundaryState(Team team, List<string> allStatesInOrder, string boundaryState)
        {
            var rawStates = team.GetRawStatesForCategory([boundaryState]);
            return allStatesInOrder.FirstOrDefault(state => rawStates.Any(raw => string.Equals(raw, state, StringComparison.OrdinalIgnoreCase)))
                ?? boundaryState;
        }

        public IEnumerable<AgeInStatePercentilesDto> GetAgeInStatePercentilesForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Age In State Percentiles for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"AgeInStatePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var completedItems = GetWorkItemsClosedInDateRange(team, startDate, endDate).ToList();
                var completedItemsWithTransitions = AssociateSyncedTransitions(completedItems);
                var cycleTimePercentiles = GetCycleTimePercentilesForTeam(team, startDate, endDate).ToList();

                return ComputeAgeInStatePercentiles(completedItemsWithTransitions, team.DoingStates, DefaultPacePercentiles, cycleTimePercentiles).ToList();
            }, logger);
        }

        public CumulativeStateTimeDto GetCumulativeStateTimeForTeam(Team team, DateTime startDate, DateTime endDate, IReadOnlyList<int>? itemIds = null)
        {
            logger.LogDebug("Getting Cumulative State Time for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"CumulativeStateTime_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}{SelectionCacheSuffix(itemIds)}", () =>
            {
                var candidateItems = NarrowToSelectedItems(ResolveCumulativeStateTimeCandidates(team, startDate, endDate), itemIds);
                var workflowStateOrder = BuildCumulativeWorkflowStateOrder(team);

                var states = ComputeCumulativeStateTime(candidateItems, workflowStateOrder, endDate);
                return new CumulativeStateTimeDto(states);
            }, logger);
        }

        public FlowEfficiencyInfoDto GetFlowEfficiencyInfoForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Flow Efficiency Info for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"FlowEfficiencyInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var candidateItems = ResolveCumulativeStateTimeCandidates(team, startDate, endDate);
                var workflowStateOrder = BuildCumulativeWorkflowStateOrder(team);
                var doingStateRows = ComputeCumulativeStateTime(candidateItems, workflowStateOrder, endDate);

                var expandedWaitStates = team.GetRawStatesForCategory(team.WaitStates);
                return ComputeFlowEfficiency(doingStateRows, expandedWaitStates, team.WaitStates.Count > 0);
            }, logger);
        }

        public CumulativeStateTimeItemsDto GetCumulativeStateTimeItemsForTeam(Team team, string state, DateTime startDate, DateTime endDate, IReadOnlyList<int>? itemIds = null)
        {
            logger.LogDebug("Getting Cumulative State Time Items for Team {TeamName} in state {State} between {StartDate} and {EndDate}", team.Name, state, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"CumulativeStateTime_Items_{state}_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}{SelectionCacheSuffix(itemIds)}", () =>
            {
                var candidateItems = NarrowToSelectedItems(ResolveCumulativeStateTimeCandidates(team, startDate, endDate), itemIds);
                var items = ComputeCumulativeStateTimeItems(candidateItems, state, endDate)
                    .OrderByDescending(item => item.DaysContributed)
                    .ToList();

                return new CumulativeStateTimeItemsDto(state, items);
            }, logger);
        }

        public CumulativeStateTimeCandidatesDto GetCumulativeStateTimeCandidatesForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cumulative State Time Candidates for Team {TeamName} between {StartDate} and {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"CumulativeStateTime_Candidates_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var candidateItems = ResolveCumulativeStateTimeCandidates(team, startDate, endDate);
                return new CumulativeStateTimeCandidatesDto(ProjectCumulativeStateTimeCandidates(candidateItems));
            }, logger);
        }

        private List<WorkItem> ResolveCumulativeStateTimeCandidates(Team team, DateTime startDate, DateTime endDate)
        {
            var teamItems = workItemRepository.GetAllByPredicate(item => item.TeamId == team.Id).ToList();
            var itemsWithTransitions = AssociateSyncedTransitionsPreservingCurrentState(teamItems);

            return itemsWithTransitions
                .Where(item => item.StateCategory != StateCategories.ToDo)
                .Where(item => IntersectsWindow(item, startDate, endDate) || IsInFlightAtWindowEnd(item, endDate))
                .ToList();
        }

        private List<WorkItem> AssociateSyncedTransitionsPreservingCurrentState(IReadOnlyCollection<WorkItem> items)
        {
            var itemIds = items.Select(item => item.Id).ToHashSet();
            var transitionsByItem = GroupTransitionsByItem(workItemStateTransitionRepository
                .GetAllByPredicate(transition => itemIds.Contains(transition.WorkItemId))
                .AsEnumerable()
                .Select(transition => (transition.WorkItemId, transition)));

            return items
                .Select(item => new WorkItem(item, item.Team)
                {
                    Id = item.Id,
                    CurrentStateEnteredAt = item.CurrentStateEnteredAt,
                    SyncedTransitions = transitionsByItem.TryGetValue(item.Id, out var transitions) ? transitions : [],
                })
                .ToList();
        }

        private static bool IntersectsWindow(WorkItem item, DateTime startDate, DateTime endDate)
        {
            if (!item.StartedDate.HasValue || item.SyncedTransitions.Count == 0)
            {
                return false;
            }

            var exits = item.SyncedTransitions
                .OrderBy(transition => transition.TransitionedAt)
                .Select(transition => transition.TransitionedAt)
                .ToList();

            return exits
                .Prepend(item.StartedDate.Value)
                .Zip(exits, (entry, exit) => entry <= endDate && exit >= startDate)
                .Any(intersects => intersects);
        }

        private static bool IsInFlightAtWindowEnd(WorkItem item, DateTime endDate)
        {
            return item.StateCategory != StateCategories.Done
                && item.CurrentStateEnteredAt.HasValue
                && item.CurrentStateEnteredAt.Value <= endDate;
        }

        private static List<string> BuildCumulativeWorkflowStateOrder(Team team)
        {
            return [.. team.DoingStates];
        }

        private List<WorkItem> AssociateSyncedTransitions(IReadOnlyCollection<WorkItem> completedItems)
        {
            var completedItemIds = completedItems.Select(item => item.Id).ToHashSet();
            var transitionsByItem = GroupTransitionsByItem(workItemStateTransitionRepository
                .GetAllByPredicate(transition => completedItemIds.Contains(transition.WorkItemId))
                .AsEnumerable()
                .Select(transition => (transition.WorkItemId, transition)));

            return completedItems
                .Select(item => new WorkItem(item, item.Team)
                {
                    Id = item.Id,
                    SyncedTransitions = transitionsByItem.TryGetValue(item.Id, out var transitions) ? transitions : [],
                })
                .ToList();
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
                             var currentArrivals = GetStartedItemsForTeam(team, startDate, endDate);
                             var periodDays = (endDate.Date - startDate.Date).Days + 1;
                             var previousEnd = startDate.AddDays(-1);
                             var previousStart = startDate.AddDays(-periodDays);
                             var previousArrivals = GetStartedItemsForTeam(team, previousStart, previousEnd);

                             return BuildArrivalsInfoDto(currentArrivals.Total, previousArrivals.Total, periodDays, startDate, endDate, previousStart, previousEnd);
                         }, logger);
        }

        public WipOverviewInfoDto GetWipOverviewInfoForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting WIP Overview Info for Team {TeamName} from {StartDate} to {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"WipOverviewInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentCount = GetWipSnapshotForTeam(team, endDate).Count();
                var previousCount = GetWipSnapshotForTeam(team, startDate).Count();
                return BuildWipOverviewInfoDto(currentCount, previousCount, endDate, startDate);
            }, logger);
        }

        public FeaturesWorkedOnInfoDto GetFeaturesWorkedOnInfoForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Features Worked On Info for Team {TeamName} from {StartDate} to {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"FeaturesWorkedOnInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentCount = GetCurrentFeaturesInProgressForTeam(team, endDate).Count();
                var previousCount = GetCurrentFeaturesInProgressForTeam(team, startDate).Count();
                return BuildFeaturesWorkedOnInfoDto(currentCount, previousCount, endDate, startDate);
            }, logger);
        }

        public TotalWorkItemAgeInfoDto GetTotalWorkItemAgeInfoForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Total Work Item Age Info for Team {TeamName} from {StartDate} to {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"TotalWorkItemAgeInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentAge = GetTotalWorkItemAge(team, endDate);
                var previousAge = GetTotalWorkItemAge(team, startDate);
                return BuildTotalWorkItemAgeInfoDto(currentAge, previousAge, endDate, startDate);
            }, logger);
        }

        public PredictabilityScoreInfoDto GetPredictabilityScoreInfoForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Predictability Score Info for Team {TeamName} from {StartDate} to {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"PredictabilityScoreInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentScore = GetMultiItemForecastPredictabilityScoreForTeam(team, startDate, endDate);
                var periodDays = (endDate.Date - startDate.Date).Days + 1;
                var previousEnd = startDate.AddDays(-1);
                var previousStart = startDate.AddDays(-periodDays);
                var previousScore = GetMultiItemForecastPredictabilityScoreForTeam(team, previousStart, previousEnd);
                return BuildPredictabilityScoreInfoDto(currentScore.PredictabilityScore, previousScore.PredictabilityScore, startDate, endDate, previousStart, previousEnd);
            }, logger);
        }

        public CycleTimePercentilesInfoDto GetCycleTimePercentilesInfoForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Percentiles Info for Team {TeamName} from {StartDate} to {EndDate}", team.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(team, $"CycleTimePercentilesInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentPercentiles = GetCycleTimePercentilesForTeam(team, startDate, endDate).ToList();
                var periodDays = (endDate.Date - startDate.Date).Days + 1;
                var previousEnd = startDate.AddDays(-1);
                var previousStart = startDate.AddDays(-periodDays);
                var previousPercentiles = GetCycleTimePercentilesForTeam(team, previousStart, previousEnd).ToList();
                return BuildCycleTimePercentilesInfoDto(currentPercentiles, previousPercentiles, startDate, endDate, previousStart, previousEnd);
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

            var featureWip = GetCurrentFeaturesInProgressForTeam(team, DateTime.UtcNow.Date).Count();
            team.FeatureWIP = featureWip;
        }

        private IEnumerable<WorkItem> GetWorkItemsClosedInDateRange(Team team, DateTime startDate, DateTime endDate)
        {
            var closedItemsOfTeam = workItemRepository.GetAllByPredicate(i => i.TeamId == team.Id && i.StateCategory == StateCategories.Done).ToList();

            var closedItemsInDateRange = closedItemsOfTeam.Where(i => i.ClosedDate.HasValue && i.ClosedDate.Value.Date >= startDate.Date && i.ClosedDate.Value.Date <= endDate.Date);
            return closedItemsInDateRange;
        }

        private RunChartData GetBlackoutAwareThroughputForTeam(Team team, int effectiveDaysNeeded)
        {
            var endDate = DateTime.UtcNow.Date;
            var rollingStart = endDate.AddDays(-(effectiveDaysNeeded - 1));

            var blackoutDayIndices = blackoutPeriodService.GetEffectiveBlackoutDays(rollingStart, endDate).GetBlackoutDayIndices(rollingStart, endDate);
            while ((endDate - rollingStart).Days + 1 - blackoutDayIndices.Count < effectiveDaysNeeded)
            {
                var deficit = effectiveDaysNeeded - ((endDate - rollingStart).Days + 1 - blackoutDayIndices.Count);
                rollingStart = rollingStart.AddDays(-deficit);
                blackoutDayIndices = blackoutPeriodService.GetEffectiveBlackoutDays(rollingStart, endDate).GetBlackoutDayIndices(rollingStart, endDate);
            }

            var rollingThroughput = GetThroughputForTeam(team, rollingStart, endDate);
            var filtered = FilterBlackoutDays(rollingThroughput, blackoutDayIndices);

            if (filtered.History > effectiveDaysNeeded)
            {
                filtered = TrimToLatestDays(filtered, effectiveDaysNeeded);
            }

            return filtered;
        }

        private ForecastThroughputStatus ApplyForecastFilter(Team team, RunChartData unfiltered, ThroughputFilterMode mode)
        {
            if (mode == ThroughputFilterMode.SkipFilter)
            {
                return new ForecastThroughputStatus(unfiltered, false, null);
            }

            var effectiveRuleSet = forecastFilterRuleService.GetEffectiveRuleSet(team);
            if (mode == ThroughputFilterMode.RespectTeamSetting && effectiveRuleSet == null || effectiveRuleSet == null)
            {
                return new ForecastThroughputStatus(unfiltered, false, null);
            }

            var allWorkItems = unfiltered.WorkItemsPerUnitOfTime.Values.SelectMany(items => items).OfType<WorkItem>().ToList();
            var kept = forecastFilterRuleService.Filter(allWorkItems, effectiveRuleSet).ToList();

            if (kept.Count == 0 && allWorkItems.Count > 0)
            {
                return new ForecastThroughputStatus(unfiltered, false, EmptyFilteredSampleWarning);
            }

            var filteredRunChart = BuildFilteredRunChart(unfiltered, kept);
            return new ForecastThroughputStatus(filteredRunChart, true, BuildExcludedSummary(allWorkItems.Count - kept.Count));
        }

        private static RunChartData BuildFilteredRunChart(RunChartData unfiltered, List<WorkItem> kept)
        {
            var keptIds = kept.Select(i => i.Id).ToHashSet();
            var filteredData = new Dictionary<int, List<WorkItemBase>>();

            for (var day = 0; day < unfiltered.History; day++)
            {
                filteredData[day] = unfiltered.WorkItemsPerUnitOfTime[day]
                    .Where(item => keptIds.Contains(item.Id))
                    .ToList();
            }

            return new RunChartData(filteredData);
        }

        private static string BuildExcludedSummary(int excludedCount)
        {
            return $"Excluded {excludedCount} work item{(excludedCount == 1 ? string.Empty : "s")} via team forecast filter";
        }

        private static RunChartData FilterBlackoutDaysFromRunChart(RunChartData runChart, DateTime startDate, DateTime endDate, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
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
