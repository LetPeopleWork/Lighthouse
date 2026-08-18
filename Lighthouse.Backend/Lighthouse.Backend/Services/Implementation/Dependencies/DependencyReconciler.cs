using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Dependencies;

namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    public class DependencyReconciler : IDependencyReconciler
    {
        public void Reconcile(Feature feature, IEnumerable<FeatureDependencyReference> referencesFromTracker)
        {
            // The key is the whole pair on purpose. A Feature that lists itself is a real one-Feature
            // loop, and a later warning has to be able to name it - dropping it as "a target that is
            // just me" would make that loop invisible instead of reported.
            var deduplicated = referencesFromTracker
                .DistinctBy(reference => (reference.FeatureId, reference.ReferenceId));

            feature.ReplaceDependsOnReferences(deduplicated);
        }
    }
}
