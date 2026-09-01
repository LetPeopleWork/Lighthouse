using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Answers who decides the order Features are forecast in. Read-only on purpose: the choice is
    /// recorded in exactly one place and this is not it. It takes no principal and no HTTP context,
    /// because the ordering seam runs deep inside repositories, where neither exists.
    /// </summary>
    public interface IFeatureOrderingPolicyProvider
    {
        /// <summary>An instance where nobody has chosen follows the tracker, without a row having to exist.</summary>
        FeatureOrderingPolicy GetPolicy();
    }
}
