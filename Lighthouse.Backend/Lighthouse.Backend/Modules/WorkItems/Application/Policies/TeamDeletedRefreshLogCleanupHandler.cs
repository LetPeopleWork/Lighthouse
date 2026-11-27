using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Platform.DomainEvents;

namespace Lighthouse.Backend.Modules.WorkItems.Application.Policies
{
    public class TeamDeletedRefreshLogCleanupHandler(IRefreshLogService refreshLogService) : IDomainEventHandler<TeamDeleted>
    {
        public Task HandleAsync(TeamDeleted domainEvent, CancellationToken cancellationToken)
        {
            return refreshLogService.RemoveRefreshLogsForEntity(RefreshType.Team, domainEvent.TeamId);
        }
    }
}
