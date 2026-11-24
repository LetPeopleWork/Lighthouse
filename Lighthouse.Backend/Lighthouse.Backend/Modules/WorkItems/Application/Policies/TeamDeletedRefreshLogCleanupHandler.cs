using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Modules.WorkItems.Application;

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
