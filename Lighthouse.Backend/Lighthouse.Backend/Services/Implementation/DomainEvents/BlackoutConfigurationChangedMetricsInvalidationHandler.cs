using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class BlackoutConfigurationChangedMetricsInvalidationHandler(
        IRepository<Team> teamRepository,
        IRepository<Portfolio> portfolioRepository,
        ITeamMetricsService teamMetricsService,
        IPortfolioMetricsService portfolioMetricsService) : IDomainEventHandler<BlackoutConfigurationChanged>
    {
        public Task HandleAsync(BlackoutConfigurationChanged domainEvent, CancellationToken cancellationToken)
        {
            foreach (var team in teamRepository.GetAll())
            {
                teamMetricsService.InvalidateTeamMetrics(team);
            }

            foreach (var portfolio in portfolioRepository.GetAll())
            {
                portfolioMetricsService.InvalidatePortfolioMetrics(portfolio);
            }

            return Task.CompletedTask;
        }
    }
}
