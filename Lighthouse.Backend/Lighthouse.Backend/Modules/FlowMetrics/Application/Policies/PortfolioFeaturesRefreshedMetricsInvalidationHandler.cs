using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application.Policies
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
