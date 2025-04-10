using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ITeamMetricsService
    {
        Throughput GetCurrentThroughputForTeam(Team team);

        Throughput GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate);

        IEnumerable<Feature> GetCurrentFeaturesInProgressForTeam(Team team);

        IEnumerable<WorkItem> GetCurrentWipForTeam(Team team);
        
        IEnumerable<WorkItem> GetClosedItemsForTeam(Team team, DateTime startDate, DateTime endDate);

        IEnumerable<PercentileValue> GetCycleTimePercentilesForTeam(Team team, DateTime startDate, DateTime endDate);

        void InvalidateTeamMetrics(Team team);
    }
}
