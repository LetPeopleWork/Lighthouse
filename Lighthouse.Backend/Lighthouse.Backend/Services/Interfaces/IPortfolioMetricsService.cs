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

        RunChartData GetThroughputForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        RunChartData GetFeaturesInProgressOverTimeForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        RunChartData GetStartedItemsForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetInProgressFeaturesForPortfolio(Portfolio portfolio);

        IEnumerable<PercentileValue> GetCycleTimePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetCycleTimeDataForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        IEnumerable<PercentileValue> GetSizePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetAllFeaturesForSizeChart(Portfolio portfolio, DateTime startDate, DateTime endDate);

        int GetTotalWorkItemAge(Portfolio portfolio);

        void InvalidatePortfolioMetrics(Portfolio portfolio);
    }
}