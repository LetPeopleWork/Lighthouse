using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application.Policies
{
    public class TeamDataRefreshedTeamMetricsUpdateHandler(
        IRepository<Team> teamRepository,
        ITeamMetricsService teamMetricsService)
        : IDomainEventHandler<TeamDataRefreshed>
    {
        public async Task HandleAsync(TeamDataRefreshed domainEvent, CancellationToken cancellationToken)
        {
            var team = teamRepository.GetById(domainEvent.TeamId);
            if (team == null)
            {
                return;
            }

            await teamMetricsService.UpdateTeamMetrics(team);
        }
    }
}
