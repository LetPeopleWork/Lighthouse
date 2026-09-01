using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// The one place that knows the ordering switch in the behaviour-settings table and the ordering
    /// policy the rest of the code works in are the same choice said two ways. Everyone else sees the
    /// policy.
    /// </summary>
    public class FeatureOrderingPolicyProvider(IRepository<OptionalFeature> repository) : IFeatureOrderingPolicyProvider
    {
        public FeatureOrderingPolicy GetPolicy()
        {
            var setting = repository.GetByPredicate(f => f.Key == OptionalFeatureKeys.FeatureOrderingKey);

            // An absent row - a fresh install, or an instance downgraded from a build that had one - reads
            // as the tracker owning the order rather than throwing. Nothing here asks about the licence:
            // a customer whose licence lapsed while they owned the order keeps the list they arranged.
            return setting?.Enabled == true
                ? FeatureOrderingPolicy.ManualOrder
                : FeatureOrderingPolicy.SourceOrder;
        }
    }
}
