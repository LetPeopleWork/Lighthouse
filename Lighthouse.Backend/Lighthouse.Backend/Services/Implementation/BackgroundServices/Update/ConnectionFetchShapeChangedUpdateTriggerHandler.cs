using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class ConnectionFetchShapeChangedUpdateTriggerHandler(
        ITeamUpdater teamUpdater,
        IPortfolioUpdater portfolioUpdater,
        ILogger<ConnectionFetchShapeChangedUpdateTriggerHandler> logger) : IDomainEventHandler<ConnectionFetchShapeChanged>
    {
        public Task HandleAsync(ConnectionFetchShapeChanged domainEvent, CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Connection {ConnectionId} changed what its Teams and Portfolios download: updating {TeamCount} Teams and {PortfolioCount} Portfolios",
                domainEvent.ConnectionId,
                domainEvent.TeamIds.Count,
                domainEvent.PortfolioIds.Count);

            // Teams are queued first, so unless one is already running when the change arrives, a Portfolio
            // updating in the same round forecasts over Team data that already carries the changed fields.
            foreach (var teamId in domainEvent.TeamIds)
            {
                teamUpdater.TriggerUpdate(teamId);
            }

            foreach (var portfolioId in domainEvent.PortfolioIds)
            {
                portfolioUpdater.TriggerUpdate(portfolioId);
            }

            return Task.CompletedTask;
        }
    }
}
