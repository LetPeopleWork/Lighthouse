using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ITeamMetricsService
    {
        Throughput GetThroughputForTeam(Team team);

        List<string> GetFeaturesInProgressForTeam(Team team);

        void InvalidateTeamMetrics(Team team);
    }
}
