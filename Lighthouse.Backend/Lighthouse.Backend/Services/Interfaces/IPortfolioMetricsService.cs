using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IPortfolioMetricsService
    {
        ProcessBehaviourChart GetThroughputProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate);

        ProcessBehaviourChart GetWipProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate);

        ProcessBehaviourChart GetTotalWorkItemAgeProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate);

        ProcessBehaviourChart GetCycleTimeProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate);

        ProcessBehaviourChart GetFeatureSizeProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate);

        RunChartData GetThroughputForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        RunChartData GetFeaturesInProgressOverTimeForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        RunChartData GetStartedItemsForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        RunChartData GetArrivalsForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        ProcessBehaviourChart GetArrivalsProcessBehaviourChart(Portfolio portfolio, DateTime startDate, DateTime endDate);

        ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetInProgressFeaturesForPortfolio(Portfolio portfolio, DateTime asOfDate);

        IEnumerable<PercentileValue> GetCycleTimePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetCycleTimeDataForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        IEnumerable<PercentileValue> GetSizePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetAllFeaturesForSizeChart(Portfolio portfolio, DateTime startDate, DateTime endDate);

        EstimationVsCycleTimeResponse GetEstimationVsCycleTimeData(Portfolio portfolio, DateTime startDate, DateTime endDate);

        FeatureSizeEstimationResponse GetFeatureSizeEstimationData(Portfolio portfolio, DateTime startDate, DateTime endDate);

        int GetTotalWorkItemAge(Portfolio portfolio, DateTime endDate);

        ThroughputInfoDto GetThroughputInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        ArrivalsInfoDto GetArrivalsInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        FeatureSizePercentilesInfoDto GetFeatureSizePercentilesInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        WipOverviewInfoDto GetWipOverviewInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        TotalWorkItemAgeInfoDto GetTotalWorkItemAgeInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        PredictabilityScoreInfoDto GetPredictabilityScoreInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        CycleTimePercentilesInfoDto GetCycleTimePercentilesInfoForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        void InvalidatePortfolioMetrics(Portfolio portfolio);
    }
}