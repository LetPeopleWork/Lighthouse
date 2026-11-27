using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.Forecasting.Application.Policies
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
