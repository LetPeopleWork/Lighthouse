using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class TeamDataRefreshedForecastTriggerHandler(
        IPortfolioRepository portfolioRepository,
        IForecastUpdater forecastUpdater) : IDomainEventHandler<TeamDataRefreshed>
    {
        public Task HandleAsync(TeamDataRefreshed domainEvent, CancellationToken cancellationToken)
        {
            foreach (var portfolioId in portfolioRepository.GetPortfolioIdsForTeam(domainEvent.TeamId))
            {
                forecastUpdater.TriggerUpdate(portfolioId);
            }

            return Task.CompletedTask;
        }
    }
}
