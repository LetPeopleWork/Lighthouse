using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Platform.DomainEvents;

namespace Lighthouse.Backend.Modules.WorkItems.Application
{
    public class TeamDataService(
        ILogger<TeamDataService> logger,
        IWorkItemService workItemService,
        IDomainEventDispatcher domainEventDispatcher)
        : ITeamDataService
    {
        public async Task<SyncOutcome> UpdateTeamData(Team team)
        {
            // Stryker disable once all: entry trace; what this pass reports is the SyncOutcome it returns, which the scenarios assert.
            logger.LogDebug("Updating Team Data for {TeamName}", team.Name);

            var outcome = await workItemService.UpdateWorkItemsForTeam(team);

            await domainEventDispatcher.PublishAsync(new TeamDataRefreshed(team.Id));

            // Stryker disable once all: exit trace of the same pass; the return below is the fact that matters.
            logger.LogDebug("Finished updating Team Data for {TeamName}", team.Name);

            return outcome;
        }
    }
}
