namespace Lighthouse.Backend.Models.Events
{
    // Kept distinct from WorkItemUnblocked per ADR-104 §2 — see FeatureBlocked.
    public record FeatureUnblocked(int FeatureId, int PortfolioId) : IDomainEvent;
}
