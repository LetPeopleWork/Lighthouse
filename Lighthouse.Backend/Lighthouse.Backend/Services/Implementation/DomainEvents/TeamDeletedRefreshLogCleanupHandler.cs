using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class TeamDeletedRefreshLogCleanupHandler(IRefreshLogService refreshLogService) : IDomainEventHandler<TeamDeleted>
    {
        public Task HandleAsync(TeamDeleted domainEvent, CancellationToken cancellationToken)
        {
            return refreshLogService.RemoveRefreshLogsForEntity(RefreshType.Team, domainEvent.TeamId);
        }
    }
}
