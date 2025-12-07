using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IProjectMetricsService
    {
        RunChartData GetThroughputForProject(Portfolio project, DateTime startDate, DateTime endDate);

        RunChartData GetFeaturesInProgressOverTimeForProject(Portfolio project, DateTime startDate, DateTime endDate);

        RunChartData GetStartedItemsForProject(Portfolio project, DateTime startDate, DateTime endDate);

        ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForProject(Portfolio project, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetInProgressFeaturesForProject(Portfolio project);

        IEnumerable<PercentileValue> GetCycleTimePercentilesForProject(Portfolio project, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetCycleTimeDataForProject(Portfolio project, DateTime startDate, DateTime endDate);

        IEnumerable<PercentileValue> GetSizePercentilesForProject(Portfolio project, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetAllFeaturesForSizeChart(Portfolio project, DateTime startDate, DateTime endDate);

        int GetTotalWorkItemAge(Portfolio project);

        void InvalidateProjectMetrics(Portfolio project);
    }
}