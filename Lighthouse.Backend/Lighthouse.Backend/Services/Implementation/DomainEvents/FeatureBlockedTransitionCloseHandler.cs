using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class FeatureBlockedTransitionCloseHandler(
        IFeatureBlockedTransitionRepository transitionRepository,
        ILogger<FeatureBlockedTransitionCloseHandler> logger)
        : IDomainEventHandler<FeatureUnblocked>
    {
        public async Task HandleAsync(FeatureUnblocked domainEvent, CancellationToken cancellationToken)
        {
            var openSpell = transitionRepository.GetOpenSpell(domainEvent.PortfolioId, domainEvent.FeatureId);

            if (openSpell == null)
            {
                logger.LogInformation(
                    "No open blocked spell found for feature {FeatureId} in portfolio {PortfolioId}; skipping close",
                    domainEvent.FeatureId,
                    domainEvent.PortfolioId);
                return;
            }

            openSpell.LeftAt = DateTime.UtcNow;
            transitionRepository.Update(openSpell);
            await transitionRepository.Save();
        }
    }
}
