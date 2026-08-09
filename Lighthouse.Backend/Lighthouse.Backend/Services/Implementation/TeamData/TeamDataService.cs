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
        public async Task<SyncOutcome> UpdateTeamData(Team team)
        {
            // Stryker disable once all: entry trace; what this pass reports is the SyncOutcome it returns, which the scenarios assert.
            logger.LogDebug("Updating Team Data for {TeamName}", team.Name);

            var outcome = await workItemService.UpdateWorkItemsForTeam(team);
            await teamMetricsService.UpdateTeamMetrics(team);

            await domainEventDispatcher.PublishAsync(new TeamDataRefreshed(team.Id));

            // Stryker disable once all: exit trace of the same pass; the return below is the fact that matters.
            logger.LogDebug("Finished updating Team Data for {TeamName}", team.Name);

            return outcome;
        }
    }
}
