using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// The only reader of the ordering-policy setting. No principal, no HTTP - the ordering seam runs
    /// deep inside repositories, where neither exists (ADR-134).
    /// </summary>
    public interface IFeatureOrderingPolicyProvider
    {
        /// <summary>An instance where nobody has chosen follows the tracker, without a row having to exist.</summary>
        FeatureOrderingPolicy GetPolicy();

        Task SetPolicy(FeatureOrderingPolicy policy);
    }
}
