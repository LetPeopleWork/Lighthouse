using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Modules.WorkItems.Application.Policies
{
    public class PortfolioDeletedOrphanedFeatureCleanupHandler(IOrphanedFeatureCleanupService orphanedFeatureCleanupService) : IDomainEventHandler<PortfolioDeleted>
    {
        public Task HandleAsync(PortfolioDeleted domainEvent, CancellationToken cancellationToken)
        {
            return orphanedFeatureCleanupService.CleanupAsync(cancellationToken);
        }
    }
}
