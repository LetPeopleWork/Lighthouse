using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    /// <summary>
    /// Broadcasts a Portfolio's forecasts to the sources its Deliveries follow, on the run that produced
    /// them.
    ///
    /// It hangs off the forecast rather than off the fetch because the numbers being published are the
    /// forecast's, and the two no longer happen in the same execution. The target dates it measures
    /// against are the freshest there are: the fetch pass re-syncs every bound Delivery from its source
    /// before it asks for this forecast at all.
    ///
    /// Best-effort, like the daily snapshot beside it. A Jira that would not take today's numbers must
    /// not cost the refresh that produced them, and the next forecast asks again.
    /// </summary>
    public class DeliveryForecastPublishingHandler(
        IRepository<Portfolio> portfolioRepository,
        IDeliveryRepository deliveryRepository,
        IDeliveryForecastPublishingService publishingService,
        ILogger<DeliveryForecastPublishingHandler> logger) : IDomainEventHandler<PortfolioForecastsUpdated>
    {
        public async Task HandleAsync(PortfolioForecastsUpdated domainEvent, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            try
            {
                var portfolio = portfolioRepository.GetById(domainEvent.PortfolioId);

                if (portfolio == null)
                {
                    return;
                }

                var deliveries = deliveryRepository.GetRecordableByPortfolio(domainEvent.PortfolioId);

                await publishingService.PublishForPortfolio(portfolio, deliveries);

                // The only thing publishing can write to a Delivery is that its Release turned out not to
                // be there. Somebody editing that Delivery while this ran wins, exactly as they do on the
                // refresh: this pass is holding a copy read before that happened, and the next round
                // finds out about the Release again anyway.
                if (!await deliveryRepository.TrySaveRecomputedDeliveries())
                {
                    // Stryker disable once all: diagnostic log text is not behaviour. That the round
                    // survives a refused save is, and that is asserted.
                    logger.LogInformation(
                        "A Delivery of Portfolio {PortfolioId} was changed while its forecast was being published; what this round found out about its Release is picked up next time",
                        domainEvent.PortfolioId);
                }
            }
#pragma warning disable CA1031 // publishing is best-effort; a remote that would not take today's numbers must not cost the refresh that produced them
            catch (Exception exception)
#pragma warning restore CA1031
            {
                // Stryker disable once all: diagnostic log text is not behaviour. That the refresh
                // survives is, and that is asserted.
                logger.LogError(
                    exception,
                    "Could not publish the forecasts of Portfolio {PortfolioId} to the sources its Deliveries follow; the next forecast asks again",
                    domainEvent.PortfolioId);
            }
        }
    }
}
