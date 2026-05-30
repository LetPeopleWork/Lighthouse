using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class PortfolioFeaturesRefreshedMetricsInvalidationHandler(
        IRepository<Portfolio> portfolioRepository,
        IPortfolioMetricsService portfolioMetricsService) : IDomainEventHandler<PortfolioFeaturesRefreshed>
    {
        public Task HandleAsync(PortfolioFeaturesRefreshed domainEvent, CancellationToken cancellationToken)
        {
            var portfolio = portfolioRepository.GetById(domainEvent.PortfolioId);
            if (portfolio == null)
            {
                return Task.CompletedTask;
            }

            portfolioMetricsService.InvalidatePortfolioMetrics(portfolio);
            return Task.CompletedTask;
        }
    }
}
