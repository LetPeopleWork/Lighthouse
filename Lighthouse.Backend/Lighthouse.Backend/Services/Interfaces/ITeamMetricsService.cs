using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ITeamMetricsService
    {
        Throughput GetCurrentThroughputForTeam(Team team);

        Throughput GetThroughputForTeam(Team team, DateTime startDate, DateTime endDate);

        List<string> GetCurrentFeaturesInProgressForTeam(Team team);

        void InvalidateTeamMetrics(Team team);
    }
}
