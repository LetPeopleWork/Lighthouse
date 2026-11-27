using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.WorkItems.Contracts
{
    // Kept distinct from WorkItemUnblocked per ADR-104 §2 — see FeatureBlocked.
    public record FeatureUnblocked(int FeatureId, int PortfolioId) : IDomainEvent;
}
