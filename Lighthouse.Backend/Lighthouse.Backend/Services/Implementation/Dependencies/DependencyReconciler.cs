using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Dependencies;

namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    public class DependencyReconciler : IDependencyReconciler
    {
        public void Reconcile(Feature feature, IEnumerable<FeatureDependencyReference> referencesFromTracker)
        {
            // A connector reads links off a Feature it has not saved yet, so the references it hands over
            // name Feature nought rather than the row they are about to hang on. Re-keying them here is
            // what makes the key below a key at all, and what lets anything reading a reference before the
            // next save - a warning, a cycle check - see the Feature it really belongs to.
            var keyedToTheFeatureThatWaits = referencesFromTracker
                .Select(reference => new FeatureDependencyReference(feature.Id, reference.ReferenceId, reference.Source));

            // The key is the whole pair on purpose. A Feature that lists itself is a real one-Feature
            // loop, and a later warning has to be able to name it - dropping it as "a target that is
            // just me" would make that loop invisible instead of reported.
            //
            // Read to the end before the replacement below clears anything: a Feature arriving for the
            // first time is reconciled against its own references, and a query still waiting to run would
            // find them already gone.
            var deduplicated = keyedToTheFeatureThatWaits
                .DistinctBy(reference => (reference.FeatureId, reference.ReferenceId))
                .ToList();

            feature.ReplaceDependsOnReferences(deduplicated);
        }
    }
}
