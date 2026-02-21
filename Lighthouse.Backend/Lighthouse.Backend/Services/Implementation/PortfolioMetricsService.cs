using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class PortfolioMetricsService(
        ILogger<PortfolioMetricsService> logger,
        IRepository<Feature> featureRepository,
        IAppSettingService appSettingService,
        IServiceProvider serviceProvider)
        : BaseMetricsService(appSettingService.GetFeatureRefreshSettings().Interval, serviceProvider),
            IPortfolioMetricsService
    {
        private readonly string inProgressFeaturesMetricIdentifier = "InProgressFeatures";

        public RunChartData GetThroughputForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Throughput for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            var portfolioFeatures = featureRepository.GetAllByPredicate(f =>
                f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                f.StateCategory == StateCategories.Done);

            var throughputByDay = GenerateThroughputRunChart(
                startDate,
                endDate,
                portfolioFeatures);

            logger.LogDebug("Finished calculating Throughput for Portfolio {PortfolioName}", portfolio.Name);

            return new RunChartData(throughputByDay);
        }

        public ProcessBehaviourChart GetThroughputProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return BuildThroughputProcessBehaviourChart(portfolio, startDate, endDate,
                (s, e) => GetThroughputForPortfolio(portfolio, s, e));
        }

        public ProcessBehaviourChart GetWipProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return BuildDailyRunChartProcessBehaviourChart(portfolio, startDate, endDate,
                (s, e) => GetFeaturesInProgressOverTimeForPortfolio(portfolio, s, e));
        }

        public ProcessBehaviourChart GetTotalWorkItemAgeProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return BuildTotalWorkItemAgeProcessBehaviourChart(portfolio, startDate, endDate,
                (s, e) => GetTotalWorkItemAgeOverTime(portfolio, s, e));
        }

        public ProcessBehaviourChart GetCycleTimeProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            return BuildCycleTimeProcessBehaviourChart(portfolio, startDate, endDate,
                (s, e) => GetFeaturesClosedInDateRange(portfolio, s, e));
        }

        public ProcessBehaviourChart GetFeatureSizeProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Feature Size Process Behaviour Chart for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

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
                var xValue = feature.ClosedDate!.Value.ToString("yyyy-MM-ddTHH:mm:ss");
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
        }

        public RunChartData GetFeaturesInProgressOverTimeForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Features In Progress Over Time for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            var features = featureRepository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                    (f.StateCategory == StateCategories.Doing || f.StateCategory == StateCategories.Done))
                .ToList();

            var wipOverTime = GenerateWorkInProgressByDay(
                startDate,
                endDate,
                features);

            logger.LogDebug("Finished calculating Features In Progress Over Time for Portfolio {PortfolioName}", portfolio.Name);

            return new RunChartData(wipOverTime);
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

            var startedItems = featureRepository.GetAllByPredicate(f => f.Portfolios.Any(p => p.Id == portfolio.Id) && (f.StateCategory == StateCategories.Done || f.StateCategory == StateCategories.Doing));
            var startedItemsByDay = GenerateStartedRunChart(startDate, endDate, startedItems);

            var throughput = new RunChartData(startedItemsByDay);

            return throughput;
        }

        public ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            var throughput = GetThroughputForPortfolio(portfolio, startDate, endDate);
            return GetMultiItemForecastPredictabilityScore(throughput, startDate, endDate);
        }

        public IEnumerable<Feature> GetInProgressFeaturesForPortfolio(Portfolio portfolio)
        {
            logger.LogDebug("Getting In Progress Features for Portfolio {PortfolioName}", portfolio.Name);

            return GetFromCacheIfExists<IEnumerable<Feature>, Portfolio>(portfolio, inProgressFeaturesMetricIdentifier, () =>
            {
                var features = featureRepository.GetAllByPredicate(f =>
                    f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                    f.StateCategory == StateCategories.Doing)
                    .ToList();

                logger.LogDebug("Found {FeatureCount} In Progress Features for Portfolio {PortfolioName}", features.Count, portfolio.Name);
                return features;
            }, logger);
        }

        public IEnumerable<PercentileValue> GetCycleTimePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Percentiles for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            var closedFeaturesInDateRange = GetFeaturesClosedInDateRange(portfolio, startDate, endDate);
            var cycleTimes = closedFeaturesInDateRange.Select(f => f.CycleTime).Where(ct => ct > 0).ToList();

            if (cycleTimes.Count == 0)
            {
                logger.LogDebug("No closed features found in the specified date range for Portfolio {PortfolioName}", portfolio.Name);
                return [];
            }

            return [
                new PercentileValue(50, PercentileCalculator.CalculatePercentile(cycleTimes, 50)),
                new PercentileValue(70, PercentileCalculator.CalculatePercentile(cycleTimes, 70)),
                new PercentileValue(85, PercentileCalculator.CalculatePercentile(cycleTimes, 85)),
                new PercentileValue(95, PercentileCalculator.CalculatePercentile(cycleTimes, 95))
            ];
        }

        public IEnumerable<Feature> GetCycleTimeDataForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Cycle Time Data for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            return GetFeaturesClosedInDateRange(portfolio, startDate, endDate).ToList();
        }

        public IEnumerable<PercentileValue> GetSizePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            logger.LogDebug("Getting Size Percentiles for Portfolio {PortfolioName} between {StartDate} and {EndDate}", portfolio.Name, startDate.Date, endDate.Date);

            var closedFeaturesInDateRange = GetFeaturesClosedInDateRange(portfolio, startDate, endDate);
            var sizes = closedFeaturesInDateRange.Select(f => f.Size).Where(s => s > 0).ToList();

            if (sizes.Count == 0)
            {
                logger.LogDebug("No closed features found in the specified date range for Portfolio {PortfolioName}", portfolio.Name);
                return [];
            }

            return [
                new PercentileValue(50, PercentileCalculator.CalculatePercentile(sizes, 50)),
                new PercentileValue(70, PercentileCalculator.CalculatePercentile(sizes, 70)),
                new PercentileValue(85, PercentileCalculator.CalculatePercentile(sizes, 85)),
                new PercentileValue(95, PercentileCalculator.CalculatePercentile(sizes, 95))
            ];
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

            var closedFeatures = GetFeaturesClosedInDateRange(portfolio, startDate, endDate);
            return BuildEstimationVsCycleTimeResponse(portfolio, closedFeatures);
        }

        public int GetTotalWorkItemAge(Portfolio portfolio)
        {
            logger.LogDebug("Getting Total Work Item Age for Portfolio {PortfolioName}", portfolio.Name);

            var inProgressFeatures = featureRepository.GetAllByPredicate(f =>
                f.Portfolios.Any(p => p.Id == portfolio.Id) &&
                f.StateCategory == StateCategories.Doing);

            var totalAge = inProgressFeatures.Sum(feature => feature.WorkItemAge);

            logger.LogDebug("Total Work Item Age for Portfolio {PortfolioName}: {TotalAge} days", portfolio.Name, totalAge);

            return totalAge;
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