using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Contracts
{
    /// <summary>Who owns the order changed, and the forecast queue changed with it (ADR-133).</summary>
    public record FeatureOrderingPolicyChanged(FeatureOrderingPolicy Policy) : IDomainEvent;
}
