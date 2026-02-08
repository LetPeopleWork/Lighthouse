using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ITeamMetricsService
    {
        RunChartData GetCurrentThroughputForTeam(Team team);

        ProcessBehaviourChart GetThroughputProcessBehaviourChart(Team team, DateTime startDate, DateTime endDate);

        RunChartData GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate);

        RunChartData GetStartedItemsForTeam(Team team, DateTime startDate, DateTime endDate);

        RunChartData GetCreatedItemsForTeam(Team team, IEnumerable<string> workItemTypes, DateTime startDate, DateTime endDate);

        RunChartData GetWorkInProgressOverTimeForTeam(Team team, DateTime startDate, DateTime endDate);

        ForecastPredictabilityScore GetMultiItemForecastPredictabilityScoreForTeam(Team team, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetCurrentFeaturesInProgressForTeam(Team team);

        IEnumerable<WorkItem> GetCurrentWipForTeam(Team team);
        
        IEnumerable<WorkItem> GetClosedItemsForTeam(Team team, DateTime startDate, DateTime endDate);

        IEnumerable<PercentileValue> GetCycleTimePercentilesForTeam(Team team, DateTime startDate, DateTime endDate);

        int GetTotalWorkItemAge(Team team);

        void InvalidateTeamMetrics(Team team);

        Task UpdateTeamMetrics(Team team);
    }
}
