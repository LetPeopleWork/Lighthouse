using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Services.Implementation
{
    public abstract class BaseMetricsService(int refreshRateInMinutes, IServiceProvider serviceProvider)
    {
        private static readonly Cache<string, object> MetricsCache = new();

        private IForecastService ForecastService => field ??= serviceProvider.GetRequiredService<IForecastService>();

        protected record InfoMetric(int Value);

        protected static Dictionary<int, IReadOnlyList<WorkItemStateTransition>> GroupTransitionsByItem(
            IEnumerable<(int ItemId, WorkItemStateTransition Transition)> transitions)
        {
            return transitions
                .GroupBy(entry => entry.ItemId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<WorkItemStateTransition>)group
                        .Select(entry => entry.Transition)
                        .OrderBy(transition => transition.TransitionedAt)
                        .ToList());
        }

        protected static IEnumerable<AgeInStatePercentilesDto> ComputeAgeInStatePercentiles(
            IEnumerable<WorkItem> completedItemsInWindow,
            IReadOnlyList<string> doingStatesInOrder,
            IReadOnlyList<int> requestedPercentiles,
            IReadOnlyList<PercentileValue> cycleTimePercentiles)
        {
            var items = completedItemsInWindow.Where(item => item.StartedDate.HasValue).ToList();
            if (items.Count == 0 || doingStatesInOrder.Count == 0)
            {
                return [];
            }

            var lastStateIndex = doingStatesInOrder.Count - 1;
            var columns = new List<AgeInStatePercentilesDto>();

            for (var index = 0; index < doingStatesInOrder.Count; index++)
            {
                var state = doingStatesInOrder[index];

                if (index == lastStateIndex)
                {
                    columns.Add(new AgeInStatePercentilesDto(state, CloneCycleTimeColumn(cycleTimePercentiles)));
                    continue;
                }

                var observations = items
                    .Select(item => LastExitCumulativeAge(item, state))
                    .Where(age => age.HasValue)
                    .Select(age => age!.Value)
                    .ToList();

                if (observations.Count > 0)
                {
                    columns.Add(BuildAgeInStatePercentilesDto(state, observations, requestedPercentiles));
                }
            }

            return ClampPercentilesNonDecreasing(columns);
        }

        private static List<PercentileValue> CloneCycleTimeColumn(IReadOnlyList<PercentileValue> cycleTimePercentiles)
        {
            return cycleTimePercentiles
                .Select(percentile => new PercentileValue(percentile.Percentile, percentile.Value))
                .ToList();
        }

        private static int? LastExitCumulativeAge(WorkItem item, string state)
        {
            if (!item.StartedDate.HasValue)
            {
                return null;
            }

            var lastExit = item.SyncedTransitions
                .Where(transition => transition.FromState == state)
                .Select(transition => (DateTime?)transition.TransitionedAt)
                .DefaultIfEmpty(null)
                .Max();

            return lastExit.HasValue
                ? CumulativeAgeAtExit(item.StartedDate.Value, lastExit.Value)
                : null;
        }

        protected static IReadOnlyList<AgeInStatePercentilesDto> ClampPercentilesNonDecreasing(
            IReadOnlyList<AgeInStatePercentilesDto> orderedPercentilesByState)
        {
            var runningMaxByRank = new Dictionary<int, int>();
            var clamped = new List<AgeInStatePercentilesDto>(orderedPercentilesByState.Count);

            foreach (var statePercentiles in orderedPercentilesByState)
            {
                var clampedPercentiles = statePercentiles.Percentiles
                    .Select(percentile => ClampRank(percentile, runningMaxByRank))
                    .ToList();

                clamped.Add(new AgeInStatePercentilesDto(statePercentiles.State, clampedPercentiles));
            }

            return clamped;
        }

        private static PercentileValue ClampRank(PercentileValue percentile, Dictionary<int, int> runningMaxByRank)
        {
            var runningMax = runningMaxByRank.TryGetValue(percentile.Percentile, out var max) ? max : int.MinValue;
            var clampedValue = Math.Max(percentile.Value, runningMax);
            runningMaxByRank[percentile.Percentile] = clampedValue;

            return new PercentileValue(percentile.Percentile, clampedValue);
        }

        private static AgeInStatePercentilesDto BuildAgeInStatePercentilesDto(string state, List<int> observations, IReadOnlyList<int> requestedPercentiles)
        {
            var percentiles = requestedPercentiles
                .Select(percentile => new PercentileValue(percentile, PercentileCalculator.CalculatePercentile(observations, percentile)))
                .ToList();

            return new AgeInStatePercentilesDto(state, percentiles);
        }

        private static int CumulativeAgeAtExit(DateTime startedDate, DateTime exitedAt)
        {
            return (int)(exitedAt.Date - startedDate.Date).TotalDays + 1;
        }

        protected static IReadOnlyList<CumulativeStateTimeStateRowDto> ComputeCumulativeStateTime(
            IEnumerable<WorkItem> includedItems,
            IReadOnlyList<string> workflowStateOrder,
            DateTime nowSnapshot)
        {
            var items = includedItems.ToList();
            if (items.Count == 0)
            {
                return [];
            }

            var completedVisitsByState = GroupCompletedVisitDurationsByState(items);
            var ongoingByState = GroupOngoingContributionsByState(items, nowSnapshot);

            return workflowStateOrder
                .Select((state, order) => BuildCumulativeStateTimeRow(
                    state,
                    order,
                    completedVisitsByState.TryGetValue(state, out var completed) ? completed : [],
                    ongoingByState.TryGetValue(state, out var ongoing) ? ongoing : []))
                .ToList();
        }

        protected static FlowEfficiencyInfoDto ComputeFlowEfficiency(
            IReadOnlyList<CumulativeStateTimeStateRowDto> doingStateRows,
            IReadOnlyList<string> expandedWaitStates,
            bool isConfigured)
        {
            var totalDoingDays = doingStateRows.Sum(row => row.TotalDays);
            if (!isConfigured || totalDoingDays <= 0)
            {
                return new FlowEfficiencyInfoDto(isConfigured, HasDataInScope: totalDoingDays > 0, EfficiencyPercent: 0, totalDoingDays, WaitDays: 0);
            }

            var waitStateSet = expandedWaitStates.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var waitDays = doingStateRows
                .Where(row => waitStateSet.Contains(row.State))
                .Sum(row => row.TotalDays);

            var efficiencyPercent = (totalDoingDays - waitDays) / totalDoingDays * 100.0;

            return new FlowEfficiencyInfoDto(isConfigured, HasDataInScope: true, efficiencyPercent, totalDoingDays, waitDays);
        }

        private static Dictionary<string, List<double>> GroupOngoingContributionsByState(IEnumerable<WorkItem> items, DateTime nowSnapshot)
        {
            var byState = new Dictionary<string, List<double>>();

            foreach (var item in items.Where(IsInFlight))
            {
                if (!byState.TryGetValue(item.State, out var durations))
                {
                    durations = [];
                    byState[item.State] = durations;
                }

                durations.Add(OngoingDuration(item, nowSnapshot));
            }

            return byState;
        }

        protected static IReadOnlyList<CumulativeStateTimeItemDto> ComputeCumulativeStateTimeItems(
            IEnumerable<WorkItem> includedItems,
            string state,
            DateTime nowSnapshot)
        {
            return includedItems
                .Select(item => (item, days: ContributionForState(item, state, nowSnapshot)))
                .Where(entry => entry.days > 0)
                .Select(entry => new CumulativeStateTimeItemDto
                {
                    WorkItemId = entry.item.Id,
                    ReferenceId = entry.item.ReferenceId,
                    Title = entry.item.Name,
                    Type = entry.item.Type,
                    State = entry.item.State,
                    StateCategory = entry.item.StateCategory.ToString(),
                    Url = entry.item.Url,
                    DaysContributed = entry.days,
                })
                .ToList();
        }

        protected static IReadOnlyList<CumulativeStateTimeCandidateRowDto> ProjectCumulativeStateTimeCandidates(IEnumerable<WorkItem> includedItems)
        {
            return includedItems
                .Select(item => new CumulativeStateTimeCandidateRowDto(item.Id, item.ReferenceId, item.Name, item.Type))
                .ToList();
        }

        protected static List<WorkItem> NarrowToSelectedItems(List<WorkItem> includedItems, IReadOnlyList<int>? itemIds)
        {
            if (itemIds is not { Count: > 0 })
            {
                return includedItems;
            }

            var selection = itemIds.ToHashSet();
            return includedItems.Where(item => selection.Contains(item.Id)).ToList();
        }

        protected static string SelectionCacheSuffix(IReadOnlyList<int>? itemIds)
        {
            if (itemIds is not { Count: > 0 })
            {
                return string.Empty;
            }

            return $"_Selection_{string.Join("-", itemIds.OrderBy(id => id))}";
        }

        private static Dictionary<string, List<(int ItemId, double Days)>> GroupCompletedVisitDurationsByState(IEnumerable<WorkItem> items)
        {
            var byState = new Dictionary<string, List<(int ItemId, double Days)>>();

            foreach (var visit in items.SelectMany(CompletedVisits))
            {
                if (!byState.TryGetValue(visit.State, out var visits))
                {
                    visits = [];
                    byState[visit.State] = visits;
                }

                visits.Add((visit.ItemId, visit.Days));
            }

            return byState;
        }

        private static IEnumerable<(string State, int ItemId, double Days)> CompletedVisits(WorkItem item)
        {
            return RawCompletedVisits(item)
                .Select(visit => (visit.State, visit.ItemId, (visit.End - visit.Start).TotalDays));
        }

        private static IEnumerable<(string State, int ItemId, DateTime Start, DateTime End)> RawCompletedVisits(WorkItem item)
        {
            var orderedTransitions = item.SyncedTransitions
                .OrderBy(transition => transition.TransitionedAt)
                .ToList();

            var entryIntoState = item.StartedDate;

            foreach (var transition in orderedTransitions)
            {
                if (entryIntoState.HasValue && !string.IsNullOrEmpty(transition.FromState))
                {
                    yield return (transition.FromState, item.Id, entryIntoState.Value, transition.TransitionedAt);
                }

                entryIntoState = transition.TransitionedAt;
            }
        }

        protected static IReadOnlyList<string> ScopedCumulativeStateOrder(IReadOnlyList<string> allStatesInOrder, string startState, string endState)
        {
            var startThreshold = BoundaryThresholdIndex(allStatesInOrder, startState);
            var endThreshold = BoundaryThresholdIndex(allStatesInOrder, endState);
            if (startThreshold < 0 || endThreshold <= startThreshold)
            {
                return [];
            }

            return allStatesInOrder.Skip(startThreshold).Take(endThreshold - startThreshold).ToList();
        }

        protected static IEnumerable<PercentileValue> BuildPercentiles(List<int> values) =>
        [
            new PercentileValue(50, PercentileCalculator.CalculatePercentile(values, 50)),
            new PercentileValue(70, PercentileCalculator.CalculatePercentile(values, 70)),
            new PercentileValue(85, PercentileCalculator.CalculatePercentile(values, 85)),
            new PercentileValue(95, PercentileCalculator.CalculatePercentile(values, 95)),
        ];

        protected static List<(int Id, string StartState, string EndState)> ResolveValidNamedDefinitions(
            WorkTrackingSystemOptionsOwner owner,
            IReadOnlyList<string> allStatesInOrder)
        {
            return owner.CycleTimeDefinitions
                .Where(owner.IsCycleTimeDefinitionValid)
                .Select(definition => (
                    definition.Id,
                    StartState: ResolveBoundaryState(owner, allStatesInOrder, definition.StartState),
                    EndState: ResolveBoundaryState(owner, allStatesInOrder, definition.EndState)))
                .ToList();
        }

        protected static List<NamedCycleTimeValue> NamedValuesForItem(
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

        protected static string ResolveBoundaryState(WorkTrackingSystemOptionsOwner owner, IReadOnlyList<string> allStatesInOrder, string boundaryState)
        {
            var rawStates = owner.GetRawStatesForCategory([boundaryState]);
            return allStatesInOrder.FirstOrDefault(state => rawStates.Any(raw => string.Equals(raw, state, StringComparison.OrdinalIgnoreCase)))
                ?? boundaryState;
        }

        protected static int? NamedCycleTimeDays(WorkItem item, IReadOnlyList<string> allStatesInOrder, string startState, string endState)
        {
            var window = NamedCycleTimeWindow(item, allStatesInOrder, startState, endState);
            if (window == null)
            {
                return null;
            }

            return (int)(window.Value.End.Date - window.Value.Start.Date).TotalDays + 1;
        }

        protected static (DateTime Start, DateTime End)? NamedCycleTimeWindow(WorkItem item, IReadOnlyList<string> allStatesInOrder, string startState, string endState)
        {
            var startThreshold = BoundaryThresholdIndex(allStatesInOrder, startState);
            var endThreshold = BoundaryThresholdIndex(allStatesInOrder, endState);
            if (startThreshold < 0 || endThreshold < 0)
            {
                return null;
            }

            var stateEntries = OrderedStateEntries(item, allStatesInOrder).ToList();

            var startMoment = stateEntries
                .Where(entry => entry.Rank >= startThreshold)
                .Select(entry => (DateTime?)entry.EnteredAt)
                .FirstOrDefault();
            if (!startMoment.HasValue)
            {
                return null;
            }

            var endMoment = stateEntries
                .Where(entry => entry.EnteredAt >= startMoment.Value && entry.Rank >= endThreshold)
                .Select(entry => (DateTime?)entry.EnteredAt)
                .FirstOrDefault();
            if (!endMoment.HasValue)
            {
                return null;
            }

            return (startMoment.Value, endMoment.Value);
        }

        private static int BoundaryThresholdIndex(IReadOnlyList<string> allStatesInOrder, string boundaryState)
        {
            for (var index = 0; index < allStatesInOrder.Count; index++)
            {
                if (string.Equals(allStatesInOrder[index], boundaryState, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private static IEnumerable<(int Rank, DateTime EnteredAt)> OrderedStateEntries(WorkItem item, IReadOnlyList<string> allStatesInOrder)
        {
            if (!item.StartedDate.HasValue)
            {
                yield break;
            }

            var orderedTransitions = item.SyncedTransitions
                .OrderBy(transition => transition.TransitionedAt)
                .ToList();

            var firstState = orderedTransitions.Count > 0 ? orderedTransitions[0].FromState : item.State;
            yield return (RankOfState(allStatesInOrder, firstState), item.StartedDate.Value);

            foreach (var transition in orderedTransitions)
            {
                yield return (RankOfState(allStatesInOrder, transition.ToState), transition.TransitionedAt);
            }
        }

        private static int RankOfState(IReadOnlyList<string> allStatesInOrder, string state)
        {
            for (var index = 0; index < allStatesInOrder.Count; index++)
            {
                if (string.Equals(allStatesInOrder[index], state, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool IsInFlight(WorkItem item)
        {
            return item.StateCategory == StateCategories.Doing && item.CurrentStateEnteredAt.HasValue;
        }

        private static double OngoingDuration(WorkItem item, DateTime nowSnapshot)
        {
            return (nowSnapshot - item.CurrentStateEnteredAt!.Value).TotalDays;
        }

        private static double ContributionForState(WorkItem item, string state, DateTime nowSnapshot)
        {
            var completed = CompletedVisits(item)
                .Where(visit => visit.State == state)
                .Sum(visit => visit.Days);

            var ongoing = IsInFlight(item) && item.State == state
                ? OngoingDuration(item, nowSnapshot)
                : 0;

            return completed + ongoing;
        }

        private static CumulativeStateTimeStateRowDto BuildCumulativeStateTimeRow(
            string state,
            int workflowOrder,
            IReadOnlyList<(int ItemId, double Days)> completedVisits,
            List<double> ongoingDurations)
        {
            var completedContribution = completedVisits.Sum(visit => visit.Days);
            var ongoingContribution = ongoingDurations.Sum();
            var completedItemCount = completedVisits.Select(visit => visit.ItemId).Distinct().Count();

            return new CumulativeStateTimeStateRowDto(
                State: state,
                WorkflowOrder: workflowOrder,
                TotalDays: completedContribution + ongoingContribution,
                CompletedContributionDays: completedContribution,
                OngoingContributionDays: ongoingContribution,
                ItemCount: completedItemCount + ongoingDurations.Count,
                CompletedItemCount: completedItemCount,
                OngoingItemCount: ongoingDurations.Count,
                MeanDays: CumulativeMean(completedVisits),
                MedianDays: CumulativeMedian(completedVisits));
        }

        private static double CumulativeMean(IReadOnlyList<(int ItemId, double Days)> completedVisits)
        {
            return completedVisits.Count == 0 ? 0 : completedVisits.Average(visit => visit.Days);
        }

        private static double? CumulativeMedian(IReadOnlyList<(int ItemId, double Days)> completedVisits)
        {
            if (completedVisits.Count == 0)
            {
                return null;
            }

            var roundedDays = completedVisits.Select(visit => (int)Math.Round(visit.Days)).ToList();
            return PercentileCalculator.CalculatePercentile(roundedDays, 50);
        }

        protected ForecastPredictabilityScore GetMultiItemForecastPredictabilityScore(RunChartData throughput)
        {
            var numberOfDays = 30;

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

                var totalAge = itemsInProgressOnDay.Sum(item => (int)((currentDate.Date - (item.StartedDate ?? item.CreatedDate)?.Date)?.TotalDays ?? 0) + 1);
                
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
            var startedDate = item.StartedDate ?? item.CreatedDate;

            // Item not started --> Can't be in progress
            if (!startedDate.HasValue)
            {
                return false;
            }

            if (!item.ClosedDate.HasValue && item.StateCategory == StateCategories.Done)
            {
                return false;
            }

            var wasStartedOnOrAfterDay = startedDate.Value.Date <= day.Date;
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

        protected static ThroughputInfoDto BuildThroughputInfoDto(
            int currentTotal,
            int previousTotal,
            int periodDays,
            DateTime currentStart,
            DateTime currentEnd,
            DateTime previousStart,
            DateTime previousEnd)
        {
            var direction = DetermineIntCountDirection(currentTotal, previousTotal);
            var dailyAverage = Math.Round((double)currentTotal / periodDays, 1);

            var currentLabel = $"{currentStart:yyyy-MM-dd} – {currentEnd:yyyy-MM-dd}";
            var previousLabel = $"{previousStart:yyyy-MM-dd} – {previousEnd:yyyy-MM-dd}";
            var percentageDelta = ComputePercentageDelta(currentTotal, previousTotal);

            var comparison = new InfoWidgetComparisonDto(
                direction,
                "Total Throughput",
                currentLabel,
                currentTotal.ToString(),
                previousLabel,
                previousTotal.ToString(),
                percentageDelta,
                null);

            return new ThroughputInfoDto(currentTotal, dailyAverage, comparison);
        }

        protected static ArrivalsInfoDto BuildArrivalsInfoDto(
            int currentTotal,
            int previousTotal,
            int periodDays,
            DateTime currentStart,
            DateTime currentEnd,
            DateTime previousStart,
            DateTime previousEnd)
        {
            var direction = DetermineIntCountDirection(currentTotal, previousTotal);
            var dailyAverage = Math.Round((double)currentTotal / periodDays, 1);

            var currentLabel = $"{currentStart:yyyy-MM-dd} – {currentEnd:yyyy-MM-dd}";
            var previousLabel = $"{previousStart:yyyy-MM-dd} – {previousEnd:yyyy-MM-dd}";
            var percentageDelta = ComputePercentageDelta(currentTotal, previousTotal);

            var comparison = new InfoWidgetComparisonDto(
                direction,
                "Total Arrivals",
                currentLabel,
                currentTotal.ToString(),
                previousLabel,
                previousTotal.ToString(),
                percentageDelta,
                null);

            return new ArrivalsInfoDto(currentTotal, dailyAverage, comparison);
        }

        private static string DetermineIntCountDirection(int current, int previous)
        {
            if (previous == 0)
            {
                return current == 0 ? "flat" : "up";
            }
            if (current > previous) return "up";
            if (current < previous) return "down";
            return "flat";
        }

        protected static WipOverviewInfoDto BuildWipOverviewInfoDto(
            int currentCount, int previousCount, DateTime asOfDate, DateTime previousDate)
        {
            var direction = DetermineIntCountDirection(currentCount, previousCount);
            var percentageDelta = ComputePercentageDelta(currentCount, previousCount);
            var comparison = new InfoWidgetComparisonDto(
                direction, "WIP", $"{asOfDate:yyyy-MM-dd}", currentCount.ToString(),
                $"{previousDate:yyyy-MM-dd}", previousCount.ToString(), percentageDelta, null);
            return new WipOverviewInfoDto(currentCount, comparison);
        }

        protected static FeaturesWorkedOnInfoDto BuildFeaturesWorkedOnInfoDto(
            int currentCount, int previousCount, DateTime asOfDate, DateTime previousDate)
        {
            var direction = DetermineIntCountDirection(currentCount, previousCount);
            var percentageDelta = ComputePercentageDelta(currentCount, previousCount);
            var comparison = new InfoWidgetComparisonDto(
                direction, "Features Being Worked On", $"{asOfDate:yyyy-MM-dd}", currentCount.ToString(),
                $"{previousDate:yyyy-MM-dd}", previousCount.ToString(), percentageDelta, null);
            return new FeaturesWorkedOnInfoDto(currentCount, comparison);
        }

        protected static TotalWorkItemAgeInfoDto BuildTotalWorkItemAgeInfoDto(
            int currentAge, int previousAge, DateTime asOfDate, DateTime previousDate)
        {
            var direction = DetermineIntCountDirection(currentAge, previousAge);
            var percentageDelta = ComputePercentageDelta(currentAge, previousAge);
            var comparison = new InfoWidgetComparisonDto(
                direction, "Total Work Item Age", $"{asOfDate:yyyy-MM-dd}", currentAge.ToString(),
                $"{previousDate:yyyy-MM-dd}", previousAge.ToString(), percentageDelta, null);
            return new TotalWorkItemAgeInfoDto(currentAge, comparison);
        }

        protected static PredictabilityScoreInfoDto BuildPredictabilityScoreInfoDto(
            double currentScore, double previousScore,
            DateTime currentStart, DateTime currentEnd,
            DateTime previousStart, DateTime previousEnd)
        {
            var currentPct = (int)Math.Round(currentScore * 100);
            var previousPct = (int)Math.Round(previousScore * 100);
            string direction;
            if (Math.Abs(currentPct - previousPct) <= 5) direction = "flat";
            else if (currentPct > previousPct) direction = "up";
            else direction = "down";

            var currentLabel = $"{currentStart:yyyy-MM-dd} – {currentEnd:yyyy-MM-dd}";
            var previousLabel = $"{previousStart:yyyy-MM-dd} – {previousEnd:yyyy-MM-dd}";
            var percentageDelta = $"{(currentPct - previousPct):+#;-#;0}pp";

            var comparison = new InfoWidgetComparisonDto(
                direction, "Predictability Score", currentLabel, $"{currentPct}%",
                previousLabel, $"{previousPct}%", percentageDelta, null);
            return new PredictabilityScoreInfoDto(currentScore, comparison);
        }

        protected static CycleTimePercentilesInfoDto BuildCycleTimePercentilesInfoDto(
            List<PercentileValue> currentPercentiles,
            List<PercentileValue> previousPercentiles,
            DateTime currentStart, DateTime currentEnd,
            DateTime previousStart, DateTime previousEnd)
        {
            var percentileDtos = currentPercentiles
                .Select(p => new PercentileValueDto(p.Percentile, p.Value))
                .ToArray();

            if (currentPercentiles.Count == 0 && previousPercentiles.Count == 0)
            {
                var emptyComparison = new InfoWidgetComparisonDto(
                    "none", "Cycle Time Percentiles", null, null, null, null, null, null);
                return new CycleTimePercentilesInfoDto(percentileDtos, emptyComparison);
            }

            var detailRows = currentPercentiles
                .Select(cp =>
                {
                    var pp = previousPercentiles.FirstOrDefault(p => p.Percentile == cp.Percentile);
                    return new TrendDetailRowDto(
                        $"{cp.Percentile}th",
                        cp.Value.ToString(),
                        pp?.Value.ToString() ?? "–");
                })
                .ToArray();

            var currentMedian = currentPercentiles.FirstOrDefault(p => p.Percentile == 50)?.Value ?? 0;
            var previousMedian = previousPercentiles.FirstOrDefault(p => p.Percentile == 50)?.Value ?? 0;
            var direction = DetermineIntCountDirection(currentMedian, previousMedian);

            var currentLabel = $"{currentStart:yyyy-MM-dd} – {currentEnd:yyyy-MM-dd}";
            var previousLabel = $"{previousStart:yyyy-MM-dd} – {previousEnd:yyyy-MM-dd}";

            var comparison = new InfoWidgetComparisonDto(
                direction, "Cycle Time Percentiles", currentLabel, null,
                previousLabel, null, null, detailRows);

            return new CycleTimePercentilesInfoDto(percentileDtos, comparison);
        }

        private static string? ComputePercentageDelta(int current, int previous)
        {
            if (previous == 0) return null;
            var delta = (double)(current - previous) / previous * 100;
            var sign = delta >= 0 ? "+" : "";
            return $"{sign}{delta:F1}%";
        }

        protected static FeatureSizePercentilesInfoDto BuildFeatureSizePercentilesInfoDto(
            List<PercentileValue> currentPercentiles,
            List<PercentileValue> previousPercentiles,
            DateTime currentStart,
            DateTime currentEnd,
            DateTime previousStart,
            DateTime previousEnd)
        {
            var percentileDtos = currentPercentiles
                .Select(p => new PercentileValueDto(p.Percentile, p.Value))
                .ToArray();

            if (currentPercentiles.Count == 0 && previousPercentiles.Count == 0)
            {
                var emptyComparison = new InfoWidgetComparisonDto(
                    "none",
                    "Feature Size Percentiles",
                    null, null, null, null, null, null);
                return new FeatureSizePercentilesInfoDto(percentileDtos, emptyComparison);
            }

            var detailRows = currentPercentiles
                .Select(cp =>
                {
                    var pp = previousPercentiles.FirstOrDefault(p => p.Percentile == cp.Percentile);
                    return new TrendDetailRowDto(
                        $"{cp.Percentile}th",
                        cp.Value.ToString(),
                        pp?.Value.ToString() ?? "–");
                })
                .ToArray();

            // Direction based on median (50th) percentile comparison
            var currentMedian = currentPercentiles.FirstOrDefault(p => p.Percentile == 50)?.Value ?? 0;
            var previousMedian = previousPercentiles.FirstOrDefault(p => p.Percentile == 50)?.Value ?? 0;
            var direction = DetermineIntCountDirection(currentMedian, previousMedian);

            var currentLabel = $"{currentStart:yyyy-MM-dd} – {currentEnd:yyyy-MM-dd}";
            var previousLabel = $"{previousStart:yyyy-MM-dd} – {previousEnd:yyyy-MM-dd}";

            var comparison = new InfoWidgetComparisonDto(
                direction,
                "Feature Size Percentiles",
                currentLabel,
                null,
                previousLabel,
                null,
                null,
                detailRows);

            return new FeatureSizePercentilesInfoDto(percentileDtos, comparison);
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