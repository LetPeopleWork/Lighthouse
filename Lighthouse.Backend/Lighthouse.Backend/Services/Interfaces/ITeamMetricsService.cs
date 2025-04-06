using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.DTO.Metrics;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ITeamMetricsService
    {
        Throughput GetCurrentThroughputForTeam(Team team);

        Throughput GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate);

        IEnumerable<WorkItemDto> GetCurrentFeaturesInProgressForTeam(Team team);

        IEnumerable<WorkItemDto> GetCurrentWipForTeam(Team team);
        
        IEnumerable<WorkItemDto> GetCycleTimeDataForTeam(Team team, DateTime startDate, DateTime endDate);

        IEnumerable<PercentileValue> GetCycleTimePercentilesForTeam(Team team, DateTime startDate, DateTime endDate);

        void InvalidateTeamMetrics(Team team);
    }
}
