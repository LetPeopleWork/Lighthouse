namespace Lighthouse.Backend.Models.Events
{
    // Distinct from WorkItemBlocked by ADR-104 §2: a generalised event would route feature ids into
    // WorkItemBlockedTransitionCaptureHandler (which writes WorkItemId unconditionally), reproducing the
    // identity-collision defect for real customers. Keep the record types separate so that is impossible.
    public record FeatureBlocked(int FeatureId, int PortfolioId, string Reason) : IDomainEvent;
}
