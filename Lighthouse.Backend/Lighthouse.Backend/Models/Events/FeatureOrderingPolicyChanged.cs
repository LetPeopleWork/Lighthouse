namespace Lighthouse.Backend.Models.Events
{
    /// <summary>Who owns the order changed, and the forecast queue changed with it (ADR-133).</summary>
    public record FeatureOrderingPolicyChanged(FeatureOrderingPolicy Policy) : IDomainEvent;
}
