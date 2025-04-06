using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.DTO.Metrics;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ITeamMetricsService
    {
        Throughput GetCurrentThroughputForTeam(Team team);

        Throughput GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate);

        List<WorkItemDto> GetCurrentFeaturesInProgressForTeam(Team team);

        List<WorkItemDto> GetCurrentWipForTeam(Team team);

        void InvalidateTeamMetrics(Team team);
    }
}
