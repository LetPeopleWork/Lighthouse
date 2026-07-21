using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class FeatureBlockedTransitionCaptureHandler(
        IFeatureBlockedTransitionRepository transitionRepository,
        ILogger<FeatureBlockedTransitionCaptureHandler> logger)
        : IDomainEventHandler<FeatureBlocked>
    {
        public async Task HandleAsync(FeatureBlocked domainEvent, CancellationToken cancellationToken)
        {
            var existingOpen = transitionRepository.GetOpenSpell(domainEvent.PortfolioId, domainEvent.FeatureId);

            if (existingOpen != null)
            {
                logger.LogInformation(
                    "Feature {FeatureId} in portfolio {PortfolioId} already has an open blocked spell; skipping capture",
                    domainEvent.FeatureId,
                    domainEvent.PortfolioId);
                return;
            }

            var transition = new FeatureBlockedTransition
            {
                FeatureId = domainEvent.FeatureId,
                PortfolioId = domainEvent.PortfolioId,
                EnteredAt = DateTime.UtcNow,
                LeftAt = null,
            };

            transitionRepository.Add(transition);
            await transitionRepository.Save();
        }
    }
}
