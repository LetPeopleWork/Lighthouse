using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Modules.Forecasting.Application.Policies
{
    public class TeamDeletedForecastRetriggerHandler(IPortfolioUpdater portfolioUpdater) : IDomainEventHandler<TeamDeleted>
    {
        public Task HandleAsync(TeamDeleted domainEvent, CancellationToken cancellationToken)
        {
            foreach (var portfolioId in domainEvent.AffectedPortfolioIds)
            {
                portfolioUpdater.TriggerUpdate(portfolioId);
            }

            return Task.CompletedTask;
        }
    }
}
