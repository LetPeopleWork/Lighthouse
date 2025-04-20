using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ITeamMetricsService
    {
        RunChartData GetCurrentThroughputForTeam(Team team);

        RunChartData GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate);

        RunChartData GetWorkInProgressOverTimeForTeam(Team team, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetCurrentFeaturesInProgressForTeam(Team team);

        IEnumerable<WorkItem> GetCurrentWipForTeam(Team team);
        
        IEnumerable<WorkItem> GetClosedItemsForTeam(Team team, DateTime startDate, DateTime endDate);

        IEnumerable<PercentileValue> GetCycleTimePercentilesForTeam(Team team, DateTime startDate, DateTime endDate);

        void InvalidateTeamMetrics(Team team);

        Task UpdateTeamMetrics(Team team);
    }
}
