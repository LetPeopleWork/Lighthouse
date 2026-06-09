using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class PortfolioMetricsService(
        ILogger<PortfolioMetricsService> logger,
        IRepository<Feature> featureRepository,
        IAppSettingService appSettingService,
        IServiceProvider serviceProvider,
        IFeatureStateTransitionRepository featureStateTransitionRepository)
        : BaseMetricsService(appSettingService.GetFeatureRefreshSettings().Interval, serviceProvider),
            IPortfolioMetricsService
    {
        private static readonly IReadOnlyList<int> DefaultPacePercentiles = [50, 70, 85, 95];

        public RunChartData GetThroughputForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Throughput for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"Throughput_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var portfolioFeatures = featureRepository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                    f.StateCategory == StateCategories.Done);

                return new RunChartData(GenerateThroughputRunChart(startDate, endDate, portfolioFeatures));
            }, logger);
        }

        public ProcessBehaviourChart GetThroughputProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(portfolio, $"ThroughputProcessBehaviourChart_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                return BuildThroughputProcessBehaviourChart(portfolio, startDate, endDate,
                    (s, e) => GetThroughputForPortfolio(portfolio, s, e));
            }, logger);
        }

        public ProcessBehaviourChart GetWipProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(portfolio, $"WipProcessBehaviourChart_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                return BuildDailyRunChartProcessBehaviourChart(portfolio, startDate, endDate,
                    (s, e) => GetFeaturesInProgressOverTimeForPortfolio(portfolio, s, e));
            }, logger);
        }

        public ProcessBehaviourChart GetTotalWorkItemAgeProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(portfolio, $"TotalWorkItemAgeProcessBehaviourChart_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
             {
                 return BuildTotalWorkItemAgeProcessBehaviourChart(portfolio, startDate, endDate,
                     (s, e) => GetTotalWorkItemAgeOverTime(portfolio, s, e));
             }, logger);
        }

        public ProcessBehaviourChart GetCycleTimeProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(portfolio, $"CycleTimeProcessBehaviourChart_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
             {
                 return BuildCycleTimeProcessBehaviourChart(portfolio, startDate, endDate,
                     (s, e) => GetFeaturesClosedInDateRange(portfolio, s, e));
             }, logger);
        }

        public ProcessBehaviourChart GetFeatureSizeProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Feature Size Process Behaviour Chart for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"FeatureSizeProcessBehaviourChart_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
             {
                 var baselineStart = portfolio.ProcessBehaviourChartBaselineStartDate;
                 var baselineEnd = portfolio.ProcessBehaviourChartBaselineEndDate;
                 var baselineConfigured = baselineStart != null || baselineEnd != null;

                 if (!baselineConfigured)
                 {
                     baselineStart = startDate;
                     baselineEnd = endDate;
                 }

                 var validation = BaselineValidationService.Validate(baselineStart, baselineEnd, portfolio.DoneItemsCutoffDays);
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

                 var baselineItems = GetFeaturesClosedInDateRange(portfolio, baselineStart!.Value, baselineEnd!.Value)
                     .Where(f => f.Size > 0)
                     .OrderBy(f => f.ClosedDate)
                     .ThenBy(f => f.Id)
                     .ToList();

                 var displayItems = GetFeaturesClosedInDateRange(portfolio, startDate, endDate)
                     .Where(f => f.Size > 0)
                     .OrderBy(f => f.ClosedDate)
                     .ThenBy(f => f.Id)
                     .ToList();

                 var baselineValues = baselineItems.Select(f => f.Size).ToArray();
                 var displayValues = displayItems.Select(f => f.Size).ToArray();

                 if (displayValues.Length == 0)
                 {
                     return new ProcessBehaviourChart
                     {
                         Status = BaselineStatus.InsufficientData,
                         StatusReason = "No closed features with a non-zero size were found in the selected date range.",
                         XAxisKind = XAxisKind.DateTime,
                         Average = 0,
                         UpperNaturalProcessLimit = 0,
                         LowerNaturalProcessLimit = 0,
                         BaselineConfigured = baselineConfigured,
                         DataPoints = [],
                     };
                 }

                 var xmrResult = XmRCalculator.Calculate(baselineValues, displayValues);

                 var dataPoints = new ProcessBehaviourChartDataPoint[displayItems.Count];
                 for (var i = 0; i < displayItems.Count; i++)
                 {
                     var feature = displayItems[i];
                     var xValue = feature.ClosedDate!.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
                     dataPoints[i] = new ProcessBehaviourChartDataPoint(xValue, feature.Size, xmrResult.SpecialCauseClassifications[i], [feature.Id]);
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
             }, logger);
        }

        public RunChartData GetFeaturesInProgressOverTimeForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Features In Progress Over Time for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"FeaturesInProgressOverTime_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var features = featureRepository.GetAllByPredicate(f =>
                        f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                        (f.StateCategory == StateCategories.Doing || f.StateCategory == StateCategories.Done))
                    .ToList();

                return new RunChartData(GenerateWorkInProgressByDay(startDate, endDate, features));
            }, logger);
        }

        private (int[] Values, int[][] WorkItemIdsPerDay) GetTotalWorkItemAgeOverTime(Portfolio portfolio, DateTime startDate,
            DateTime endDate)
        {
            logger.LogDebug("Getting Total Work Item Age Over Time for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            var features = featureRepository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                    (f.StateCategory == StateCategories.Doing || f.StateCategory == StateCategories.Done))
                .ToList();

            var wiaOverTime = GenerateTotalWorkItemAgeByDay(
                startDate,
                endDate,
                features);

            logger.LogDebug("Finished calculating Total Work Item Age Over Time for Portfolio {PortfolioName}", portfolio.Name);

            return wiaOverTime;
        }

        public RunChartData GetStartedItemsForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Started Items for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"StartedItems_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var startedItems = featureRepository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                    (f.StateCategory == StateCategories.Done || f.StateCategory == StateCategories.Doing));

                return new RunChartData(GenerateStartedRunChart(startDate, endDate, startedItems));
            }, logger);
        }

        public RunChartData GetArrivalsForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return GetStartedItemsForPortfolio(portfolio, startDate, endDate);
        }

        public ProcessBehaviourChart GetArrivalsProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return BuildDailyRunChartProcessBehaviourChart(portfolio, startDate, endDate,
                (s, e) => GetArrivalsForPortfolio(portfolio, s, e));
        }

        public ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return GetFromCacheIfExists(portfolio, $"ForecastPredictabilityScore_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var throughput = GetThroughputForPortfolio(portfolio, startDate, endDate);
                return GetMultiItemForecastPredictabilityScore(throughput);
            }, logger);
        }

        public IEnumerable<Feature> GetInProgressFeaturesForPortfolio(Portfolio portfolio, DateTime asOfDate)
        {
            logger.LogDebug("Getting WIP snapshot for Portfolio {PortfolioName} at {EndDate}", portfolio.Name, asOfDate.Date);

            return GetFromCacheIfExists(portfolio, $"WipSnapshot_{asOfDate:yyyy-MM-dd}", () =>
            {
                var features = featureRepository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                    (f.StateCategory == StateCategories.Doing || f.StateCategory == StateCategories.Done))
                    .ToList();

                return GenerateWorkInProgressByDay(asOfDate, asOfDate, features)[0].OfType<Feature>();
            }
            , logger);
        }

        public IEnumerable<PercentileValue> GetCycleTimePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Percentiles for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"CycleTimePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var closedFeaturesInDateRange = GetFeaturesClosedInDateRange(portfolio, startDate, endDate);
                var cycleTimes = closedFeaturesInDateRange.Select(f => f.CycleTime).Where(ct => ct > 0).ToList();

                if (cycleTimes.Count == 0)
                {
                    return Enumerable.Empty<PercentileValue>();
                }

                return
                [
                    new PercentileValue(50, PercentileCalculator.CalculatePercentile(cycleTimes, 50)),
                    new PercentileValue(70, PercentileCalculator.CalculatePercentile(cycleTimes, 70)),
                    new PercentileValue(85, PercentileCalculator.CalculatePercentile(cycleTimes, 85)),
                    new PercentileValue(95, PercentileCalculator.CalculatePercentile(cycleTimes, 95))
                ];
            }, logger);
        }

        public IEnumerable<PercentileValue> GetWorkItemAgePercentilesForPortfolio(Portfolio portfolio, DateTime endDate)
        {
            logger.LogDebug("Getting Work Item Age Percentiles for Portfolio {PortfolioName} at {EndDate}", portfolio.Name, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"WorkItemAgePercentiles_{endDate:yyyy-MM-dd}", () =>
            {
                var ages = GetInProgressFeaturesForPortfolio(portfolio, endDate).Select(f => f.WorkItemAge).Where(age => age > 0).ToList();

                return BuildPercentiles(ages);
            }, logger);
        }

        public IEnumerable<AgeInStatePercentilesDto> GetAgeInStatePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Age In State Percentiles for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"AgeInStatePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var completedFeatures = GetFeaturesClosedInDateRange(portfolio, startDate, endDate).ToList();
                var completedItemsWithTransitions = AssociateSyncedTransitions(completedFeatures);
                var cycleTimePercentiles = GetCycleTimePercentilesForPortfolio(portfolio, startDate, endDate).ToList();

                return ComputeAgeInStatePercentiles(completedItemsWithTransitions, portfolio.DoingStates, DefaultPacePercentiles, cycleTimePercentiles).ToList();
            }, logger);
        }

        public CumulativeStateTimeDto GetCumulativeStateTimeForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate, IReadOnlyList<int>? itemIds = null, int? definitionId = null)
        {
            logger.LogDebug("Getting Cumulative State Time for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            var scopedDefinition = definitionId is > 0
                ? portfolio.CycleTimeDefinitions.FirstOrDefault(candidate => candidate.Id == definitionId && portfolio.IsCycleTimeDefinitionValid(candidate))
                : null;
            var scopeSuffix = scopedDefinition != null ? $"_Def_{definitionId}" : string.Empty;

            return GetFromCacheIfExists(portfolio, $"CumulativeStateTime_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}{SelectionCacheSuffix(itemIds)}{scopeSuffix}", () =>
            {
                var candidateItems = NarrowToSelectedItems(ResolveCumulativeStateTimeCandidates(portfolio, startDate, endDate), itemIds);
                var stateOrder = ResolveCumulativeStateOrder(portfolio, scopedDefinition);

                var states = ComputeCumulativeStateTime(candidateItems, stateOrder, endDate);
                return new CumulativeStateTimeDto(states);
            }, logger);
        }

        private static IReadOnlyList<string> ResolveCumulativeStateOrder(Portfolio portfolio, CycleTimeDefinition? scopedDefinition)
        {
            if (scopedDefinition == null)
            {
                return BuildCumulativeWorkflowStateOrder(portfolio);
            }

            var allStatesInOrder = portfolio.AllStates.ToList();
            var startState = ResolveBoundaryState(portfolio, allStatesInOrder, scopedDefinition.StartState);
            var endState = ResolveBoundaryState(portfolio, allStatesInOrder, scopedDefinition.EndState);
            return ScopedCumulativeStateOrder(allStatesInOrder, startState, endState);
        }

        public IReadOnlyList<CycleTimeFeature> GetNamedCycleTimeDataForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Named Cycle Time Data for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            var allStatesInOrder = portfolio.AllStates.ToList();
            var resolvedDefinitions = ResolveValidNamedDefinitions(portfolio, allStatesInOrder);

            var closedFeatures = GetFeaturesClosedInDateRange(portfolio, startDate, endDate).ToList();
            var itemsWithTransitions = AssociateSyncedTransitions(closedFeatures);
            var namedByItemId = itemsWithTransitions.ToDictionary(
                item => item.Id,
                item => (IReadOnlyList<NamedCycleTimeValue>)NamedValuesForItem(item, allStatesInOrder, resolvedDefinitions));

            return closedFeatures
                .Select(feature => new CycleTimeFeature(
                    feature,
                    namedByItemId.TryGetValue(feature.Id, out var named) ? named : []))
                .ToList();
        }

        public IEnumerable<PercentileValue> GetNamedCycleTimePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate, int definitionId)
        {
            logger.LogDebug("Getting Named Cycle Time Percentiles for Portfolio {PortfolioName} definition {DefinitionId} between {StartDate} and {EndDate}", portfolio.Name, definitionId, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"NamedCycleTimePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}_Def_{definitionId}", () =>
                BuildPercentiles(ComputeNamedDurations(portfolio, startDate, endDate, definitionId)), logger);
        }

        private List<int> ComputeNamedDurations(Portfolio portfolio, DateTime startDate, DateTime endDate, int definitionId)
        {
            var definition = portfolio.CycleTimeDefinitions.FirstOrDefault(candidate => candidate.Id == definitionId);
            if (definition == null || !portfolio.IsCycleTimeDefinitionValid(definition))
            {
                return [];
            }

            var allStatesInOrder = portfolio.AllStates.ToList();
            var startState = ResolveBoundaryState(portfolio, allStatesInOrder, definition.StartState);
            var endState = ResolveBoundaryState(portfolio, allStatesInOrder, definition.EndState);

            var closedFeatures = GetFeaturesClosedInDateRange(portfolio, startDate, endDate).ToList();
            var itemsWithTransitions = AssociateSyncedTransitions(closedFeatures);

            return itemsWithTransitions
                .Select(item => NamedCycleTimeDays(item, allStatesInOrder, startState, endState))
                .Where(days => days.HasValue)
                .Select(days => days!.Value)
                .ToList();
        }

        public CumulativeStateTimeItemsDto GetCumulativeStateTimeItemsForPortfolio(Portfolio portfolio, string state, DateTime startDate, DateTime endDate, IReadOnlyList<int>? itemIds = null)
        {
            logger.LogDebug("Getting Cumulative State Time Items for Portfolio {PortfolioName} in state {State} between {StartDate} and {EndDate}", portfolio.Name, state, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"CumulativeStateTime_Items_{state}_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}{SelectionCacheSuffix(itemIds)}", () =>
            {
                var candidateItems = NarrowToSelectedItems(ResolveCumulativeStateTimeCandidates(portfolio, startDate, endDate), itemIds);
                var items = ComputeCumulativeStateTimeItems(candidateItems, state, endDate)
                    .OrderByDescending(item => item.DaysContributed)
                    .ToList();

                return new CumulativeStateTimeItemsDto(state, items);
            }, logger);
        }

        public CumulativeStateTimeCandidatesDto GetCumulativeStateTimeCandidatesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cumulative State Time Candidates for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"CumulativeStateTime_Candidates_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var candidateItems = ResolveCumulativeStateTimeCandidates(portfolio, startDate, endDate);
                return new CumulativeStateTimeCandidatesDto(ProjectCumulativeStateTimeCandidates(candidateItems));
            }, logger);
        }

        private List<WorkItem> ResolveCumulativeStateTimeCandidates(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            var portfolioFeatures = featureRepository.GetAllByPredicate(f => f.Portfolios.Any(p => p.Id == portfolio.Id)).ToList();
            var itemsWithTransitions = AssociateSyncedTransitionsPreservingCurrentState(portfolioFeatures);

            return itemsWithTransitions
                .Where(item => item.StateCategory != StateCategories.ToDo)
                .Where(item => IntersectsWindow(item, startDate, endDate) || IsInFlightAtWindowEnd(item, endDate))
                .ToList();
        }

        private List<WorkItem> AssociateSyncedTransitionsPreservingCurrentState(IReadOnlyCollection<Feature> features)
        {
            var featureIds = features.Select(feature => feature.Id).ToHashSet();
            var transitionsByFeature = GroupTransitionsByItem(featureStateTransitionRepository
                .GetAllByPredicate(transition => featureIds.Contains(transition.FeatureId))
                .AsEnumerable()
                .Select(transition => (transition.FeatureId, ToWorkItemStateTransition(transition))));

            return features
                .Select(feature => new WorkItem
                {
                    Id = feature.Id,
                    ReferenceId = feature.ReferenceId,
                    ParentReferenceId = feature.ParentReferenceId,
                    Name = feature.Name,
                    Type = feature.Type,
                    State = feature.State,
                    StateCategory = feature.StateCategory,
                    Url = feature.Url,
                    StartedDate = feature.StartedDate,
                    ClosedDate = feature.ClosedDate,
                    CurrentStateEnteredAt = feature.CurrentStateEnteredAt,
                    SyncedTransitions = transitionsByFeature.TryGetValue(feature.Id, out var transitions) ? transitions : [],
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

        private static List<string> BuildCumulativeWorkflowStateOrder(Portfolio portfolio)
        {
            return [.. portfolio.DoingStates];
        }

        private List<WorkItem> AssociateSyncedTransitions(IReadOnlyCollection<Feature> completedFeatures)
        {
            var completedFeatureIds = completedFeatures.Select(feature => feature.Id).ToHashSet();
            var transitionsByFeature = GroupTransitionsByItem(featureStateTransitionRepository
                .GetAllByPredicate(transition => completedFeatureIds.Contains(transition.FeatureId))
                .AsEnumerable()
                .Select(transition => (transition.FeatureId, ToWorkItemStateTransition(transition))));

            return completedFeatures
                .Select(feature => new WorkItem
                {
                    Id = feature.Id,
                    StartedDate = feature.StartedDate,
                    SyncedTransitions = transitionsByFeature.TryGetValue(feature.Id, out var transitions) ? transitions : [],
                })
                .ToList();
        }

        private static WorkItemStateTransition ToWorkItemStateTransition(FeatureStateTransition transition)
        {
            return new WorkItemStateTransition
            {
                WorkItemId = transition.FeatureId,
                FromState = transition.FromState,
                ToState = transition.ToState,
                TransitionedAt = transition.TransitionedAt,
            };
        }

        public IEnumerable<Feature> GetCycleTimeDataForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Data for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFeaturesClosedInDateRange(portfolio, startDate, endDate).ToList();
        }

        public IEnumerable<PercentileValue> GetSizePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Size Percentiles for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"SizePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var closedFeaturesInDateRange = GetFeaturesClosedInDateRange(portfolio, startDate, endDate);
                var sizes = closedFeaturesInDateRange.Select(f => f.Size).Where(s => s > 0).ToList();

                if (sizes.Count == 0)
                {
                    return Enumerable.Empty<PercentileValue>();
                }

                return
                [
                    new PercentileValue(50, PercentileCalculator.CalculatePercentile(sizes, 50)),
                    new PercentileValue(70, PercentileCalculator.CalculatePercentile(sizes, 70)),
                    new PercentileValue(85, PercentileCalculator.CalculatePercentile(sizes, 85)),
                    new PercentileValue(95, PercentileCalculator.CalculatePercentile(sizes, 95))
                ];
            }, logger);
        }

        public IEnumerable<Feature> GetAllFeaturesForSizeChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting All Features For Size Chart for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            var allFeatures = featureRepository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                    (f.StateCategory == StateCategories.Done ||
                     f.StateCategory == StateCategories.ToDo ||
                     f.StateCategory == StateCategories.Doing))
                .ToList();

            // Filter to only features that were closed in the date range OR are currently in To Do/Doing state
            return allFeatures.Where(f =>
                (f.StateCategory == StateCategories.Done &&
                 f.ClosedDate.HasValue &&
                 f.ClosedDate.Value.Date >= startDate.Date &&
                 f.ClosedDate.Value.Date <= endDate.Date) ||
                f.StateCategory == StateCategories.ToDo ||
                f.StateCategory == StateCategories.Doing
            ).ToList();
        }

        public EstimationVsCycleTimeResponse GetEstimationVsCycleTimeData(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Estimation vs Cycle Time Data for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"EstimationVsCycleTime_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var closedFeatures = GetFeaturesClosedInDateRange(portfolio, startDate, endDate);
                return BuildEstimationVsCycleTimeResponse(portfolio, closedFeatures);
            }, logger);
        }

        public FeatureSizeEstimationResponse GetFeatureSizeEstimationData(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Feature Size Estimation Data for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            var allFeatures = GetAllFeaturesForSizeChart(portfolio, startDate, endDate);
            return BuildFeatureSizeEstimationResponse(portfolio, allFeatures);
        }

        public int GetTotalWorkItemAge(Portfolio portfolio, DateTime endDate)
        {
            logger.LogDebug("Getting Total Work Item Age snapshot for Portfolio {PortfolioName} at {EndDate}", portfolio.Name, endDate.Date);

            var totalWorkItemAge = GetFromCacheIfExists(portfolio, $"TotalWorkItemAge_{endDate:yyyy-MM-dd}", () =>
             {
                 var features = featureRepository.GetAllByPredicate(f =>
                     f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                     (f.StateCategory == StateCategories.Doing || f.StateCategory == StateCategories.Done));

                 var (values, _) = GenerateTotalWorkItemAgeByDay(endDate, endDate, features);
                 return new InfoMetric(values[0]);
             }, logger);

            return totalWorkItemAge.Value;
        }

        public ThroughputInfoDto GetThroughputInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Throughput Info for Portfolio {PortfolioName} from {StartDate} to {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"ThroughputInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
             {
                 var currentThroughput = GetThroughputForPortfolio(portfolio, startDate, endDate);
                 var periodDays = (endDate.Date - startDate.Date).Days + 1;
                 var previousEnd = startDate.AddDays(-1);
                 var previousStart = startDate.AddDays(-periodDays);
                 var previousThroughput = GetThroughputForPortfolio(portfolio, previousStart, previousEnd);

                 return BuildThroughputInfoDto(currentThroughput.Total, previousThroughput.Total, periodDays, startDate, endDate, previousStart, previousEnd);
             }, logger);
        }

        public ArrivalsInfoDto GetArrivalsInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Arrivals Info for Portfolio {PortfolioName} from {StartDate} to {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"ArrivalsInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
             {
                 var currentArrivals = GetArrivalsForPortfolio(portfolio, startDate, endDate);
                 var periodDays = (endDate.Date - startDate.Date).Days + 1;
                 var previousEnd = startDate.AddDays(-1);
                 var previousStart = startDate.AddDays(-periodDays);
                 var previousArrivals = GetArrivalsForPortfolio(portfolio, previousStart, previousEnd);

                 return BuildArrivalsInfoDto(currentArrivals.Total, previousArrivals.Total, periodDays, startDate, endDate, previousStart, previousEnd);
             }, logger);
        }

        public FeatureSizePercentilesInfoDto GetFeatureSizePercentilesInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Feature Size Percentiles Info for Portfolio {PortfolioName} from {StartDate} to {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"FeatureSizePercentilesInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentPercentiles = GetSizePercentilesForPortfolio(portfolio, startDate, endDate).ToList();
                var periodDays = (endDate.Date - startDate.Date).Days + 1;
                var previousEnd = startDate.AddDays(-1);
                var previousStart = startDate.AddDays(-periodDays);
                var previousPercentiles = GetSizePercentilesForPortfolio(portfolio, previousStart, previousEnd).ToList();

                return BuildFeatureSizePercentilesInfoDto(currentPercentiles, previousPercentiles, currentStart: startDate, currentEnd: endDate, previousStart: previousStart, previousEnd: previousEnd);
            }, logger);
        }

        public WipOverviewInfoDto GetWipOverviewInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting WIP Overview Info for Portfolio {PortfolioName} from {StartDate} to {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"WipOverviewInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentCount = GetInProgressFeaturesForPortfolio(portfolio, endDate).Count();
                var previousCount = GetInProgressFeaturesForPortfolio(portfolio, startDate).Count();
                return BuildWipOverviewInfoDto(currentCount, previousCount, endDate, startDate);
            }, logger);
        }

        public TotalWorkItemAgeInfoDto GetTotalWorkItemAgeInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Total Work Item Age Info for Portfolio {PortfolioName} from {StartDate} to {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"TotalWorkItemAgeInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentAge = GetTotalWorkItemAge(portfolio, endDate);
                var previousAge = GetTotalWorkItemAge(portfolio, startDate);
                return BuildTotalWorkItemAgeInfoDto(currentAge, previousAge, endDate, startDate);
            }, logger);
        }

        public PredictabilityScoreInfoDto GetPredictabilityScoreInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Predictability Score Info for Portfolio {PortfolioName} from {StartDate} to {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"PredictabilityScoreInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentScore = GetMultiItemForecastPredictabilityScoreForPortfolio(portfolio, startDate, endDate);
                var periodDays = (endDate.Date - startDate.Date).Days + 1;
                var previousEnd = startDate.AddDays(-1);
                var previousStart = startDate.AddDays(-periodDays);
                var previousScore = GetMultiItemForecastPredictabilityScoreForPortfolio(portfolio, previousStart, previousEnd);
                return BuildPredictabilityScoreInfoDto(currentScore.PredictabilityScore, previousScore.PredictabilityScore, startDate, endDate, previousStart, previousEnd);
            }, logger);
        }

        public CycleTimePercentilesInfoDto GetCycleTimePercentilesInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Percentiles Info for Portfolio {PortfolioName} from {StartDate} to {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"CycleTimePercentilesInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var currentPercentiles = GetCycleTimePercentilesForPortfolio(portfolio, startDate, endDate).ToList();
                var periodDays = (endDate.Date - startDate.Date).Days + 1;
                var previousEnd = startDate.AddDays(-1);
                var previousStart = startDate.AddDays(-periodDays);
                var previousPercentiles = GetCycleTimePercentilesForPortfolio(portfolio, previousStart, previousEnd).ToList();
                return BuildCycleTimePercentilesInfoDto(currentPercentiles, previousPercentiles, startDate, endDate, previousStart, previousEnd);
            }, logger);
        }

        public FlowEfficiencyInfoDto GetFlowEfficiencyInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Flow Efficiency Info for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFromCacheIfExists(portfolio, $"FlowEfficiencyInfo_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}", () =>
            {
                var candidateItems = ResolveCumulativeStateTimeCandidates(portfolio, startDate, endDate);
                var workflowStateOrder = BuildCumulativeWorkflowStateOrder(portfolio);
                var doingStateRows = ComputeCumulativeStateTime(candidateItems, workflowStateOrder, endDate);

                var expandedWaitStates = portfolio.GetRawStatesForCategory(portfolio.WaitStates);
                return ComputeFlowEfficiency(doingStateRows, expandedWaitStates, portfolio.WaitStates.Count > 0);
            }, logger);
        }

        public void InvalidatePortfolioMetrics(Portfolio portfolio)
        {
            InvalidateMetrics(portfolio, logger);
        }

        private IEnumerable<Feature> GetFeaturesClosedInDateRange(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            var closedFeaturesOfPortfolio = featureRepository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                    f.StateCategory == StateCategories.Done)
                .ToList();

            return closedFeaturesOfPortfolio
                .Where(f => f.ClosedDate.HasValue &&
                           f.ClosedDate.Value.Date >= startDate.Date &&
                           f.ClosedDate.Value.Date <= endDate.Date);
        }
    }
}