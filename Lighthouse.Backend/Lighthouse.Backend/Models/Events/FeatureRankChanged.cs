namespace Lighthouse.Backend.Models.Events
{
    /// <summary>
    /// A Feature holds a new place, so every forecast drawing from the order is stale (ADR-133). Carries
    /// the identity only — the handler resolves which Portfolios need a fresh run.
    /// </summary>
    public record FeatureRankChanged(int FeatureId) : IDomainEvent;
}
