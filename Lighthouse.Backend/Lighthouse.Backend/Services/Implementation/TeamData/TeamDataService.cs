using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.WorkItems;

namespace Lighthouse.Backend.Services.Implementation.TeamData
{
    public class TeamDataService(
        ILogger<TeamDataService> logger,
        ITeamMetricsService teamMetricsService,
        IWorkItemService workItemService,
        IDomainEventDispatcher domainEventDispatcher)
        : ITeamDataService
    {
        public async Task UpdateTeamData(Team team)
        {
            logger.LogInformation("Updating Team Data for {TeamName}", team.Name);

            await workItemService.UpdateWorkItemsForTeam(team);
            await teamMetricsService.UpdateTeamMetrics(team);

            await domainEventDispatcher.PublishAsync(new TeamDataRefreshed(team.Id));

            logger.LogInformation("Finished updating Team Data for {TeamName}", team.Name);
        }
    }
}
