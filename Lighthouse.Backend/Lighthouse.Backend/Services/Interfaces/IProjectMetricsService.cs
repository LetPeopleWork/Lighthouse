using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IProjectMetricsService
    {
        RunChartData GetThroughputForProject(Project project, DateTime startDate, DateTime endDate);

        RunChartData GetFeaturesInProgressOverTimeForProject(Project project, DateTime startDate, DateTime endDate);

        RunChartData GetStartedItemsForProject(Project project, DateTime startDate, DateTime endDate);

        ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForProject(Project project, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetInProgressFeaturesForProject(Project project);

        IEnumerable<PercentileValue> GetCycleTimePercentilesForProject(Project project, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetCycleTimeDataForProject(Project project, DateTime startDate, DateTime endDate);

        IEnumerable<PercentileValue> GetSizePercentilesForProject(Project project, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetAllFeaturesForSizeChart(Project project, DateTime startDate, DateTime endDate);

        int GetTotalWorkItemAge(Project project);

        void InvalidateProjectMetrics(Project project);
    }
}