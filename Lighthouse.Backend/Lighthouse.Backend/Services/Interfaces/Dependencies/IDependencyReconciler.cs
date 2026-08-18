using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Dependencies
{
    /// <summary>
    /// Writes what a Feature waits on. Lighthouse never authors a dependency of its own, so there is
    /// nothing of the user's to preserve here and reconciling is a wholesale replacement rather than a
    /// merge: to change a dependency you change it in the work tracking system.
    /// </summary>
    public interface IDependencyReconciler
    {
        void Reconcile(Feature feature, IEnumerable<FeatureDependencyReference> referencesFromTracker);
    }
}
